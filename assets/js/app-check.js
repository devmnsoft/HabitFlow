import { getEnvBoolean, getEnvString, APP_CHECK_DEBUG_TOKEN, IS_DEVELOPMENT } from "./env.js";

export async function initAppCheckIfEnabled(app) {
  const enabled = getEnvBoolean("VITE_APP_CHECK_ENABLED", false);
  const siteKey = getEnvString("VITE_APP_CHECK_SITE_KEY", "");

  if (!enabled) {
    if (IS_DEVELOPMENT) {
      console.info("[HabitFlow] App Check desativado em desenvolvimento.");
    }
    return null;
  }

  if (!siteKey) {
    console.warn("[HabitFlow] App Check habilitado, mas VITE_APP_CHECK_SITE_KEY não foi configurado.");
    return null;
  }

  if (IS_DEVELOPMENT && APP_CHECK_DEBUG_TOKEN) {
    self.FIREBASE_APPCHECK_DEBUG_TOKEN = APP_CHECK_DEBUG_TOKEN;
  }

  try {
    const { initializeAppCheck, ReCaptchaV3Provider } = await import("https://www.gstatic.com/firebasejs/10.12.5/firebase-app-check.js");

    return initializeAppCheck(app, {
      provider: new ReCaptchaV3Provider(siteKey),
      isTokenAutoRefreshEnabled: true
    });
  } catch (error) {
    console.warn("[HabitFlow] App Check não inicializado.", error?.message || error);
    return null;
  }
}
