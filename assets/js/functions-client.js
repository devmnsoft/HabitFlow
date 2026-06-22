import { getFunctions, httpsCallable } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-functions.js";
import { app } from "./firebase.js";
import { IS_DEVELOPMENT } from "./env.js";

export const functions = getFunctions(app, "us-central1");

const FRIENDLY_MESSAGES = {
  "functions/unauthenticated": "Sua sessão expirou. Faça login novamente.",
  "functions/permission-denied": "Você não tem permissão para executar esta ação.",
  "functions/not-found": "Recurso não encontrado.",
  "functions/invalid-argument": "Dados inválidos. Revise as informações e tente novamente.",
  "functions/resource-exhausted": "Muitas solicitações. Aguarde alguns instantes e tente novamente.",
  "functions/unavailable": "Serviço temporariamente indisponível. Tente novamente em instantes.",
  "functions/deadline-exceeded": "A operação demorou mais que o esperado. Tente novamente.",
  "functions/internal": "Não foi possível concluir esta ação agora."
};

export function getFunctionErrorCode(error) {
  return error?.code || "functions/unknown";
}

export function getFunctionFriendlyMessage(error, fallback = "Não foi possível concluir esta ação agora.") {
  const code = typeof error === "string" ? error : getFunctionErrorCode(error);
  return FRIENDLY_MESSAGES[code] || fallback;
}

export async function callFunction(name, payload = {}, options = {}) {
  try {
    const callable = httpsCallable(functions, name);
    const result = await callable(payload);
    return { ok: true, data: result.data };
  } catch (error) {
    const code = getFunctionErrorCode(error);
    if (IS_DEVELOPMENT && options.silent !== true) {
      console.warn(`[HabitFlow] Function ${name} falhou`, {
        code,
        message: error?.message || null
      });
    }

    return {
      ok: false,
      error,
      code,
      message: getFunctionFriendlyMessage(error)
    };
  }
}
