import { getFunctions, httpsCallable } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-functions.js";
import { app } from "./firebase.js";
import { APP_ENV } from "./plans.js";

export const functions = getFunctions(app, "us-central1");

export async function callFunction(name, payload = {}, options = {}) {
  try {
    const fn = httpsCallable(functions, name);
    const result = await fn(payload);
    return { ok: true, data: result.data };
  } catch (error) {
    if (APP_ENV === "development" && options.silent !== true) {
      console.warn(`[HabitFlow] Function ${name} falhou`, {
        code: error?.code || null,
        message: error?.message || null
      });
    }

    return {
      ok: false,
      error,
      code: error?.code || "functions/internal",
      message: error?.message || "Falha na Function."
    };
  }
}
