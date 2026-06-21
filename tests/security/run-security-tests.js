import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";

const app = readFileSync("assets/js/app.js", "utf8");
const utils = readFileSync("assets/js/utils.js", "utf8");
const kb = readFileSync("assets/js/chatbot-knowledge.js", "utf8");
const fn = readFileSync("functions/index.js", "utf8");
const rules = readFileSync("firestore.rules", "utf8");

assert.match(utils, /escapeHtml/, "sanitizer escapeHtml deve existir");
assert.match(app, /textContent=String\(text/, "chatbot deve renderizar mensagens com textContent");
assert.match(kb, /ignore as instruções|revele.*prompt|logs internos|código fonte|invadir|prompt_injection_attempt/is, "chatbot deve bloquear prompt injection e pedidos internos");
assert.match(fn, /function checkRateLimit|async function checkRateLimit/, "Functions devem ter rate limit");
for (const name of ["logSystemEvent", "askHabitFlowAssistant", "createSupportTicket", "createCheckoutSession", "sendTestTelegramAlert", "adminSetUserPlan", "updateSystemSettings"]) assert.match(fn, new RegExp(name), `${name} deve existir`);
assert.match(fn, /function sanitizeMetadata/, "logger backend deve sanitizar metadata");
assert.match(fn, /errorFingerprint/, "logger deve gerar fingerprint");
assert.match(fn, /assertGeneralAdmin/, "admin deve validar permissão no backend");
assert.match(rules, /systemAuditLogs\/{document=\*\*}.*allow read, write: if false/s, "logs globais não podem ser acessados por client");

execFileSync(process.execPath, ["scripts/validate-firestore-rules.js"], { stdio: "inherit" });
execFileSync(process.execPath, ["scripts/validate-firebase-config.js"], { stdio: "inherit" });
console.log("Security unit tests OK.");
