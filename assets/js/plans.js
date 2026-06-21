import { APP_VERSION, APP_ENV, IS_DEVELOPMENT, IS_PRODUCTION } from "./env.js";
export { APP_VERSION, APP_ENV, IS_DEVELOPMENT, IS_PRODUCTION };
export const ENABLE_DEV_PLAN_TOGGLE = IS_DEVELOPMENT && APP_ENV !== "production";
export const ENABLE_GLOBAL_METRICS = false;
// Métricas globais e billing real devem ser gravados por backend/Firebase Functions para evitar abuso.
export const PAYMENT_PROVIDER = "mercadopago";
export const PAYMENT_MODE = import.meta.env?.VITE_PAYMENT_MODE || "sandbox";
export const PREMIUM_MONTHLY_PRICE = 14.90;
export const PREMIUM_YEARLY_PRICE = 99.00;
export const PREMIUM_MONTHLY_LABEL = "R$ 14,90/mês";
export const PREMIUM_YEARLY_LABEL = "R$ 99/ano";
export const ADMIN_EMAILS = ["admin@habitflow.app", "marcelo@mnsoft.com.br", "coloque-aqui-o-email-admin@exemplo.com"];
export const PLAN_LIMITS = { free: 5, premium: Infinity };

export function getUserPlan(profile = {}, subscription = {}) {
  profile = profile || {};
  subscription = subscription || {};
  const plan = profile.plan || "free";
  const profileStatus = profile.planStatus || "active";
  const subscriptionStatus = subscription.status || profileStatus;
  const effectiveStatus = subscriptionStatus || profileStatus;

  if (plan === "premium" && ["active", "trial", "past_due"].includes(effectiveStatus)) return "premium";
  return "free";
}

export function hasPremiumAccess(profile = {}, subscription = {}) {
  return getUserPlan(profile, subscription) === "premium";
}

export function getPlanAlert(profile = {}, subscription = {}) {
  const status = subscription?.status || profile?.planStatus || "active";
  if (profile?.plan === "premium" && status === "past_due") return "Seu Premium está com pagamento pendente. Regularize para evitar bloqueios futuros.";
  if (profile?.plan === "premium" && ["canceled", "inactive"].includes(status)) return "Sua assinatura Premium não está ativa. Você voltou ao plano gratuito.";
  return "";
}

export function getActiveHabitLimit(profile = {}, subscription = {}) {
  return hasPremiumAccess(profile, subscription) ? Infinity : PLAN_LIMITS.free;
}

export function planLabel(plan) { return plan === "premium" ? "Premium" : "Gratuito"; }
export function billingCycleLabel(cycle) { return cycle === "yearly" ? "Anual" : cycle === "monthly" ? "Mensal" : "-"; }
export function planStatusLabel(status) {
  return ({ active: "Ativo", trial: "Trial", past_due: "Pendente", canceled: "Cancelado", inactive: "Inativo" }[status] || status || "Ativo");
}
