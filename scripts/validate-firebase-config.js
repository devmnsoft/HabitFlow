import { readFileSync } from "node:fs";
const cfg = JSON.parse(readFileSync("firebase.json", "utf8"));
const findings = [];
const hosting = cfg.hosting || {};
const ignore = hosting.ignore || [];
const ignoreText = ignore.join("\n");
if (hosting.public !== "dist") findings.push('hosting.public deve ser "dist"');
if ([".", "./", ""].includes(String(hosting.public || ""))) findings.push("hosting.public não pode publicar a raiz");
for (const required of ["node_modules", "functions", ".env", ".git"]) if (!ignoreText.includes(required)) findings.push(`hosting.ignore deve conter ${required}`);
if (!JSON.stringify(hosting.rewrites || []).includes("/index.html")) findings.push("rewrite para /index.html ausente");
const headers = JSON.stringify(hosting.headers || []);
for (const key of ["Content-Security-Policy", "X-Frame-Options", "X-Content-Type-Options", "Strict-Transport-Security"]) if (!headers.includes(key)) findings.push(`header de segurança ausente: ${key}`);
if (cfg.firestore?.rules !== "firestore.rules") findings.push("firestore.rules deve apontar para firestore.rules");
const functionsSource = Array.isArray(cfg.functions) ? cfg.functions[0]?.source : cfg.functions?.source;
if (functionsSource !== "functions") findings.push('functions.source deve ser "functions"');
if (findings.length) { console.error(`firebase.json inválido:\n- ${findings.join("\n- ")}`); process.exit(1); }
console.log("Firebase config validation OK.");
