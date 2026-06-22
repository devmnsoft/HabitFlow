import { auth, db, firebaseApi } from "./firebase.js";
import { APP_VERSION, APP_ENV } from "./plans.js";
import { sanitizeMetadata as legacySanitize } from "./error-monitor.js";
import { callFunction } from "./functions-client.js";
import { enqueuePendingLog, getPendingLogs, clearPendingLogs, flushPendingLogs } from "./log-queue.js";

const GLOBAL_ACTIONS = new Set(["frontend_logger_test","chatbot_sensitive_request_blocked","chatbot_unknown_question","chatbot_bug_report_started","user_bug_report","whatsapp_clicked","system_settings_loaded","system_settings_fallback_used"]);
const REMOTE_LOG_FAILURE_LIMIT = 3;
const REMOTE_LOG_DISABLE_MS = 60000;
const LOG_QUEUE_KEY = "habitflow_pending_logs";
const MAX_LOG_QUEUE_SIZE = 100;

let remoteLogFailureCount = 0;
let remoteLogDisabledUntil = 0;
let isFlushingLogs = false;
let remoteFailureWarningShownUntil = 0;
function now(){ return Date.now(); }
export function isRemoteLoggingAvailable(){ return now() >= remoteLogDisabledUntil; }
export function disableRemoteLoggingTemporarily(){ remoteLogDisabledUntil=now()+REMOTE_LOG_DISABLE_MS; }
function remoteLoggerStatus(){ return { disabledUntil: remoteLogDisabledUntil, paused: !isRemoteLoggingAvailable(), consecutiveFailures: remoteLogFailureCount, isFlushingLogs, queueKey: LOG_QUEUE_KEY, maxQueueSize: MAX_LOG_QUEUE_SIZE }; }
export function getLoggerDiagnostics(){ return { ...remoteLoggerStatus(), pendingLogs:getPendingLogs().length, lastRemoteFailure: localStorage.getItem("habitflow_last_log_failure") || "" }; }
export { getPendingLogs, clearPendingLogs, flushPendingLogs };
window.addEventListener("online",()=>flushPendingLogs().catch(()=>{}));
setInterval(()=>flushPendingLogs().catch(()=>{}),60000);
function userInfo(){ const u=auth.currentUser; return u?{userId:u.uid,userEmail:u.email||"",userName:u.displayName||""}:{}; }
export function sanitizeMetadata(metadata={}){ return legacySanitize(metadata); }
async function writeUsage(action, severity, message, metadata){ const u=auth.currentUser; if(!u) return; await firebaseApi.addDoc(firebaseApi.collection(db,"users",u.uid,"usageEvents"),{type:action,action,severity,source:"frontend",message:String(message||"").slice(0,300),metadata:sanitizeMetadata(metadata),createdAt:firebaseApi.serverTimestamp(),appVersion:APP_VERSION,environment:APP_ENV}); }
async function writeGlobal(action,severity,message,metadata){
  const payload={type:action,severity,source:"frontend",action,message:String(message||"Evento registrado.").slice(0,500),metadata:sanitizeMetadata(metadata)};
  if(!auth.currentUser || (severity==="info" && !GLOBAL_ACTIONS.has(action))) return null;
  if(!isRemoteLoggingAvailable()){ enqueuePendingLog(payload); return null; }
  const result=await callFunction("logSystemEvent", payload, { silent:true });
  if(result.ok){ remoteLogFailureCount=0; isFlushingLogs=true; flushPendingLogs().finally(()=>{ isFlushingLogs=false; }).catch(()=>{}); return result.data; }
  enqueuePendingLog(payload);
  remoteLogFailureCount++;
  localStorage.setItem("habitflow_last_log_failure", JSON.stringify({ at:new Date().toISOString(), code:result.code, action }));
  if(remoteLogFailureCount>=REMOTE_LOG_FAILURE_LIMIT) disableRemoteLoggingTemporarily();
  if(APP_ENV==="development" && now() > remoteFailureWarningShownUntil){ remoteFailureWarningShownUntil = now()+REMOTE_LOG_DISABLE_MS; console.warn("[HabitFlow] logSystemEvent falhou; logs serão mantidos localmente temporariamente", result.code); }
  return null;
}
async function log(severity, action, message, error=null, metadata={}){ try{ const payload={...metadata,...userInfo(),errorCode:error?.code||"",errorName:error?.name||"",errorMessage:error?.message||"",page:location.pathname,hash:location.hash}; if(APP_ENV==="development") console[severity==="critical"?"error":severity==="warning"?"warn":severity]("[HabitFlow]", action, message, sanitizeMetadata(payload)); await writeUsage(action,severity,message,payload).catch(()=>{}); await writeGlobal(action,severity,message,payload).catch(()=>{}); }catch(loggerError){ if(APP_ENV==="development") console.warn("[HabitFlow] logger falhou", loggerError?.message||loggerError); } }
export const logger={ info:(a,m,md={})=>log("info",a,m,null,md), warning:(a,m,md={})=>log("warning",a,m,null,md), error:(a,m,e=null,md={})=>log("error",a,m,e,md), critical:(a,m,e=null,md={})=>log("critical",a,m,e,md) };
export async function safeAsync(actionName, asyncFn, options={}){ const {successMessage,errorMessage="Não foi possível concluir esta ação.",logStart=true,logSuccess=false,logError=true,rethrow=false,toast=null,metadata={}}=options; try{ if(logStart) await logger.info(`${actionName}_started`,"Ação iniciada.",metadata); const result=await asyncFn(); if(logSuccess) await logger.info(actionName,successMessage||"Ação concluída com sucesso.",metadata); if(successMessage && toast) toast("HabitFlow",successMessage,"success"); return result; }catch(error){ if(logError) await logger.error(actionName,errorMessage,error,metadata); if(toast) toast("Ops",errorMessage,"danger"); if(rethrow) throw error; return null; } }
