const { onCall, HttpsError, onRequest } = require("firebase-functions/v2/https");
const logger = require("firebase-functions/logger");
const admin = require("firebase-admin");
const crypto = require("node:crypto");
const { answerByRules, detectSensitiveContent, sanitizeText } = require("./knowledgeBase");
const { buildAssistantSystemPrompt } = require("./assistantPrompt");
const { applyCors } = require("./cors");

admin.initializeApp();
const db = admin.firestore();
const FieldValue = admin.firestore.FieldValue;

const APP_VERSION = "2.1-SecurityOps";
const LOG_RETENTION_DAYS = 90;
const ERROR_LOG_RETENTION_DAYS = 180;
const VALID_SEVERITIES = ["info", "warning", "error", "critical"];
const VALID_SOURCES = ["frontend", "backend", "function", "payment", "auth", "firestore", "pwa", "chatbot"];
const SEVERITY_RANK = { info: 0, warning: 1, error: 2, critical: 3 };
const PREMIUM_MONTHLY_PRICE = 14.90;
const PREMIUM_YEARLY_PRICE = 99.00;
const VALID_PLAN_TYPES = ["monthly", "yearly"];
const VALID_PLANS = ["free", "premium"];
const VALID_STATUSES = ["active", "trial", "past_due", "canceled", "inactive"];
const BLOCKED_KEY = /password|senha|token|accessToken|refreshToken|authorization|apiKey|secret|card|cvv|cpf|document|payloadCompleto|rawPayload|payer|stack|raw|payload/i;
const DEFAULT_TELEGRAM_EVENTS = "critical,error,checkout_failed,webhook_error,premium_interest,user_signup,frontend_error,backend_error,firebase_error,unauthorized_admin_attempt,admin_set_user_plan,payment_confirmed,payment_failed,chatbot_sensitive_request_blocked,chatbot_security_block,prompt_injection_attempt,rate_limit_exceeded,suspicious_activity,support_ticket_created,chatbot_commercial_request,security_incident_created,lgpd_data_deletion_requested";

function env(name, fallback = "") { return process.env[name] || fallback; }
function environment() { return env("APP_ENV", env("FUNCTIONS_EMULATOR") ? "development" : "production"); }
function paymentProvider() { return env("PAYMENT_PROVIDER", "mercadopago").toLowerCase(); }
function appBaseUrl() { return env("APP_BASE_URL", "https://habitflow-5f945.web.app"); }
function adminEmails() { return env("ADMIN_EMAILS", "").split(",").map((e) => e.trim().toLowerCase()).filter(Boolean); }
function isGeneralAdmin(request) { return Boolean(request.auth?.token?.email && adminEmails().includes(request.auth.token.email.toLowerCase())); }
function requireAuth(request) { if (!request.auth) throw new HttpsError("unauthenticated", "Você precisa estar autenticado."); }
async function checkRateLimit(uid, functionName, limitCount, windowMs) {
  if (!uid) throw new HttpsError("unauthenticated", "Você precisa estar autenticado.");
  const ref = db.doc(`users/${uid}/rateLimits/${functionName}`);
  const nowMs = Date.now();
  try {
    await db.runTransaction(async (tx) => {
      const snap = await tx.get(ref);
      const data = snap.exists ? snap.data() : {};
      const windowStartMs = data.windowStart?.toMillis ? data.windowStart.toMillis() : 0;
      const expired = !windowStartMs || nowMs - windowStartMs >= windowMs;
      const nextCount = expired ? 1 : Number(data.count || 0) + 1;
      if (nextCount > limitCount) throw new HttpsError("resource-exhausted", "Muitas solicitações. Aguarde alguns instantes e tente novamente.");
      tx.set(ref, { count: nextCount, windowStart: expired ? FieldValue.serverTimestamp() : data.windowStart, updatedAt: FieldValue.serverTimestamp(), action: functionName }, { merge: true });
    });
  } catch (error) {
    if (error instanceof HttpsError && error.code === "resource-exhausted") {
      logger.warn("rate_limit_exceeded", { uid, action: functionName });
      writeSystemAuditLog({ type: "rate_limit_exceeded", severity: "warning", source: "backend", userId: uid, action: functionName, message: "Rate limit excedido em Function crítica.", metadata: { limit: limitCount, windowMs } }).catch(() => {});
    }
    throw error;
  }
}
function validatePayload(allowedFields, data = {}) {
  const extra = Object.keys(data || {}).filter((key) => !allowedFields.includes(key));
  if (extra.length) throw new HttpsError("invalid-argument", "Payload contém campos não permitidos.");
  return data;
}
async function assertGeneralAdmin(request) {
  requireAuth(request);
  await checkRateLimit(request.auth.uid, "adminFunctions", 30, 60 * 1000);
  if (!isGeneralAdmin(request)) {
    await writeSystemAuditLog({ type: "unauthorized_admin_attempt", severity: "critical", source: "backend", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "admin_function_denied", message: "Tentativa de chamada administrativa sem permissão." });
    throw new HttpsError("permission-denied", "Você não tem permissão para executar esta ação.");
  }
}
function truncate(value, max = 500) { return String(value ?? "").slice(0, max); }
function safeTelegramValue(value, max = 160) { return truncate(value || "-", max).replace(/[\r\n]+/g, " "); }
function sanitizeMetadata(input = {}, depth = 0) {
  if (depth > 3 || input == null) return null;
  if (["string", "number", "boolean"].includes(typeof input)) return typeof input === "string" ? truncate(input) : input;
  if (Array.isArray(input)) return input.slice(0, 20).map((item) => sanitizeMetadata(item, depth + 1));
  if (typeof input !== "object") return String(input).slice(0, 120);
  const output = {};
  for (const [key, value] of Object.entries(input).slice(0, 40)) {
    if (BLOCKED_KEY.test(key)) continue;
    output[key] = sanitizeMetadata(value, depth + 1);
  }
  return JSON.parse(JSON.stringify(output).slice(0, 6000));
}
function errorFingerprint(metadata = {}, message = "") {
  const raw = [metadata.errorName, metadata.errorCode, metadata.functionName, truncate(message, 120)].filter(Boolean).join("|");
  return raw ? crypto.createHash("sha256").update(raw).digest("hex").slice(0, 16) : null;
}
async function recordSecurityEvent(event) {
  return writeSystemAuditLog({ type: event.type || "suspicious_activity", severity: event.severity || "warning", source: event.source || "backend", action: event.action || event.type || "security_event", message: event.message || "Evento de segurança registrado.", ...event });
}
async function writeSystemAuditLog(event) {
  const metadata = sanitizeMetadata(event.metadata || {});
  const severity = VALID_SEVERITIES.includes(event.severity) ? event.severity : "info";
  const source = VALID_SOURCES.includes(event.source) ? event.source : "backend";
  const ref = db.collection("systemAuditLogs").doc();
  const payload = {
    type: truncate(event.type || event.action || "backend_event", 80), severity, source,
    userId: event.userId || null, userEmail: truncate(event.userEmail || "", 160), userName: truncate(event.userName || "", 160),
    action: truncate(event.action || event.type || "event", 100), message: truncate(event.message || "Evento registrado.", 500),
    createdAt: FieldValue.serverTimestamp(), metadata, appVersion: APP_VERSION, environment: environment(), readByAdmin: false,
    telegramSent: false, telegramSentAt: null, bugStatus: event.bugStatus || (["error","critical"].includes(severity) || String(event.type || "").includes("bug") ? "new" : null), resolvedAt: null, resolvedBy: null, errorCode: metadata?.errorCode || event.errorCode || null, errorName: metadata?.errorName || event.errorName || null, errorMessage: metadata?.errorMessage || event.errorMessage || null, errorFingerprint: event.errorFingerprint || errorFingerprint(metadata, event.message)
  };
  await ref.set(payload);
  const sent = await sendTelegramAlert({ id: ref.id, ...payload });
  if (sent) await ref.set({ telegramSent: true, telegramSentAt: FieldValue.serverTimestamp() }, { merge: true });
  return ref;
}
function shouldNotifyTelegram(event) {
  if (env("TELEGRAM_ENABLED", "false") !== "true") return false;
  const min = env("TELEGRAM_MIN_SEVERITY", "warning");
  const notifyEvents = env("TELEGRAM_NOTIFY_EVENTS", DEFAULT_TELEGRAM_EVENTS).split(",").map((v) => v.trim()).filter(Boolean);
  return SEVERITY_RANK[event.severity] >= SEVERITY_RANK[min] || notifyEvents.includes(event.type) || notifyEvents.includes(event.action);
}
function telegramMessage(event) {
  if (event.type === "telegram_test") {
    return `✅ HabitFlow Telegram configurado com sucesso.\n\nBot: @hablitflowmns_bot\nAmbiente: ${environment()}\nVersão: ${APP_VERSION}\nData: ${new Date().toISOString()}`;
  }
  const meta = sanitizeMetadata(event.metadata || {}) || {};
  const user = `${safeTelegramValue(event.userName, 80)} / ${safeTelegramValue(event.userEmail, 120)}`;
  if (["error", "critical"].includes(event.severity) || String(event.type).includes("error")) {
    return `🚨 HabitFlow Alerta\n\nSeveridade: ${String(event.severity).toUpperCase()}\nTipo: ${safeTelegramValue(event.type)}\nAção: ${safeTelegramValue(event.action)}\nUsuário: ${user}\nAmbiente: ${safeTelegramValue(event.environment)}\nVersão: ${APP_VERSION}\nMensagem: ${safeTelegramValue(event.message, 240)}\nData: ${new Date().toISOString()}\n\nDetalhes:\n- Código: ${safeTelegramValue(meta.errorCode || meta.code, 80)}\n- Origem: ${safeTelegramValue(event.source, 80)}\n- Página: ${safeTelegramValue(meta.page || meta.path, 120)}`;
  }
  return `📌 HabitFlow Evento\n\nTipo: ${safeTelegramValue(event.type)}\nUsuário: ${user}\nPlano atual: ${safeTelegramValue(meta.currentPlan || meta.plan || "-")}\nAmbiente: ${safeTelegramValue(event.environment)}\nMensagem: ${safeTelegramValue(event.message, 240)}\nData: ${new Date().toISOString()}`;
}
async function sendTelegramAlert(event) {
  if (!shouldNotifyTelegram(event)) return false;
  const token = env("TELEGRAM_BOT_TOKEN");
  const chatId = env("TELEGRAM_ADMIN_CHAT_ID");
  if (!token || !chatId) { logger.warn("telegram_not_configured", { tokenConfigured: Boolean(token), chatConfigured: Boolean(chatId) }); return false; }
  try {
    const response = await fetch(`https://api.telegram.org/bot${token}/sendMessage`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ chat_id: chatId, text: telegramMessage(event).slice(0, 3500), disable_web_page_preview: true }) });
    if (!response.ok) logger.error("telegram_api_error", { status: response.status });
    return response.ok;
  } catch (error) { logger.error("telegram_error", { message: error.message }); return false; }
}
const requireAdmin = assertGeneralAdmin;
async function audit(action, authInfo = {}, metadata = {}) { await db.collection("adminAuditLogs").add({ action, adminUid: authInfo.uid || "system", adminEmail: authInfo.email || "system", targetUserId: metadata.targetUserId || metadata.userId || null, createdAt: FieldValue.serverTimestamp(), metadata: sanitizeMetadata(metadata) }); }

const DEFAULT_SYSTEM_SETTINGS = { companyName: "MNSOFT", companyLegalName: "MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA", companyCnpj: "18.160.057/0001-13", commercialEmail: "comercial@mnsoft.com.br", whatsappEnabled: false, whatsappNumber: "", whatsappDefaultMessage: "Olá, vim pelo HabitFlow e gostaria de falar com a equipe da MNSOFT.", whatsappButtonText: "Falar com a MNSOFT", supportEmail: "comercial@mnsoft.com.br" };
function publicSettings(data = {}) { const merged = { ...DEFAULT_SYSTEM_SETTINGS, ...(data || {}) }; return { companyName: truncate(merged.companyName, 80), companyLegalName: truncate(merged.companyLegalName, 160), companyCnpj: truncate(merged.companyCnpj, 24), commercialEmail: truncate(merged.commercialEmail, 160), whatsappEnabled: merged.whatsappEnabled === true, whatsappNumber: truncate(merged.whatsappNumber || "", 20), whatsappDefaultMessage: truncate(merged.whatsappDefaultMessage, 240), whatsappButtonText: truncate(merged.whatsappButtonText, 80), supportEmail: truncate(merged.supportEmail || merged.commercialEmail, 160) }; }
function normalizeWhatsapp(value = "") { if (!/^[+()\-\s\d]*$/.test(String(value))) throw new HttpsError("invalid-argument", "WhatsApp inválido."); const normalized = String(value).replace(/[+()\-\s]/g, ""); if (normalized && !/^[1-9]\d{9,14}$/.test(normalized)) throw new HttpsError("invalid-argument", "WhatsApp inválido."); return normalized; }
async function writeBillingEvent({ provider, type, userId = null, status = "received", metadata = {} }) { const ref = db.collection("billingEvents").doc(); await ref.set({ provider, type, userId, receivedAt: FieldValue.serverTimestamp(), status, metadata: sanitizeMetadata(metadata) }); return ref; }
async function setSubscription(userId, data) { const subRef = db.doc(`users/${userId}/billing/subscription`); const profileRef = db.doc(`users/${userId}/profile/main`); const effectivePlan = data.plan === "premium" && ["active", "trial", "past_due"].includes(data.status) ? "premium" : "free"; await db.runTransaction(async (tx) => { const snap = await tx.get(subRef); tx.set(subRef, { ...data, updatedAt: FieldValue.serverTimestamp(), createdAt: snap.exists ? snap.get("createdAt") || FieldValue.serverTimestamp() : FieldValue.serverTimestamp() }, { merge: true }); tx.set(profileRef, { plan: effectivePlan, planStatus: data.status === "past_due" ? "past_due" : (effectivePlan === "premium" ? "active" : data.status), updatedAt: FieldValue.serverTimestamp() }, { merge: true }); }); }
function mockCheckoutUrl(planType) { return `${appBaseUrl()}?payment=pending&mode=mock&plan=${encodeURIComponent(planType)}`; }
async function createMercadoPagoCheckout(userId, planType) { if (!env("MERCADOPAGO_ACCESS_TOKEN")) return { checkoutUrl: mockCheckoutUrl(planType), mode: "mock" }; return { checkoutUrl: mockCheckoutUrl(planType), mode: "sandbox" }; }
async function createStripeCheckout(userId, planType) { if (!env("STRIPE_SECRET_KEY")) return { checkoutUrl: mockCheckoutUrl(planType), mode: "mock" }; return { checkoutUrl: mockCheckoutUrl(planType), mode: "sandbox" }; }



const VALID_TICKET_TYPES = ["bug", "support", "commercial", "premium", "other"];
const VALID_TICKET_PRIORITIES = ["low", "medium", "high", "critical"];
const VALID_TICKET_STATUSES = ["open", "in_progress", "resolved", "closed"];
function aiEnabled() { return env("AI_ENABLED", "false") === "true" && Boolean(env("AI_API_KEY")); }
function supportProtocol() { const d = new Date(); const ymd = d.toISOString().slice(0, 10).replace(/-/g, ""); return `HF-${ymd}-${Math.floor(1000 + Math.random() * 9000)}`; }
function publicTicket(doc) { const d = doc.data ? doc.data() : doc; return { id: doc.id || d.id || d.protocol, protocol: d.protocol, type: d.type, status: d.status, priority: d.priority, title: d.title, description: d.description, screen: d.screen, source: d.source, userEmail: d.userEmail || "", userName: d.userName || "", createdAt: d.createdAt, updatedAt: d.updatedAt, resolvedAt: d.resolvedAt || null, telegramSent: d.telegramSent === true }; }
async function saveConversationMessage(uid, authToken, message, result, context = {}) {
  const convRef = db.collection("users").doc(uid).collection("supportConversations").doc("chatbot-main");
  const safeUserText = result.safetyBlocked ? "[mensagem bloqueada por segurança]" : sanitizeText(message, 600);
  await convRef.set({ createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp(), status: "open", source: "chatbot", lastIntent: result.intent, lastMessagePreview: safeUserText.slice(0, 140), messageCount: FieldValue.increment(2), screen: sanitizeText(context.screen || "", 80) }, { merge: true });
  await convRef.collection("messages").add({ role: "user", text: safeUserText, intent: result.intent, createdAt: FieldValue.serverTimestamp(), safetyBlocked: result.safetyBlocked === true, source: result.source });
  await convRef.collection("messages").add({ role: "assistant", text: sanitizeText(result.answer, 1200), intent: result.intent, createdAt: FieldValue.serverTimestamp(), safetyBlocked: result.safetyBlocked === true, source: result.source });
}
async function createTicketRecord({ request, type = "support", priority = "medium", title, description, screen = "", source = "chatbot" }) {
  requireAuth(request);
  const safeType = VALID_TICKET_TYPES.includes(type) ? type : "other";
  const safePriority = VALID_TICKET_PRIORITIES.includes(priority) ? priority : "medium";
  const protocol = supportProtocol();
  const ref = db.collection("supportTickets").doc(protocol);
  const payload = { protocol, userId: request.auth.uid, userEmail: sanitizeText(request.auth.token.email || "", 160), userName: sanitizeText(request.auth.token.name || "", 160), type: safeType, status: "open", priority: safePriority, title: sanitizeText(title || "Chamado HabitFlow", 120), description: sanitizeText(description || "", 1000), screen: sanitizeText(screen || "", 80), createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp(), resolvedAt: null, assignedTo: null, source, telegramSent: false };
  await ref.set(payload);
  await writeSystemAuditLog({ type: "support_ticket_created", severity: safePriority === "critical" ? "critical" : (safeType === "bug" ? "warning" : "info"), source: "chatbot", userId: request.auth.uid, userEmail: request.auth.token.email || "", userName: request.auth.token.name || "", action: "create_support_ticket", message: `Chamado criado. Protocolo: ${protocol}`, metadata: { protocol, ticketType: safeType, priority: safePriority, screen } });
  return { id: ref.id, ...payload };
}
exports.askHabitFlowAssistant = onCall(async (request) => { try {
  requireAuth(request); await checkRateLimit(request.auth.uid, "askHabitFlowAssistant", 20, 60 * 1000); const d = request.data || {}; validatePayload(["message","context"], d); const message = sanitizeText(d.message || "", 1000); const context = d.context || {};
  if (!message) throw new HttpsError("invalid-argument", "Envie uma mensagem para o assistente.");
  if (String(d.message || "").length > 1000) throw new HttpsError("invalid-argument", "Mensagem muito longa. Use até 1000 caracteres.");
  const sensitive = detectSensitiveContent(message); let result = answerByRules(message);
  if (aiEnabled()) { result = { ...result, source: "backend_ai", answer: result.answer }; /* Provider futuro: usar buildAssistantSystemPrompt() + base controlada. */ }
  if (env("AI_ENABLED", "false") === "true" && !env("AI_API_KEY")) result.source = "fallback";
  await writeSystemAuditLog({ type: "chatbot_message_received", severity: result.severity || "info", source: "chatbot", userId: request.auth.uid, userEmail: request.auth.token.email || "", userName: request.auth.token.name || "", action: "ask_habitflow_assistant", message: result.safetyBlocked ? "Mensagem bloqueada por segurança." : "Mensagem recebida pelo chatbot.", metadata: { intent: result.intent, screen: context.screen, appVersion: context.appVersion, aiEnabled: aiEnabled(), promptReady: Boolean(buildAssistantSystemPrompt()) } });
  if (sensitive.blocked) await writeSystemAuditLog({ type: "chatbot_sensitive_request_blocked", severity: sensitive.critical ? "critical" : "warning", source: "chatbot", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "safety_block", message: "Solicitação sensível bloqueada pelo chatbot.", metadata: { intent: result.intent, screen: context.screen } });
  await saveConversationMessage(request.auth.uid, request.auth.token, message, result, context);
  await writeSystemAuditLog({ type: "chatbot_response_generated", severity: "info", source: "chatbot", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "chatbot_response", message: "Resposta segura gerada pelo assistente.", metadata: { intent: result.intent, source: result.source } });
  return { answer: result.answer, intent: result.intent, source: result.source, confidence: result.confidence, suggestedActions: result.suggestedActions || [], safetyBlocked: result.safetyBlocked === true };
} catch (error) { logger.error("askHabitFlowAssistant error", { message: error.message }); if (error instanceof HttpsError) throw error; await writeSystemAuditLog({ type: "chatbot_error", severity: "error", source: "chatbot", userId: request.auth?.uid, userEmail: request.auth?.token?.email || "", action: "ask_habitflow_assistant", message: "Erro na Function askHabitFlowAssistant.", metadata: { errorName: error.name } }); throw new HttpsError("internal", "Não foi possível responder agora. Tente novamente ou fale com o suporte da MNSOFT."); } });
exports.createSupportTicket = onCall(async (request) => { try { requireAuth(request); await checkRateLimit(request.auth.uid, "createSupportTicket", 5, 10 * 60 * 1000); const d = request.data || {}; validatePayload(["type","priority","title","description","screen","source"], d); const ticket = await createTicketRecord({ request, type: d.type, priority: d.priority, title: d.title, description: d.description, screen: d.screen, source: d.source || "chatbot" }); return { success: true, ticket: publicTicket(ticket) }; } catch (error) { logger.error("createSupportTicket error", { message: error.message }); if (error instanceof HttpsError) throw error; throw new HttpsError("internal", "Não foi possível criar o chamado agora."); } });
exports.getMySupportTickets = onCall(async (request) => { requireAuth(request); const snap = await db.collection("supportTickets").where("userId", "==", request.auth.uid).orderBy("createdAt", "desc").limit(50).get(); return { tickets: snap.docs.map(publicTicket) }; });
exports.getAdminSupportTickets = onCall(async (request) => { await requireAdmin(request); const snap = await db.collection("supportTickets").orderBy("createdAt", "desc").limit(100).get(); await audit("get_admin_support_tickets", { uid: request.auth.uid, email: request.auth.token.email }); return { tickets: snap.docs.map(publicTicket) }; });
exports.updateSupportTicketStatus = onCall(async (request) => { await requireAdmin(request); const { ticketId, status } = request.data || {}; if (!ticketId || !VALID_TICKET_STATUSES.includes(status)) throw new HttpsError("invalid-argument", "Status inválido."); const update = { status, updatedAt: FieldValue.serverTimestamp() }; if (["resolved", "closed"].includes(status)) update.resolvedAt = FieldValue.serverTimestamp(); await db.collection("supportTickets").doc(ticketId).set(update, { merge: true }); await audit("update_support_ticket_status", { uid: request.auth.uid, email: request.auth.token.email }, { ticketId, status }); return { success: true }; });
exports.getAdminSupportSummary = onCall(async (request) => { await requireAdmin(request); const snap = await db.collection("supportTickets").orderBy("createdAt", "desc").limit(200).get(); const rows = snap.docs.map(publicTicket); return { totalOpen: rows.filter(t=>t.status==="open").length, bugsOpen: rows.filter(t=>t.type==="bug" && !["resolved","closed"].includes(t.status)).length, criticalOpen: rows.filter(t=>t.priority==="critical" && !["resolved","closed"].includes(t.status)).length, resolved: rows.filter(t=>t.status==="resolved").length, latest: rows.slice(0, 10) }; });

exports.logSystemEvent = onCall(async (request) => { try { requireAuth(request); await checkRateLimit(request.auth.uid, "logSystemEvent", 60, 60 * 1000); const d = request.data || {}; const ref = await writeSystemAuditLog({ ...d, userId: request.auth.uid, userEmail: request.auth.token.email || "", userName: request.auth.token.name || "", severity: VALID_SEVERITIES.includes(d.severity) ? d.severity : "info" }); return { success: true, logId: ref.id }; } catch (error) { logger.error("logSystemEvent_failed", { message: error.message }); if (error instanceof HttpsError) throw error; throw new HttpsError("internal", "Erro interno registrado para análise."); } });
exports.sendTestTelegramAlert = onCall(async (request) => { await requireAdmin(request); await checkRateLimit(request.auth.uid, "sendTestTelegramAlert", 5, 10 * 60 * 1000); const ok = await sendTelegramAlert({ type: "telegram_test", severity: "warning", source: "backend", action: "send_test", userEmail: request.auth.token.email || "", environment: environment(), message: "Teste de configuração do Telegram.", metadata: {} }); await audit("send_test_telegram_alert", { uid: request.auth.uid, email: request.auth.token.email }); return { success: ok, message: ok ? "Mensagem de teste enviada para o Telegram." : "Não foi possível enviar o teste do Telegram. Verifique as configurações das Functions." }; });
exports.getAdminDashboardSummary = onCall(async (request) => { await requireAdmin(request); const logs = await db.collection("systemAuditLogs").orderBy("createdAt", "desc").limit(200).get(); const rows = logs.docs.map((d) => ({ id: d.id, ...d.data() })); await audit("get_admin_dashboard_summary", { uid: request.auth.uid, email: request.auth.token.email }); return { totalEvents: rows.length, totalErrors: rows.filter((r) => r.severity === "error").length, totalCritical: rows.filter((r) => r.severity === "critical").length, totalPremiumInterest: rows.filter((r) => r.type === "premium_interest").length, totalSignupsRecent: rows.filter((r) => r.type === "user_signup" || r.action === "signup").length, totalLoginsRecent: rows.filter((r) => r.type === "user_login" || r.action === "login").length, telegramEnabled: env("TELEGRAM_ENABLED", "false") === "true", telegramChatConfigured: Boolean(env("TELEGRAM_ADMIN_CHAT_ID")), telegramTokenConfigured: Boolean(env("TELEGRAM_BOT_TOKEN")), telegramMinSeverity: env("TELEGRAM_MIN_SEVERITY", "warning"), telegramNotifyEvents: env("TELEGRAM_NOTIFY_EVENTS", DEFAULT_TELEGRAM_EVENTS), latestErrors: rows.filter((r) => ["error", "critical"].includes(r.severity)).slice(0, 5).map(publicLog) }; });
exports.getAdminRecentLogs = onCall(async (request) => { await requireAdmin(request); const d = request.data || {}; let q = db.collection("systemAuditLogs").orderBy("createdAt", "desc").limit(Math.min(Number(d.limit) || 50, 100)); const snap = await q.get(); let rows = snap.docs.map((doc) => publicLog({ id: doc.id, ...doc.data() })); if (d.severity) rows = rows.filter((r) => r.severity === d.severity); if (d.type) rows = rows.filter((r) => r.type === d.type); if (d.userEmail) rows = rows.filter((r) => String(r.userEmail || "").includes(String(d.userEmail).toLowerCase())); if (d.environment) rows = rows.filter((r) => r.environment === d.environment); await audit("get_admin_recent_logs", { uid: request.auth.uid, email: request.auth.token.email }, sanitizeMetadata(d)); return { logs: rows }; });
exports.getAdminErrorLogs = onCall(async (request) => { await requireAdmin(request); const snap = await db.collection("systemAuditLogs").orderBy("createdAt", "desc").limit(100).get(); await audit("get_admin_error_logs", { uid: request.auth.uid, email: request.auth.token.email }); return { logs: snap.docs.map((d) => publicLog({ id: d.id, ...d.data() })).filter((r) => ["error", "critical"].includes(r.severity)) }; });
exports.getAdminUserActivitySummary = onCall(async (request) => { await requireAdmin(request); const snap = await db.collection("systemAuditLogs").orderBy("createdAt", "desc").limit(100).get(); await audit("get_admin_user_activity_summary", { uid: request.auth.uid, email: request.auth.token.email }); return { activities: snap.docs.map((d) => publicLog({ id: d.id, ...d.data() })).filter((r) => ["user_login", "user_signup", "habit_created", "premium_interest", "checkout_started", "terms_accepted", "login", "signup"].includes(r.type) || ["login", "signup", "habit_created", "premium_interest", "premium_checkout_clicked", "terms_accepted"].includes(r.action)).slice(0, 50) }; });
exports.markAuditLogAsRead = onCall(async (request) => { await requireAdmin(request); const { logId } = request.data || {}; if (!logId) throw new HttpsError("invalid-argument", "Log inválido."); await db.collection("systemAuditLogs").doc(logId).set({ readByAdmin: true }, { merge: true }); await audit("mark_audit_log_as_read", { uid: request.auth.uid, email: request.auth.token.email }, { logId }); return { success: true }; });
function publicLog(r) { return { id: r.id, type: r.type, severity: r.severity, source: r.source, userId: r.userId, userEmail: r.userEmail, userName: r.userName, action: r.action, message: r.message, createdAt: r.createdAt, metadata: sanitizeMetadata(r.metadata || {}), appVersion: r.appVersion, environment: r.environment, readByAdmin: r.readByAdmin, telegramSent: r.telegramSent, telegramSentAt: r.telegramSentAt, bugStatus: r.bugStatus || null, errorCode: r.errorCode || null, errorName: r.errorName || null, errorMessage: r.errorMessage || null, errorFingerprint: r.errorFingerprint }; }
exports.createCheckoutSession = onCall(async (request) => { try { requireAuth(request); await checkRateLimit(request.auth.uid, "createCheckoutSession", 5, 10 * 60 * 1000); const { planType } = request.data || {}; if (!VALID_PLAN_TYPES.includes(planType)) { await writeSystemAuditLog({ type: "checkout_failed", severity: "warning", source: "payment", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "invalid_plan_type", message: "planType inválido no checkout.", metadata: { planType } }); throw new HttpsError("invalid-argument", "Não foi possível iniciar o checkout agora."); } const userId = request.auth.uid; const provider = paymentProvider(); const checkout = provider === "stripe" ? await createStripeCheckout(userId, planType) : await createMercadoPagoCheckout(userId, planType); await writeBillingEvent({ provider, type: "checkout_created", userId, status: "processed", metadata: { planType, mode: checkout.mode, price: planType === "monthly" ? PREMIUM_MONTHLY_PRICE : PREMIUM_YEARLY_PRICE } }); await writeSystemAuditLog({ type: "checkout_started", severity: "info", source: "payment", userId, userEmail: request.auth.token.email || "", action: "create_checkout_session", message: "Checkout iniciado.", metadata: { provider, planType, mode: checkout.mode } }); return { checkoutUrl: checkout.checkoutUrl, provider, mode: checkout.mode }; } catch (error) { logger.error("createCheckoutSession error", { message: error.message }); if (error instanceof HttpsError) throw error; await writeSystemAuditLog({ type: "checkout_failed", severity: "error", source: "payment", userId: request.auth?.uid, userEmail: request.auth?.token?.email || "", action: "create_checkout_session", message: "Falha ao iniciar checkout.", metadata: { errorName: error.name, errorCode: error.code } }); throw new HttpsError("internal", "Não foi possível iniciar o checkout agora."); } });
function validateWebhook(req, provider) { const secret = provider === "stripe" ? env("STRIPE_WEBHOOK_SECRET") : env("MERCADOPAGO_WEBHOOK_SECRET"); if (!secret) return { valid: true, mode: "unsigned" }; const signature = req.get("x-signature") || req.get("stripe-signature") || ""; return signature ? { valid: true, mode: "signature-present" } : { valid: false, mode: "missing-signature" }; }
exports.paymentWebhook = onRequest(async (req, res) => { if (applyCors(req, res)) return; const provider = (req.query.provider || paymentProvider()).toString().toLowerCase(); const type = req.body?.type || req.body?.action || "unknown"; const eventRef = await writeBillingEvent({ provider, type, metadata: { query: req.query, type, id: req.body?.id, external_reference: req.body?.external_reference } }); try { const validation = validateWebhook(req, provider); if (!validation.valid) { await writeSystemAuditLog({ type: "webhook_error", severity: "critical", source: "payment", action: "invalid_webhook", message: "Webhook inválido recebido.", metadata: { provider, type, validation: validation.mode } }); await eventRef.set({ status: "ignored", processedAt: FieldValue.serverTimestamp() }, { merge: true }); return res.status(401).json({ ok: false, status: "ignored" }); } const userId = req.body?.metadata?.userId || req.body?.external_reference || req.query.userId; const paid = ["payment.approved", "approved", "checkout.session.completed", "invoice.paid"].includes(type) || req.body?.status === "approved"; const canceled = ["subscription.canceled", "customer.subscription.deleted", "canceled"].includes(type) || req.body?.status === "cancelled"; if (userId && paid) await setSubscription(userId, { provider, plan: "premium", status: "active", billingCycle: req.body?.metadata?.planType || "monthly", providerPaymentId: String(req.body?.id || ""), lastWebhookAt: FieldValue.serverTimestamp() }); if (userId && canceled) await setSubscription(userId, { provider, plan: "free", status: "inactive", billingCycle: null, canceledAt: FieldValue.serverTimestamp(), lastWebhookAt: FieldValue.serverTimestamp() }); await eventRef.set({ userId: userId || null, status: paid || canceled ? "processed" : "ignored", processedAt: FieldValue.serverTimestamp() }, { merge: true }); await writeSystemAuditLog({ type: "webhook_received", severity: "info", source: "payment", userId: userId || null, action: paid || canceled ? "webhook_processed" : "webhook_ignored", message: "Webhook recebido.", metadata: { provider, type, paid, canceled } }); res.json({ ok: true, status: paid || canceled ? "processed" : "ignored" }); } catch (error) { logger.error("paymentWebhook error", { message: error.message }); await eventRef.set({ status: "error", processedAt: FieldValue.serverTimestamp() }, { merge: true }); await writeSystemAuditLog({ type: "webhook_error", severity: "critical", source: "payment", action: "webhook_exception", message: "Erro ao processar webhook.", metadata: { provider, type, errorName: error.name } }); res.status(500).json({ ok: false }); } });
exports.adminSetUserPlan = onCall(async (request) => { try { await requireAdmin(request); const { userId, plan, status, reason } = request.data || {}; if (!userId || !VALID_PLANS.includes(plan) || !VALID_STATUSES.includes(status) || !reason) throw new HttpsError("invalid-argument", "Não foi possível processar essa solicitação."); await setSubscription(userId, { provider: "manual", plan, status, billingCycle: null }); await writeBillingEvent({ provider: "manual", type: "admin_set_user_plan", userId, status: "processed", metadata: { reason, plan, planStatus: status } }); await audit("admin_set_user_plan", { uid: request.auth.uid, email: request.auth.token.email }, { targetUserId: userId, plan, status, reason }); await writeSystemAuditLog({ type: "manual_plan_change", severity: "warning", source: "backend", userId, action: "admin_set_user_plan", message: "Plano alterado manualmente por administrador.", metadata: { plan, status, reason } }); return { ok: true }; } catch (error) { if (error instanceof HttpsError) throw error; logger.error("adminSetUserPlan error", { message: error.message }); throw new HttpsError("internal", "Erro interno registrado para análise."); } });

exports.markBugAsRead = onCall(async (request) => { await requireAdmin(request); const { logId } = request.data || {}; if (!logId) throw new HttpsError("invalid-argument", "Log inválido."); await db.collection("systemAuditLogs").doc(logId).set({ readByAdmin: true, bugStatus: "read" }, { merge: true }); await audit("mark_bug_as_read", { uid: request.auth.uid, email: request.auth.token.email }, { logId }); return { success: true }; });
exports.markBugAsResolved = onCall(async (request) => { await requireAdmin(request); const { logId } = request.data || {}; if (!logId) throw new HttpsError("invalid-argument", "Log inválido."); await db.collection("systemAuditLogs").doc(logId).set({ readByAdmin: true, bugStatus: "resolved", resolvedAt: FieldValue.serverTimestamp(), resolvedBy: { uid: request.auth.uid, email: request.auth.token.email || "" } }, { merge: true }); await audit("mark_bug_as_resolved", { uid: request.auth.uid, email: request.auth.token.email }, { logId }); return { success: true }; });
exports.ignoreBug = onCall(async (request) => { await requireAdmin(request); const { logId } = request.data || {}; if (!logId) throw new HttpsError("invalid-argument", "Log inválido."); await db.collection("systemAuditLogs").doc(logId).set({ readByAdmin: true, bugStatus: "ignored" }, { merge: true }); await audit("ignore_bug", { uid: request.auth.uid, email: request.auth.token.email }, { logId }); return { success: true }; });
exports.getPublicSystemSettings = onCall(async (request) => { requireAuth(request); const snap = await db.doc("systemSettings/public").get(); return publicSettings(snap.exists ? snap.data() : DEFAULT_SYSTEM_SETTINGS); });
exports.updateSystemSettings = onCall(async (request) => { await requireAdmin(request); const d = request.data || {}; const settings = publicSettings({ ...d, whatsappNumber: normalizeWhatsapp(d.whatsappNumber || ""), whatsappEnabled: d.whatsappEnabled === true, supportEmail: d.supportEmail || d.commercialEmail || DEFAULT_SYSTEM_SETTINGS.supportEmail }); await db.doc("systemSettings/public").set({ ...settings, updatedAt: FieldValue.serverTimestamp(), updatedBy: { uid: request.auth.uid, email: request.auth.token.email || "" } }, { merge: true }); await audit("update_system_settings", { uid: request.auth.uid, email: request.auth.token.email }, settings); await writeSystemAuditLog({ type: "system_settings_updated", severity: "warning", source: "backend", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "update_system_settings", message: "Configurações públicas de atendimento atualizadas.", metadata: { whatsappEnabled: settings.whatsappEnabled, companyName: settings.companyName } }); return { success: true, settings }; });
exports.testFrontendLogger = onCall(async (request) => { await requireAdmin(request); await writeSystemAuditLog({ type: "frontend_logger_test", severity: "warning", source: "backend", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "diagnostic_logger", message: "Teste controlado do logger executado." }); return { success: true }; });

exports.healthCheck = onCall(async (request) => {
  try {
    requireAuth(request);
    const adminUser = isGeneralAdmin(request);
    if (!adminUser) return { ok: true };
    let firestoreOk = false;
    try { await db.collection("systemAuditLogs").limit(1).get(); firestoreOk = true; } catch (error) { logger.error("healthCheck_firestore_failed", { message: error.message }); }
    return { ok: true, timestamp: new Date().toISOString(), environment: environment(), appVersion: APP_VERSION, functionsRegion: "us-central1", telegramConfigured: Boolean(env("TELEGRAM_BOT_TOKEN") && env("TELEGRAM_ADMIN_CHAT_ID")), firestoreOk };
  } catch (error) {
    logger.error("healthCheck_failed", { message: error.message });
    if (error instanceof HttpsError) throw error;
    throw new HttpsError("internal", "HealthCheck indisponível.");
  }
});

const VALID_INCIDENT_SEVERITIES = ["low", "medium", "high", "critical"];
const VALID_INCIDENT_STATUSES = ["open", "investigating", "resolved", "closed"];
function publicIncident(doc) { const d = doc.data ? doc.data() : doc; return { id: doc.id || d.id, title: d.title, description: d.description, severity: d.severity, status: d.status, createdAt: d.createdAt, updatedAt: d.updatedAt, resolvedAt: d.resolvedAt || null, createdBy: d.createdBy || null, assignedTo: d.assignedTo || null, relatedLogIds: d.relatedLogIds || [], actionsTaken: d.actionsTaken || [], impact: d.impact || "", rootCause: d.rootCause || "", resolution: d.resolution || "" }; }
exports.getAdminSecuritySummary = onCall(async (request) => {
  await requireAdmin(request);
  const logs = await db.collection("systemAuditLogs").orderBy("createdAt", "desc").limit(300).get();
  const rows = logs.docs.map((d) => ({ id: d.id, ...d.data() }));
  const suspiciousTypes = ["suspicious_activity","rate_limit_exceeded","unauthorized_admin_attempt","chatbot_security_block","prompt_injection_attempt","invalid_payload_detected","possible_scraping_detected"];
  const suspicious = rows.filter((r) => suspiciousTypes.includes(r.type));
  const critical = rows.filter((r) => r.severity === "critical");
  const riskLevel = critical.length ? "Crítico" : suspicious.length > 10 ? "Alto" : suspicious.length > 3 ? "Médio" : "Baixo";
  await audit("get_admin_security_summary", { uid: request.auth.uid, email: request.auth.token.email });
  return { riskLevel, suspicious24h: suspicious.length, deniedAdmin: rows.filter((r) => r.type === "unauthorized_admin_attempt").length, rateLimitExceeded: rows.filter((r) => r.type === "rate_limit_exceeded").length, criticalErrors: critical.length, pendingLogs: rows.filter((r) => r.readByAdmin !== true).length, latestCritical: publicLog(critical[0] || {}), appCheckStatus: env("APP_CHECK_ENFORCED", "false") === "true" ? "configurado" : "pendente", lastBackup: env("LAST_BACKUP_AT", "pendente"), pipelineStatus: env("PIPELINE_STATUS", "validado manualmente"), events: suspicious.slice(0, 100).map(publicLog) };
});
exports.createSecurityIncident = onCall(async (request) => { await requireAdmin(request); const d = validatePayload(["title","description","severity","assignedTo","relatedLogIds","impact"], request.data || {}); if (!d.title || !VALID_INCIDENT_SEVERITIES.includes(d.severity)) throw new HttpsError("invalid-argument", "Incidente inválido."); const ref = await db.collection("securityIncidents").add({ title: truncate(d.title, 160), description: truncate(d.description, 1000), severity: d.severity, status: "open", createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp(), resolvedAt: null, createdBy: { uid: request.auth.uid, email: request.auth.token.email || "" }, assignedTo: truncate(d.assignedTo || "", 160), relatedLogIds: Array.isArray(d.relatedLogIds) ? d.relatedLogIds.slice(0, 20) : [], actionsTaken: [], impact: truncate(d.impact || "", 1000), rootCause: "", resolution: "" }); await audit("create_security_incident", { uid: request.auth.uid, email: request.auth.token.email }, { incidentId: ref.id, severity: d.severity }); await recordSecurityEvent({ type: "security_incident_created", severity: ["high","critical"].includes(d.severity) ? "critical" : "warning", source: "backend", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: "create_security_incident", message: "Incidente de segurança criado.", metadata: { incidentId: ref.id, incidentSeverity: d.severity } }); return { success: true, incidentId: ref.id }; });
exports.getSecurityIncidents = onCall(async (request) => { await requireAdmin(request); const snap = await db.collection("securityIncidents").orderBy("updatedAt", "desc").limit(100).get(); await audit("get_security_incidents", { uid: request.auth.uid, email: request.auth.token.email }); return { incidents: snap.docs.map(publicIncident) }; });
exports.updateSecurityIncident = onCall(async (request) => { await requireAdmin(request); const d = validatePayload(["incidentId","status","assignedTo","actionsTaken","impact","rootCause","resolution"], request.data || {}); if (!d.incidentId || (d.status && !VALID_INCIDENT_STATUSES.includes(d.status))) throw new HttpsError("invalid-argument", "Incidente inválido."); const update = { updatedAt: FieldValue.serverTimestamp() }; for (const k of ["status","assignedTo","impact","rootCause","resolution"]) if (d[k] !== undefined) update[k] = typeof d[k] === "string" ? truncate(d[k], 1000) : d[k]; if (Array.isArray(d.actionsTaken)) update.actionsTaken = d.actionsTaken.slice(0, 50).map((v) => truncate(v, 300)); await db.collection("securityIncidents").doc(d.incidentId).set(update, { merge: true }); await audit("update_security_incident", { uid: request.auth.uid, email: request.auth.token.email }, { incidentId: d.incidentId, status: d.status }); return { success: true }; });
exports.closeSecurityIncident = onCall(async (request) => { await requireAdmin(request); const { incidentId, resolution } = request.data || {}; if (!incidentId || !resolution) throw new HttpsError("invalid-argument", "Incidente inválido."); await db.collection("securityIncidents").doc(incidentId).set({ status: "closed", resolution: truncate(resolution, 1000), resolvedAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp() }, { merge: true }); await audit("close_security_incident", { uid: request.auth.uid, email: request.auth.token.email }, { incidentId }); return { success: true }; });
async function createDataRequest(request, type) { requireAuth(request); await checkRateLimit(request.auth.uid, `dataRequest_${type}`, 3, 24 * 60 * 60 * 1000); const ref = await db.collection("users").doc(request.auth.uid).collection("dataRequests").add({ type, status: "requested", createdAt: FieldValue.serverTimestamp(), updatedAt: FieldValue.serverTimestamp(), userEmail: request.auth.token.email || "" }); await recordSecurityEvent({ type: type === "delete" ? "lgpd_data_deletion_requested" : "lgpd_data_export_requested", severity: type === "delete" ? "critical" : "warning", source: "backend", userId: request.auth.uid, userEmail: request.auth.token.email || "", action: `request_user_data_${type}`, message: type === "delete" ? "Solicitação LGPD de exclusão criada." : "Solicitação LGPD de exportação criada.", metadata: { requestId: ref.id } }); return { success: true, requestId: ref.id, protocol: `LGPD-${ref.id.slice(0, 8).toUpperCase()}` }; }
exports.requestUserDataExport = onCall((request) => createDataRequest(request, "export"));
exports.requestUserDataDeletion = onCall((request) => createDataRequest(request, "delete"));
exports.getAdminDataRequests = onCall(async (request) => { await requireAdmin(request); const users = await db.collectionGroup("dataRequests").orderBy("createdAt", "desc").limit(100).get(); await audit("get_admin_data_requests", { uid: request.auth.uid, email: request.auth.token.email }); return { requests: users.docs.map((doc) => ({ id: doc.id, path: doc.ref.path, ...doc.data() })) }; });
exports.updateDataRequestStatus = onCall(async (request) => { await requireAdmin(request); const { path, status } = request.data || {}; if (!path || !["requested","reviewing","completed"].includes(status)) throw new HttpsError("invalid-argument", "Solicitação inválida."); await db.doc(path).set({ status, updatedAt: FieldValue.serverTimestamp(), updatedBy: { uid: request.auth.uid, email: request.auth.token.email || "" } }, { merge: true }); await audit("update_data_request_status", { uid: request.auth.uid, email: request.auth.token.email }, { path, status }); return { success: true }; });

exports.LOG_RETENTION_DAYS = LOG_RETENTION_DAYS;
exports.ERROR_LOG_RETENTION_DAYS = ERROR_LOG_RETENTION_DAYS;
