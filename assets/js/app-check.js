import { APP_CHECK_DEBUG_TOKEN, APP_CHECK_ENABLED, APP_CHECK_SITE_KEY, IS_DEVELOPMENT, IS_PRODUCTION } from "./env.js";

export function getAppCheckDiagnostics() {
  if (!APP_CHECK_ENABLED) return { status: IS_DEVELOPMENT ? "desativado em desenvolvimento" : "desativado" };
  if (!APP_CHECK_SITE_KEY) return { status: "configuração ausente" };
  return { status: "ativo" };
}

export async function initAppCheckIfEnabled(app) {
  if (!APP_CHECK_ENABLED) {
    if (IS_DEVELOPMENT) console.info("[HabitFlow] App Check desativado em desenvolvimento.");
    return null;
  }

  if (!APP_CHECK_SITE_KEY) {
    console.warn("[HabitFlow] App Check habilitado, mas VITE_APP_CHECK_SITE_KEY não foi configurado.");
    return null;
  }

  if (IS_DEVELOPMENT && APP_CHECK_DEBUG_TOKEN) {
    self.FIREBASE_APPCHECK_DEBUG_TOKEN = APP_CHECK_DEBUG_TOKEN;
  }

  try {
    const { initializeAppCheck, ReCaptchaV3Provider } = await import("https://www.gstatic.com/firebasejs/10.12.5/firebase-app-check.js");
    return initializeAppCheck(app, {
      provider: new ReCaptchaV3Provider(APP_CHECK_SITE_KEY),
      isTokenAutoRefreshEnabled: IS_PRODUCTION
    });
  } catch (error) {
    console.warn("[HabitFlow] App Check não inicializado.", error?.message || error);
    return null;
  }
}

export const setupAppCheck = initAppCheckIfEnabled;
