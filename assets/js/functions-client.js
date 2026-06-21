import { firebaseApi } from "./firebase.js";
import { APP_ENV } from "./plans.js";

export async function callFunction(name, payload = {}, options = {}) {
  try {
    const fn = firebaseApi.httpsCallable(firebaseApi.functions, name);
    const result = await fn(payload);
    return { ok: true, data: result.data };
  } catch (error) {
    if (APP_ENV === "development" && options.silent !== true) {
      console.warn(`[HabitFlow] Function ${name} falhou`, error?.code || error?.message || error);
    }
    return { ok: false, error, code: error?.code || "functions/internal", message: error?.message || "Falha na Function." };
  }
}
