import { readdir, readFile, stat } from "node:fs/promises";
import { join, relative } from "node:path";

const distOnly = process.argv.includes("--dist-only");
const roots = distOnly ? ["dist"] : ["."];
const ignoredDirs = new Set([".git", "node_modules", ".firebase", "dist"]);
const allowedFiles = new Set(["package-lock.json", ".gitignore", ".firebaseignore", "scripts/security-scan.js", "scripts/security-dist-scan.js"]);
// Reviewed false positives. Keep this list file- and rule-specific: adding a
// directory or suppressing every rule would hide newly committed credentials.
const reviewedFindings = new Map([
  [".github/workflows/dotnet-ci.yml|SEC_CONNECTION_PASSWORD", "ephemeral PostgreSQL CI fixture"],
  ["docker-compose.yml|SEC_CONNECTION_PASSWORD", "local-only PostgreSQL fixture"],
  ["docs/AUDITORIA_PLANTAOPRO_DB_TEMPLATE.md|SEC_CONNECTION_PASSWORD", "historical redacted documentation"],
  ["scripts/publisher/publisher.config.example.json|SEC_PRIVATE_KEY", "empty publisher example field"],
  ["src/HabitFlow.Web/appsettings.Development.json|SEC_CONNECTION_PASSWORD", "local PostgreSQL fixture"],
  ["src/HabitFlow.Web/appsettings.Development.local.example.json|SEC_CONNECTION_PASSWORD", "invalid local example"],
  ["src/HabitFlow.Web/appsettings.Docker.json|SEC_CONNECTION_PASSWORD", "local PostgreSQL fixture"],
  ["src/HabitFlow.Web/appsettings.Production.example.json|SEC_CONNECTION_PASSWORD", "invalid production example"],
  ["src/HabitFlow.Web/appsettings.json|SEC_CONNECTION_PASSWORD", "legacy local default scheduled for removal"],
  ["tests/HabitFlow.Tests/WindowsOperationsTests.cs|SEC_CONNECTION_PASSWORD", "assertion fixture"],
  [".github/workflows/dotnet-ci.yml|credencial potencial", "ephemeral PostgreSQL CI fixture"],
  ["docker-compose.yml|credencial potencial", "local-only PostgreSQL fixture"],
  ["docs/AUDITORIA_PLANTAOPRO_DB_TEMPLATE.md|credencial potencial", "historical redacted documentation"],
  ["scripts/database/check-postgres-connection.ps1|credencial potencial", "PowerShell parameter name; no value"],
  ["scripts/publisher/publisher.config.example.json|Service account", "invalid publisher example"],
  ["scripts/publisher/publisher.config.example.json|Private key", "empty publisher example field"],
  ["src/HabitFlow.Application/Services/MercadoPagoService.cs|credencial potencial", "reads configuration and HTTP headers; no value"],
  ["src/HabitFlow.Web/appsettings.Development.json|credencial potencial", "local PostgreSQL fixture"],
  ["src/HabitFlow.Web/appsettings.Development.local.example.json|credencial potencial", "invalid local example"],
  ["src/HabitFlow.Web/appsettings.Docker.json|credencial potencial", "local PostgreSQL fixture"],
  ["src/HabitFlow.Web/appsettings.Production.example.json|credencial potencial", "invalid production example"],
  ["src/HabitFlow.Web/appsettings.json|credencial potencial", "legacy local default scheduled for removal"],
  ["tests/HabitFlow.Tests/WindowsOperationsTests.cs|credencial potencial", "assertion fixture"],
]);
// Rules require a value, rather than merely a security-related identifier.
// Thus PasswordHash properties, SQL column names, DTOs and method signatures
// remain valid while assignments containing fixed credentials are rejected.
const patterns = [
  [/\b(TELEGRAM_BOT_TOKEN|MERCADOPAGO_ACCESS_TOKEN|STRIPE_SECRET_KEY|AI_API_KEY)\s*[:=]\s*["'][^"']{8,}["']/i, "SEC_TOKEN_LITERAL", "token fixo"],
  [/BEGIN (RSA |EC )?PRIVATE KEY/i, "SEC_PRIVATE_KEY", "chave privada"],
  [/\b(password|senha|smtp[_-]?secret|client[_-]?secret|privateKey)\s*[:=]\s*["'][^"'{}$@<][^"']{5,}["']/i, "SEC_PASSWORD_LITERAL", "senha ou segredo literal"],
  [/\bpassword_hash\s*=\s*["'][^"'@:][^"']+["']/i, "SEC_HASH_LITERAL", "hash literal"],
  [/\b(Host|Server)=[^;\r\n]+;[^\r\n]*(Password|Pwd)=[^;${<\r\n][^;\r\n]*/i, "SEC_CONNECTION_PASSWORD", "connection string com senha"],
  [/--password(?:=|\s+)\S+/i, "SEC_CLI_PASSWORD", "senha em argumento de CLI"],
  [/\bbearer\s+[A-Za-z0-9._-]{20,}/i, "SEC_BEARER_LITERAL", "token bearer fixo"],
  [/\b\d{8,12}:[A-Za-z0-9_-]{30,}\b/, "SEC_TELEGRAM_TOKEN", "possível token real do Telegram"]
];

async function exists(path) { try { await stat(path); return true; } catch { return false; } }
async function walk(dir) {
  if (!(await exists(dir))) return [];
  const out = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    if (entry.isDirectory() && ignoredDirs.has(entry.name) && !(distOnly && entry.name === "dist")) continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...await walk(full));
    else out.push(full);
  }
  return out;
}

let findings = [];
for (const root of roots) {
  for (const file of await walk(root)) {
    const rel = relative(process.cwd(), file);
    if (!distOnly && /(^|\/)\.runtimeconfig\.json$/i.test(rel)) findings.push(`${rel}:1 .runtimeconfig.json versionado`);
    if (!distOnly && /(^|\/)\.env($|\.)/i.test(rel) && !/\.env(\.[^.]+)?\.example$/i.test(rel)) findings.push(`${rel}:1 .env versionado`);
    if (allowedFiles.has(rel) || /\.(png|jpg|jpeg|gif|webp|ico|svg|woff2?)$/i.test(rel)) continue;
    if (distOnly && rel.endsWith(".map")) findings.push(`${rel}:1 arquivo .map em dist`);
    const text = await readFile(file, "utf8").catch(() => "");
    if (distOnly && /sourceMappingURL/i.test(text)) findings.push(`${rel}:1 sourceMappingURL em dist`);
    text.split(/\r?\n/).forEach((line, index) => {
      const allowedExample = /example|exemplo|placeholder|<|>|env\(|process\.env|VITE_|documenta|rg -n|configure|configur|proibido|ausente|secret marker|patterns|regex|label|api key|Read\(["'].*senha/i.test(line) || /^[\s`*>-]*(TELEGRAM_BOT_TOKEN|MERCADOPAGO_ACCESS_TOKEN|STRIPE_SECRET_KEY|AI_API_KEY|.*SECRET)\s*=\s*[`\s]*$/.test(line);
      for (const [regex, reason, label] of patterns) {
        if (regex.test(line) && !allowedExample && !reviewedFindings.has(`${rel}|${reason}`)) findings.push(`${rel}:${index + 1} [${reason}] ${label}`);
      }
    });
  }
}
if (findings.length) {
  console.error("Security scan falhou:\n" + findings.join("\n"));
  process.exit(1);
}
console.log(`Security scan OK (${distOnly ? "dist" : "projeto"}).`);
