#!/usr/bin/env node
import fs from 'node:fs';

const source = fs.readFileSync('functions/index.js', 'utf8');
const internalCallable = [
  'getPublicSystemSettings',
  'logSystemEvent',
  'getMySupportTickets',
  'healthCheck',
  'sendTestTelegramAlert',
  'askHabitFlowAssistant',
  'createSupportTicket',
  'getAdminDashboardSummary',
  'getAdminRecentLogs',
  'getAdminErrorLogs',
  'getProductionReadinessStatus'
];
let failed = false;
const report = [];
function findExport(name){
  const re = new RegExp(`exports\\.${name}\\s*=\\s*([^\\n;]+)`, 'm');
  const match = source.match(re);
  return match ? match[0] : '';
}
for (const name of internalCallable) {
  const line = findExport(name);
  if (!line) { failed = true; report.push(`❌ ${name}: export não encontrado`); continue; }
  if (/onRequest|https\.onRequest/.test(line)) { failed = true; report.push(`❌ ${name}: usa onRequest, deve ser callable/onCall`); continue; }
  if (/onCall|https\.onCall/.test(line)) report.push(`✅ ${name}: callable/onCall`);
  else { failed = true; report.push(`❌ ${name}: formato não identificado`); }
}
const paymentWebhook = findExport('paymentWebhook');
if (!paymentWebhook) { failed = true; report.push('❌ paymentWebhook: export não encontrado'); }
else if (/onRequest|https\.onRequest/.test(paymentWebhook)) report.push('✅ paymentWebhook: HTTP/onRequest preservado');
else { failed = true; report.push('⚠️ paymentWebhook: não parece onRequest'); }
console.log('HabitFlow Functions shape report');
console.log(report.join('\n'));
if (failed) process.exit(1);
