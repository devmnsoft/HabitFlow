import { initializeAppCheck, ReCaptchaV3Provider } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app-check.js";
import { APP_CHECK_DEBUG_TOKEN, APP_CHECK_ENABLED, APP_CHECK_SITE_KEY, IS_DEVELOPMENT, IS_PRODUCTION } from "./env.js";

export function setupAppCheck(app) {
  if (!APP_CHECK_ENABLED || !APP_CHECK_SITE_KEY) {
    if (IS_DEVELOPMENT) console.info("[HabitFlow] App Check não inicializado: configure VITE_APP_CHECK_ENABLED=true e VITE_APP_CHECK_SITE_KEY.");
    return null;
  }

  if (IS_DEVELOPMENT && APP_CHECK_DEBUG_TOKEN) {
    self.FIREBASE_APPCHECK_DEBUG_TOKEN = APP_CHECK_DEBUG_TOKEN;
  }

  try {
    return initializeAppCheck(app, {
      provider: new ReCaptchaV3Provider(APP_CHECK_SITE_KEY),
      isTokenAutoRefreshEnabled: IS_PRODUCTION
    });
  } catch (error) {
    if (IS_DEVELOPMENT) console.warn("[HabitFlow] Falha ao inicializar App Check", error?.message || error);
    return null;
  }
}
