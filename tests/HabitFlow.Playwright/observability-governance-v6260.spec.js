import { test, expect } from '@playwright/test';

const viewports = [
  { width: 320, height: 700 },
  { width: 375, height: 800 },
  { width: 768, height: 900 },
  { width: 1440, height: 900 }
];

async function trackErrors(page) {
  const errors = [];
  page.on('pageerror', e => errors.push(`pageerror: ${e.message}`));
  page.on('console', m => {
    if (m.type() === 'error' && !m.text().includes('ERR_NETWORK_CHANGED')) {
      errors.push(`console: ${m.text()}`);
    }
  });
  return errors;
}

test.describe('HabitFlow v6.26.0 - Observabilidade, Governança, LGPD e Release', () => {
  for (const vp of viewports) {
    test(`Páginas públicas e de autenticação sem erro em ${vp.width}px`, async ({ page }) => {
      const errors = await trackErrors(page);
      await page.setViewportSize(vp);

      // Home / Planos pública
      const plansRes = await page.goto('/plans', { waitUntil: 'domcontentloaded' });
      expect(plansRes?.status()).toBeLessThan(400);
      await expect(page.locator('body')).toBeVisible();

      // Ausência de blocos brancos vazios
      const emptyWhiteBlocks = await page.evaluate(() => {
        return [...document.querySelectorAll('main div, main section, main article')]
          .filter(el => {
            const rect = el.getBoundingClientRect();
            const style = getComputedStyle(el);
            return rect.width > 150 && rect.height > 100 &&
                   style.backgroundColor === 'rgb(255, 255, 255)' &&
                   el.innerText.trim().length === 0;
          }).length;
      });
      expect(emptyWhiteBlocks).toBe(0);

      // Login público
      const loginRes = await page.goto('/login', { waitUntil: 'domcontentloaded' });
      expect(loginRes?.status()).toBeLessThan(400);

      expect(errors).toEqual([]);
    });
  }

  test('Rotas críticas de governança e SuperAdmin bloqueiam acesso anônimo', async ({ page }) => {
    const errors = await trackErrors(page);
    const superAdminRoutes = [
      '/superadmin',
      '/superadmin/health',
      '/superadmin/audit',
      '/superadmin/lgpd',
      '/superadmin/backup',
      '/superadmin/release',
      '/superadmin/incidents'
    ];

    for (const route of superAdminRoutes) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      const currentUrl = page.url();
      // Não deve permitir acesso anônimo; deve redirecionar para login ou access denied
      expect(currentUrl).toMatch(/login|access-denied|forbidden/i);
    }

    expect(errors).toEqual([]);
  });

  test('Jornada autenticada de SuperAdmin para Observabilidade, Auditoria, LGPD, Backup e Release', async ({ page }) => {
    test.skip(!process.env.HABITFLOW_E2E_EMAIL, 'Credenciais E2E não configuradas para fluxo autenticado SuperAdmin');

    const errors = await trackErrors(page);

    // Login
    await page.goto('/login');
    await page.getByLabel(/e-mail/i).fill(process.env.HABITFLOW_E2E_EMAIL);
    await page.getByLabel(/senha/i).fill(process.env.HABITFLOW_E2E_PASSWORD);
    await page.getByRole('button', { name: /entrar/i }).click();
    await page.waitForLoadState('networkidle');

    // 1. Painel Executivo
    await page.goto('/superadmin');
    await expect(page.getByRole('heading', { name: /Painel Executivo/i })).toBeVisible();

    // 2. Health & Observabilidade
    await page.goto('/superadmin/health');
    await expect(page.getByRole('heading', { name: /Observabilidade e Health Checks/i })).toBeVisible();
    await expect(page.locator('text=Aplicação').first()).toBeVisible();
    await expect(page.locator('text=Banco de Dados').first()).toBeVisible();

    // 3. Auditoria Operacional
    await page.goto('/superadmin/audit');
    await expect(page.getByRole('heading', { name: /Auditoria Operacional/i })).toBeVisible();
    await expect(page.locator('a[href*="/superadmin/audit/export"]').first()).toBeVisible();

    // 4. Governança LGPD
    await page.goto('/superadmin/lgpd');
    await expect(page.getByRole('heading', { name: /Governança LGPD & Privacidade/i })).toBeVisible();

    // 5. Backup & Restore
    await page.goto('/superadmin/backup');
    await expect(page.getByRole('heading', { name: /Backup & Continuidade Operacional/i })).toBeVisible();
    await expect(page.locator('text=RPO Alvo').first()).toBeVisible();
    await expect(page.locator('text=RTO Alvo').first()).toBeVisible();

    // 6. Governança de Release
    await page.goto('/superadmin/release');
    await expect(page.getByRole('heading', { name: /Governança de Release/i })).toBeVisible();
    await expect(page.locator('text=v6.26.0').first()).toBeVisible();

    // 7. Gestão de Incidentes com SEV e Mitigação
    await page.goto('/superadmin/incidents');
    await expect(page.getByRole('heading', { name: /Gestão de Incidentes/i })).toBeVisible();

    expect(errors).toEqual([]);
  });
});
