const crypto = require("node:crypto");
const functionsLogger = require("firebase-functions/logger");
const BLOCKED_KEY = /password|senha|token|authorization|apiKey|secret|card|cvv|cpf|document|payloadCompleto|rawPayload|payer|stack|raw|payload/i;
function truncate(value, max = 500) { return String(value ?? "").slice(0, max); }
function sanitizeMetadata(input = {}, depth = 0) { if (depth > 3 || input == null) return null; if (["string","number","boolean"].includes(typeof input)) return typeof input === "string" ? truncate(input) : input; if (Array.isArray(input)) return input.slice(0,20).map((i)=>sanitizeMetadata(i, depth+1)); if (typeof input !== "object") return truncate(input,120); const out={}; for (const [k,v] of Object.entries(input).slice(0,40)) if(!BLOCKED_KEY.test(k)) out[k]=sanitizeMetadata(v, depth+1); return JSON.parse(JSON.stringify(out).slice(0,6000)); }
function getErrorDetails(error) { return sanitizeMetadata({ errorCode: error?.code || "", errorName: error?.name || "Error", errorMessage: error?.message || "", errorFingerprint: crypto.createHash("sha256").update(`${error?.name}|${error?.code}|${error?.message}`).digest("hex").slice(0,16) }); }
async function writeSystemAuditLog(event) { functionsLogger.info("writeSystemAuditLog hook", sanitizeMetadata(event)); return null; }
async function notifyIfNeeded(event) { functionsLogger.info("notifyIfNeeded hook", sanitizeMetadata(event)); return false; }
function logInfo(event){ functionsLogger.info(event?.message || event?.action || "info", sanitizeMetadata(event)); }
function logWarning(event){ functionsLogger.warn(event?.message || event?.action || "warning", sanitizeMetadata(event)); }
function logError(event){ functionsLogger.error(event?.message || event?.action || "error", sanitizeMetadata(event)); }
function logCritical(event){ functionsLogger.error(event?.message || event?.action || "critical", sanitizeMetadata(event)); }
module.exports = { logInfo, logWarning, logError, logCritical, writeSystemAuditLog, notifyIfNeeded, sanitizeMetadata, getErrorDetails };
