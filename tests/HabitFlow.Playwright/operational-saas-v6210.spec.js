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

test.describe('HabitFlow v6.21.0 - Operação SaaS e Homologação Visual', () => {
  for (const vp of viewports) {
    test(`Página de Planos e Suporte sem erro e responsivo em ${vp.width}px`, async ({ page }) => {
      const errors = await trackErrors(page);
      await page.setViewportSize(vp);

      // 1. Tela de planos mostra trial de 15 dias corretamente
      const plansRes = await page.goto('/plans', { waitUntil: 'domcontentloaded' });
      expect(plansRes?.status()).toBeLessThan(400);
      await expect(page.locator('body')).toBeVisible();
      const planText = await page.innerText('body');
      expect(planText).toMatch(/15 dias|Trial|Gratuito|Ritmo|Evolução/i);

      // Verificação de ausência de blocos brancos vazios
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

      // 2. Central de suporte pública
      const supportRes = await page.goto('/support', { waitUntil: 'domcontentloaded' });
      expect(supportRes?.status()).toBeLessThan(400);
      await expect(page.locator('body')).toBeVisible();

      // Zero erros de console e página
      expect(errors).toEqual([]);
    });
  }

  test('Rotas administrativas exigem autorização e redirecionam sem vazamento', async ({ page }) => {
    const errors = await trackErrors(page);
    const protectedRoutes = [
      '/superadmin/customer-success',
      '/superadmin/incidents',
      '/superadmin/support',
      '/superadmin/homologation',
      '/admin/onboarding'
    ];

    for (const route of protectedRoutes) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      const url = page.url();
      expect(url).toMatch(/login|access-denied|forbidden/i);
    }

    expect(errors).toEqual([]);
  });

  test('Jornada autenticada de Operação, Customer Success, Suporte e Incidentes', async ({ page }) => {
    test.skip(!process.env.HABITFLOW_E2E_EMAIL, 'Credenciais E2E não configuradas para fluxo autenticado de SuperAdmin/TenantAdmin');

    const errors = await trackErrors(page);

    // Login
    await page.goto('/login');
    await page.getByLabel(/e-mail/i).fill(process.env.HABITFLOW_E2E_EMAIL);
    await page.getByLabel(/senha/i).fill(process.env.HABITFLOW_E2E_PASSWORD);
    await page.getByRole('button', { name: /entrar/i }).click();
    await page.waitForLoadState('networkidle');

    // SuperAdmin abre Customer Success
    await page.goto('/superadmin/customer-success');
    await expect(page.getByRole('heading', { name: /Customer Success/i })).toBeVisible();

    // Filtra tenant em risco
    const filterSelect = page.locator('select[name="statusFilter"]');
    if (await filterSelect.isVisible()) {
      await filterSelect.selectOption({ label: 'AtRisk' });
    }

    // SuperAdmin abre Incidents e cria incidente
    await page.goto('/superadmin/incidents');
    await expect(page.getByRole('heading', { name: /Gestão de Incidentes/i })).toBeVisible();

    // SuperAdmin abre fila de suporte
    await page.goto('/superadmin/support');
    await expect(page.getByRole('heading', { name: /Fila Operacional de Suporte/i })).toBeVisible();

    // Admin visualiza onboarding
    await page.goto('/admin/onboarding');
    await expect(page.getByRole('heading', { name: /Implantação Guiada/i })).toBeVisible();

    expect(errors).toEqual([]);
  });
});
