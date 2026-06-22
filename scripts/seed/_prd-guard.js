export function requirePrdConfirmation() {
  if (!process.argv.includes('--confirm-prd')) throw new Error('Use --confirm-prd para confirmar execução em PRD.');
}
export function requireSmokeEnabled() {
  requirePrdConfirmation();
  if (process.env.ALLOW_PRD_SMOKE_DATA !== 'true') throw new Error('Defina ALLOW_PRD_SMOKE_DATA=true para dados smoke em PRD.');
  if (!process.env.PRD_SMOKE_TEST_UID) throw new Error('Defina PRD_SMOKE_TEST_UID com o UID exclusivo de smoke test.');
}
export function getRunId() { return process.env.SMOKE_RUN_ID || `smoke-${new Date().toISOString().replace(/[:.]/g, '-')}`; }
