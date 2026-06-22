import { getFunctions, httpsCallable } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-functions.js";
import { app } from "./firebase.js";
import { IS_DEVELOPMENT } from "./env.js";

export const functions = getFunctions(app, "us-central1");

export async function callFunction(name, payload = {}, options = {}) {
  try {
    const fn = httpsCallable(functions, name);
    const result = await fn(payload);

    return {
      ok: true,
      data: result.data
    };
  } catch (error) {
    if (options.silent !== true && IS_DEVELOPMENT) {
      console.warn(`[HabitFlow] Function ${name} falhou`, {
        code: error?.code || null,
        message: error?.message || null
      });
    }

    return {
      ok: false,
      error,
      code: getFunctionErrorCode(error),
      message: error?.message || "Falha na Function."
    };
  }
}

export function getFunctionErrorCode(error) {
  return error?.code || "functions/unknown";
}

export function getFunctionFriendlyMessage(error, fallback = "Não foi possível concluir esta ação agora.") {
  const code = getFunctionErrorCode(error);

  const messages = {
    "functions/unauthenticated": "Sua sessão expirou. Faça login novamente.",
    "functions/permission-denied": "Você não tem permissão para executar esta ação.",
    "functions/unavailable": "Serviço temporariamente indisponível. Tente novamente em instantes.",
    "functions/deadline-exceeded": "A operação demorou mais que o esperado. Tente novamente.",
    "functions/resource-exhausted": "Muitas tentativas em pouco tempo. Aguarde um instante.",
    "functions/internal": "Não foi possível concluir esta ação agora."
  };

  return messages[code] || fallback;
}
