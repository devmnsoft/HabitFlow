const { onCall, HttpsError, onRequest } = require("firebase-functions/v2/https");
const logger = require("firebase-functions/logger");
const admin = require("firebase-admin");
const crypto = require("node:crypto");

admin.initializeApp();
const db = admin.firestore();
const FieldValue = admin.firestore.FieldValue;

const PREMIUM_MONTHLY_PRICE = 14.90;
const PREMIUM_YEARLY_PRICE = 99.00;
const PREMIUM_MONTHLY_LABEL = "HabitFlow Premium Mensal";
const PREMIUM_YEARLY_LABEL = "HabitFlow Premium Anual";
const VALID_PLAN_TYPES = ["monthly", "yearly"];
const VALID_PLANS = ["free", "premium"];
const VALID_STATUSES = ["active", "trial", "past_due", "canceled", "inactive"];

function env(name, fallback = "") { return process.env[name] || fallback; }
function paymentProvider() { return env("PAYMENT_PROVIDER", "mercadopago").toLowerCase(); }
function appBaseUrl() { return env("APP_BASE_URL", "https://habitflow-5f945.web.app"); }
function adminEmails() { return env("ADMIN_EMAILS", "").split(",").map((e) => e.trim().toLowerCase()).filter(Boolean); }
function isAdminEmail(email) { return Boolean(email && adminEmails().includes(email.toLowerCase())); }
function sanitizeMetadata(input = {}) {
  const blocked = /card|cpf|document|token|secret|password|authorization|payer/i;
  return Object.fromEntries(Object.entries(input || {}).filter(([key]) => !blocked.test(key)).slice(0, 30));
}
async function audit(action, authInfo = {}, metadata = {}) {
  await db.collection("adminAuditLogs").add({
    action,
    adminUid: authInfo.uid || "system",
    adminEmail: authInfo.email || "system",
    targetUserId: metadata.targetUserId || metadata.userId || null,
    createdAt: FieldValue.serverTimestamp(),
    metadata: sanitizeMetadata(metadata)
  });
}
async function writeBillingEvent({ provider, type, userId = null, status = "received", metadata = {} }) {
  const ref = db.collection("billingEvents").doc();
  await ref.set({ provider, type, userId, receivedAt: FieldValue.serverTimestamp(), status, metadata: sanitizeMetadata(metadata) });
  return ref;
}
async function setSubscription(userId, data) {
  const subscription = {
    provider: data.provider || "manual",
    plan: data.plan,
    status: data.status,
    billingCycle: data.billingCycle || null,
    updatedAt: FieldValue.serverTimestamp(),
    ...data
  };
  const subRef = db.doc(`users/${userId}/billing/subscription`);
  const profileRef = db.doc(`users/${userId}/profile/main`);
  const effectivePlan = subscription.plan === "premium" && ["active", "trial", "past_due"].includes(subscription.status) ? "premium" : "free";
  await db.runTransaction(async (tx) => {
    const snap = await tx.get(subRef);
    tx.set(subRef, { ...subscription, createdAt: snap.exists ? snap.get("createdAt") || FieldValue.serverTimestamp() : FieldValue.serverTimestamp() }, { merge: true });
    tx.set(profileRef, { plan: effectivePlan, planStatus: subscription.status === "past_due" ? "past_due" : (effectivePlan === "premium" ? "active" : subscription.status), updatedAt: FieldValue.serverTimestamp() }, { merge: true });
  });
}
function mockCheckoutUrl(planType) { return `${appBaseUrl()}?payment=pending&mode=mock&plan=${encodeURIComponent(planType)}`; }
async function createMercadoPagoCheckout(userId, planType, userEmail) {
  if (!env("MERCADOPAGO_ACCESS_TOKEN")) return { checkoutUrl: mockCheckoutUrl(planType), mode: "mock" };
  // Estrutura preparada: chamada real ao endpoint /checkout/preferences deve ser ativada após validar credenciais e webhook no sandbox.
  logger.info("Mercado Pago token configured; returning sandbox placeholder until real preference creation is enabled", { userId, planType });
  return { checkoutUrl: mockCheckoutUrl(planType), mode: "sandbox" };
}
async function createStripeCheckout(userId, planType, userEmail) {
  if (!env("STRIPE_SECRET_KEY")) return { checkoutUrl: mockCheckoutUrl(planType), mode: "mock" };
  logger.info("Stripe secret configured; returning sandbox placeholder until Stripe checkout is enabled", { userId, planType });
  return { checkoutUrl: mockCheckoutUrl(planType), mode: "sandbox" };
}
exports.createCheckoutSession = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Usuário precisa estar autenticado.");
  const { planType } = request.data || {};
  if (!VALID_PLAN_TYPES.includes(planType)) throw new HttpsError("invalid-argument", "planType deve ser monthly ou yearly.");
  const userId = request.auth.uid;
  const email = request.auth.token.email || "";
  const profile = await db.doc(`users/${userId}/profile/main`).get();
  if (!profile.exists) throw new HttpsError("failed-precondition", "Perfil do usuário não encontrado.");
  const provider = paymentProvider();
  const checkout = provider === "stripe" ? await createStripeCheckout(userId, planType, email) : await createMercadoPagoCheckout(userId, planType, email);
  await writeBillingEvent({ provider, type: "checkout_created", userId, status: "processed", metadata: { planType, mode: checkout.mode, price: planType === "monthly" ? PREMIUM_MONTHLY_PRICE : PREMIUM_YEARLY_PRICE } });
  await audit("checkout_created", { uid: userId, email }, { userId, provider, planType, mode: checkout.mode });
  return { checkoutUrl: checkout.checkoutUrl, provider, mode: checkout.mode };
});
function validateWebhook(req, provider) {
  const secret = provider === "stripe" ? env("STRIPE_WEBHOOK_SECRET") : env("MERCADOPAGO_WEBHOOK_SECRET");
  if (!secret) return { valid: true, mode: "unsigned" };
  const signature = req.get("x-signature") || req.get("stripe-signature") || "";
  if (!signature) return { valid: false, mode: "missing-signature" };
  // Placeholder mínimo. Em produção, valide conforme a especificação do gateway antes de processar eventos reais.
  return { valid: true, mode: "signature-present" };
}
exports.paymentWebhook = onRequest(async (req, res) => {
  const provider = (req.query.provider || paymentProvider()).toString().toLowerCase();
  const type = req.body?.type || req.body?.action || "unknown";
  const eventRef = await writeBillingEvent({ provider, type, metadata: { query: req.query, type, id: req.body?.id, external_reference: req.body?.external_reference } });
  await audit("webhook_received", {}, { provider, type });
  try {
    const validation = validateWebhook(req, provider);
    if (!validation.valid) {
      await eventRef.set({ status: "ignored", processedAt: FieldValue.serverTimestamp(), metadata: { validation: validation.mode } }, { merge: true });
      return res.status(401).json({ ok: false, status: "ignored" });
    }
    const userId = req.body?.metadata?.userId || req.body?.external_reference || req.query.userId;
    if (!userId) {
      await eventRef.set({ status: "ignored", processedAt: FieldValue.serverTimestamp() }, { merge: true });
      return res.json({ ok: true, status: "ignored" });
    }
    const paid = ["payment.approved", "approved", "checkout.session.completed", "invoice.paid"].includes(type) || req.body?.status === "approved";
    const canceled = ["subscription.canceled", "customer.subscription.deleted", "canceled"].includes(type) || req.body?.status === "cancelled";
    if (paid) await setSubscription(userId, { provider, plan: "premium", status: "active", billingCycle: req.body?.metadata?.planType || "monthly", providerPaymentId: String(req.body?.id || ""), lastWebhookAt: FieldValue.serverTimestamp() });
    if (canceled) await setSubscription(userId, { provider, plan: "free", status: "inactive", billingCycle: null, canceledAt: FieldValue.serverTimestamp(), lastWebhookAt: FieldValue.serverTimestamp() });
    await eventRef.set({ userId, status: paid || canceled ? "processed" : "ignored", processedAt: FieldValue.serverTimestamp() }, { merge: true });
    await audit(paid || canceled ? "webhook_processed" : "webhook_received", {}, { provider, type, userId });
    res.json({ ok: true, status: paid || canceled ? "processed" : "ignored" });
  } catch (error) {
    logger.error("paymentWebhook error", error);
    await eventRef.set({ status: "error", processedAt: FieldValue.serverTimestamp() }, { merge: true });
    await audit("webhook_error", {}, { provider, type, message: error.message });
    res.status(500).json({ ok: false });
  }
});
exports.adminSetUserPlan = onCall(async (request) => {
  if (!request.auth) throw new HttpsError("unauthenticated", "Admin precisa estar autenticado.");
  const adminEmail = request.auth.token.email || "";
  if (!isAdminEmail(adminEmail)) throw new HttpsError("permission-denied", "Apenas admins podem alterar planos.");
  const { userId, plan, status, reason } = request.data || {};
  if (!userId || !VALID_PLANS.includes(plan) || !VALID_STATUSES.includes(status) || !reason) throw new HttpsError("invalid-argument", "Dados inválidos para alteração manual.");
  await setSubscription(userId, { provider: "manual", plan, status, billingCycle: null });
  await writeBillingEvent({ provider: "manual", type: "admin_set_user_plan", userId, status: "processed", metadata: { reason, plan, planStatus: status } });
  await audit("admin_set_user_plan", { uid: request.auth.uid, email: adminEmail }, { targetUserId: userId, plan, status, reason });
  return { ok: true };
});
