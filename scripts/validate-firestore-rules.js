import { readFileSync } from "node:fs";
const file = "firestore.rules";
const text = readFileSync(file, "utf8");
const findings = [];
const dangerous = [
  [/allow\s+read\s*,\s*write\s*:\s*if\s+true/i, "allow read/write público"],
  [/allow\s+read\s*:\s*if\s+true/i, "allow read público"],
  [/allow\s+write\s*:\s*if\s+true/i, "allow write público"],
  [/request\.auth\s*==\s*null[\s\S]{0,80}allow/i, "permissão para usuário anônimo"],
];
for (const [regex, label] of dangerous) if (regex.test(text)) findings.push(label);
for (const collection of ["systemAuditLogs", "adminAuditLogs", "billingEvents", "appMetrics", "securityIncidents", "supportTickets"]) {
  const line = text.split(/\r?\n/).find((l) => l.includes(`match /${collection}/`)) || "";
  if (!/allow\s+read\s*,\s*write\s*:\s*if\s*false/.test(line)) findings.push(`${collection} deve negar read/write client`);
}
const required = [
  [/match\s+\/users\/\{userId\}\/\{document=\*\*\}/, "match /users/{userId}/{document=**}"],
  [/request\.auth\s*!=\s*null/, "request.auth != null"],
  [/request\.auth\.uid\s*==\s*userId/, "request.auth.uid == userId"]
];
for (const [regex, label] of required) if (!regex.test(text)) findings.push(`ausente: ${label}`);
if (findings.length) { console.error(`Firestore rules inseguras:\n- ${findings.join("\n- ")}`); process.exit(1); }
console.log("Firestore rules validation OK.");
