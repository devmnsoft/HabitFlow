import { test, expect } from '@playwright/test';

const viewports = [
  { width: 320, height: 700 },
  { width: 375, height: 800 },
  { width: 768, height: 900 },
  { width: 1440, height: 900 }
];

async function watch(page) {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  return errors;
}

test.describe('v6.19.8 ativação do produto', () => {
  test('login e planos seguem legíveis e as áreas administrativas redirecionam', async ({ page }) => {
    const errors = await watch(page);
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      const login = await page.goto('/login');
      expect(login?.status()).toBeLessThan(400);
      await expect(page.locator('body')).toBeVisible();
      const plans = await page.goto('/plans');
      expect(plans?.status()).toBeLessThan(400);
    }
    for (const path of ['/superadmin/customer-success', '/superadmin/homologation', '/admin/onboarding', '/admin/templates', '/notifications/alerts']) {
      const response = await page.goto(path);
      expect(response?.url() ?? '').toMatch(/login|access-denied|forbidden/i);
    }
    expect(errors).toEqual([]);
  });

  test('jornada autenticada fica pendente sem credencial E2E', async () => {
    test.skip(!process.env.HABITFLOW_E2E_EMAIL, 'Credenciais E2E não configuradas. Login de SuperAdmin, template e chatbot não foram executados.');
  });
});
