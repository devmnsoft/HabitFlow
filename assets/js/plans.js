export const APP_VERSION = "1.5";
export const ENABLE_DEV_PLAN_TOGGLE = true;
export const ENABLE_GLOBAL_METRICS = false;
// Métricas globais devem ser gravadas futuramente por backend/Firebase Functions para evitar abuso.
export const PAYMENT_PROVIDER = "future";
export const PREMIUM_MONTHLY_PRICE = 14.90;
export const PREMIUM_YEARLY_PRICE = 99.00;
export const PREMIUM_MONTHLY_LABEL = "R$ 14,90/mês";
export const PREMIUM_YEARLY_LABEL = "R$ 99/ano";
export const ADMIN_EMAILS = ["coloque-aqui-o-email-admin@exemplo.com"];
export const PLAN_LIMITS = { free: 5, premium: Infinity };
export function hasPremiumAccess(profile = {}) {
  profile = profile || {};
  return profile.plan === "premium" && ["active", "trial"].includes(profile.planStatus || "active");
}
export function getActiveHabitLimit(profile = {}) {
  profile = profile || {};
  return hasPremiumAccess(profile) ? Infinity : PLAN_LIMITS.free;
}
export function planLabel(plan) { return plan === "premium" ? "Premium" : "Gratuito"; }
