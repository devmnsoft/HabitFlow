import { readdirSync, readFileSync, statSync, existsSync } from "node:fs";
import { join, relative } from "node:path";
const root = "dist";
const findings = [];
if (!existsSync(root)) { console.error("dist/ não foi gerado."); process.exit(1); }
function walk(dir) { for (const ent of readdirSync(dir, { withFileTypes: true })) { const full = join(dir, ent.name); const rel = relative(process.cwd(), full); if (ent.isDirectory()) { if (["functions", "node_modules"].includes(ent.name)) findings.push(`${rel}: diretório proibido em dist`); walk(full); } else check(full, rel); } }
function check(file, rel) { if (/\.map$/i.test(rel)) findings.push(`${rel}: source map proibido`); if (/(^|\/)\.env($|\.)/.test(rel)) findings.push(`${rel}: .env proibido`); if (/(SECURITY_AUDIT|TODO_TECNICO|README)\.md$/i.test(rel)) findings.push(`${rel}: documentação interna proibida`); const text = readFileSync(file, "utf8"); for (const [regex,label] of [[/sourceMappingURL/i,"sourceMappingURL"],[/TELEGRAM_BOT_TOKEN|MERCADOPAGO_ACCESS_TOKEN|STRIPE_SECRET_KEY|AI_API_KEY|serviceAccount|BEGIN PRIVATE KEY/i,"secret marker"],[/firebase-adminsdk/i,"admin sdk key"]]) if (regex.test(text)) findings.push(`${rel}: ${label}`); }
walk(root);
if (findings.length) { console.error(`Security dist scan falhou:\n${findings.join("\n")}`); process.exit(1); }
console.log("Security dist scan OK.");
