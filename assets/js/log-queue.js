import { APP_ENV } from "./plans.js";
import { callFunction } from "./functions-client.js";

const BLOCKED_KEY = /password|senha|token|accessToken|refreshToken|authorization|apiKey|secret|card|cvv|cpf|document|payloadCompleto|rawPayload|payer|stack|raw|payload/i;
export const LOG_QUEUE_KEY = "habitflow_pending_logs";
export const MAX_LOG_QUEUE_SIZE = 100;
let isFlushingLogs = false;
let remoteLoggerReadyProvider = () => false;
let remoteLoggingAvailableProvider = () => true;

export function configureLogQueue({ isRemoteLoggerReady, isRemoteLoggingAvailable } = {}) {
  if (typeof isRemoteLoggerReady === "function") remoteLoggerReadyProvider = isRemoteLoggerReady;
  if (typeof isRemoteLoggingAvailable === "function") remoteLoggingAvailableProvider = isRemoteLoggingAvailable;
}
export function sanitizeQueuedMetadata(input = {}, depth = 0) {
  if (depth > 3 || input == null) return null;
  if (["string", "number", "boolean"].includes(typeof input)) return typeof input === "string" ? input.slice(0, 500) : input;
  if (Array.isArray(input)) return input.slice(0, 20).map((item) => sanitizeQueuedMetadata(item, depth + 1));
  if (typeof input !== "object") return String(input).slice(0, 120);
  const output = {};
  for (const [key, value] of Object.entries(input).slice(0, 40)) { if (BLOCKED_KEY.test(key)) continue; output[key] = sanitizeQueuedMetadata(value, depth + 1); }
  return JSON.parse(JSON.stringify(output).slice(0, 6000));
}
export function getPendingLogs() { try { return JSON.parse(localStorage.getItem(LOG_QUEUE_KEY) || "[]"); } catch { return []; } }
export function clearPendingLogs() { localStorage.removeItem(LOG_QUEUE_KEY); }
export function enqueuePendingLog(log) { try { const queue = getPendingLogs(); queue.push(sanitizeQueuedMetadata({ ...log, queuedAt: new Date().toISOString() })); localStorage.setItem(LOG_QUEUE_KEY, JSON.stringify(queue.slice(-MAX_LOG_QUEUE_SIZE))); } catch (error) { if (APP_ENV === "development") console.warn("[HabitFlow] fila local indisponível", error?.message || error); } }
export async function flushPendingLogs() {
  if (!remoteLoggerReadyProvider()) return { ok: false, reason: "remote_logger_not_ready", pending: getPendingLogs().length };
  if (!remoteLoggingAvailableProvider()) return { ok: false, reason: "remote_logger_paused", pending: getPendingLogs().length };
  if (isFlushingLogs) return { ok: false, skipped: true, pending: getPendingLogs().length };
  if (!navigator.onLine) return { ok: false, skipped: true, pending: getPendingLogs().length };
  const queue = getPendingLogs(); if (!queue.length) return { ok: true, sent: 0, pending: 0 };
  isFlushingLogs = true;
  try { const remaining = []; let sent = 0; for (const item of queue) { const result = await callFunction("logSystemEvent", item, { silent: true }); if (result.ok) sent += 1; else { remaining.push(item); break; } } const tail = queue.slice(sent + remaining.length); localStorage.setItem(LOG_QUEUE_KEY, JSON.stringify([...remaining, ...tail].slice(-MAX_LOG_QUEUE_SIZE))); return { ok: remaining.length === 0, sent, pending: getPendingLogs().length }; }
  finally { isFlushingLogs = false; }
}
