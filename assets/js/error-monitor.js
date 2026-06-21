import { auth, db, firebaseApi } from "./firebase.js";
import { APP_VERSION, APP_ENV } from "./plans.js";

const BLOCKED_KEY = /password|senha|token|accessToken|refreshToken|authorization|apiKey|secret|card|cvv|cpf|document|payloadCompleto|rawPayload/i;

export function sanitizeMetadata(input = {}, depth = 0) {
  if (depth > 3 || input == null) return null;
  if (["string", "number", "boolean"].includes(typeof input)) return typeof input === "string" ? input.slice(0, 500) : input;
  if (Array.isArray(input)) return input.slice(0, 20).map((item) => sanitizeMetadata(item, depth + 1));
  if (typeof input !== "object") return String(input).slice(0, 120);
  const output = {};
  for (const [key, value] of Object.entries(input).slice(0, 40)) {
    if (BLOCKED_KEY.test(key)) continue;
    output[key] = sanitizeMetadata(value, depth + 1);
  }
  return JSON.parse(JSON.stringify(output).slice(0, 6000));
}

function safeUrl() {
  return `${location.origin}${location.pathname}${location.hash || ""}`;
}

export function errorMetadata(error, extra = {}) {
  const stack = String(error?.stack || "").split("\n").slice(0, 5).join("\n");
  return sanitizeMetadata({
    page: location.pathname,
    url: safeUrl(),
    userAgent: navigator.userAgent.slice(0, 180),
    appVersion: APP_VERSION,
    environment: APP_ENV,
    errorCode: error?.code || extra.errorCode || "",
    errorName: error?.name || "Error",
    sanitizedStack: stack,
    ...extra
  });
}

export async function registerUsageEvent(user, type, metadata = {}) {
  if (!user || !user.uid) return;
  try {
    const eventsRef = firebaseApi.collection(db, "users", user.uid, "usageEvents");
    await firebaseApi.addDoc(eventsRef, {
      type,
      metadata: sanitizeMetadata(metadata),
      createdAt: firebaseApi.serverTimestamp(),
      appVersion: APP_VERSION,
      environment: APP_ENV
    });
  } catch (error) {
    if (APP_ENV === "development") console.warn("[HabitFlow] registerUsageEvent falhou", error?.code || error?.message || error);
  }
}

export async function reportFrontendError(error, context = {}) {
  const metadata = errorMetadata(error, {
    ...(context.metadata || {}),
    action: context.action || "unknown"
  });
  const payload = {
    type: "frontend_error",
    severity: context.severity || "error",
    source: context.source || "frontend",
    action: context.action || "unknown",
    message: String(context.message || error?.message || "Erro frontend capturado.").slice(0, 500),
    metadata
  };
  await registerUsageEvent(auth.currentUser, "frontend_error", payload);
  await logSystemEvent(payload);
}

export async function logSystemEvent({ type, severity = "info", source = "frontend", action, message, metadata = {} }) {
  try {
    const callable = firebaseApi.httpsCallable(firebaseApi.functions, "logSystemEvent");
    return await callable({ type, severity, source, action: action || type, message: String(message || "Evento registrado.").slice(0, 500), metadata: sanitizeMetadata(metadata) });
  } catch (error) {
    if (APP_ENV === "development") console.warn("[HabitFlow] logSystemEvent falhou", error?.code || error?.message || error);
    return null;
  }
}

export async function trackUserAction(action, metadata = {}, options = {}) {
  const user = auth.currentUser;
  if (!user) return;
  const safeMetadata = sanitizeMetadata(metadata);
  try {
    await firebaseApi.addDoc(firebaseApi.collection(db, "users", user.uid, "usageEvents"), {
      userId: user.uid,
      userEmail: user.email || "",
      createdAt: firebaseApi.serverTimestamp(),
      type: action,
      action,
      source: "frontend",
      metadata: safeMetadata,
      appVersion: APP_VERSION,
      environment: APP_ENV
    });
  } catch (error) {
    if (APP_ENV === "development") console.warn("[HabitFlow] usage event falhou", error?.code || error?.message || error);
  }
  const globalActions = new Set(["login", "signup", "logout", "terms_accepted", "habit_created", "habit_updated", "habit_archived", "habit_restored", "habit_completed", "habit_uncompleted", "premium_interest", "premium_checkout_clicked", "checkout_success_return", "checkout_pending_return", "checkout_failure_return", "admin_panel_opened", "app_loaded", "dashboard_loaded", "habits_loaded"]);
  if (options.global !== false && globalActions.has(action)) {
    const severity = options.severity || (safeMetadata?.durationMs > 3000 ? "warning" : "info");
    await logSystemEvent({ type: action.startsWith("login") ? "user_login" : action === "signup" ? "user_signup" : action, severity, source: options.source || "frontend", action, message: options.message || `Ação registrada: ${action}`, metadata: safeMetadata });
  }
}

export function setupErrorMonitoring(showFriendlyError = () => {}) {
  window.addEventListener("error", (event) => {
    reportFrontendError(event.error || new Error(event.message), { action: "window_error", message: "Erro JavaScript inesperado." }).catch((error) => APP_ENV === "development" && console.warn("Falha no logger global", error));
    showFriendlyError("Ops", "Encontramos um erro inesperado. Nossa equipe foi notificada.", "danger");
  });
  window.addEventListener("unhandledrejection", (event) => {
    const reason = event.reason instanceof Error ? event.reason : new Error(String(event.reason || "Promise rejeitada"));
    reportFrontendError(reason, { action: "unhandledrejection", message: "Falha assíncrona inesperada." }).catch((error) => APP_ENV === "development" && console.warn("Falha no logger global", error));
  });
  if ("serviceWorker" in navigator) {
    navigator.serviceWorker?.addEventListener?.("error", (event) => logSystemEvent({ type: "pwa_error", severity: "warning", source: "pwa", action: "service_worker_error", message: "Falha no service worker.", metadata: sanitizeMetadata({ message: event.message, page: location.pathname }) }).catch(() => {}));
  }
}
