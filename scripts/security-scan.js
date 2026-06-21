import { readdir, readFile, stat } from "node:fs/promises";
import { join, relative } from "node:path";

const distOnly = process.argv.includes("--dist-only");
const roots = distOnly ? ["dist"] : ["."];
const ignoredDirs = new Set([".git", "node_modules", ".firebase", "dist"]);
const allowedFiles = new Set(["package-lock.json", ".gitignore", ".firebaseignore", "scripts/security-scan.js"]);
const patterns = [
  [/TELEGRAM_BOT_TOKEN\s*=/i, "TELEGRAM_BOT_TOKEN atribuído"],
  [/MERCADOPAGO_ACCESS_TOKEN\s*=/i, "Mercado Pago access token"],
  [/STRIPE_SECRET_KEY\s*=/i, "Stripe secret key"],
  [/AI_API_KEY\s*=/i, "AI API key"],
  [/BEGIN PRIVATE KEY/i, "Private key"],
  [/serviceAccount|firebase-adminsdk/i, "Service account"],
  [/password\s*=|senha\s*=|secret\s*=|privateKey\s*=|authorization\s*=|bearer\s+[a-z0-9._-]+/i, "credencial potencial"],
  [/\b\d{8,12}:[A-Za-z0-9_-]{30,}\b/, "possível token real do Telegram"]
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
    if (!distOnly && (/\.md$/i.test(rel) || rel.endsWith(".env.example"))) continue;
    if (allowedFiles.has(rel) || /\.(png|jpg|jpeg|gif|webp|ico|svg|woff2?)$/i.test(rel)) continue;
    if (distOnly && rel.endsWith(".map")) findings.push(`${rel}:1 arquivo .map em dist`);
    const text = await readFile(file, "utf8").catch(() => "");
    if (distOnly && /sourceMappingURL/i.test(text)) findings.push(`${rel}:1 sourceMappingURL em dist`);
    text.split(/\r?\n/).forEach((line, index) => {
      const allowedExample = /example|exemplo|placeholder|<|>|env\(|process\.env|VITE_|documenta|configure|configur/i.test(line);
      for (const [regex, label] of patterns) {
        if (regex.test(line) && !allowedExample) findings.push(`${rel}:${index + 1} ${label}`);
      }
    });
  }
}
if (findings.length) {
  console.error("Security scan falhou:\n" + findings.join("\n"));
  process.exit(1);
}
console.log(`Security scan OK (${distOnly ? "dist" : "projeto"}).`);
