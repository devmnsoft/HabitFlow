import { test, expect } from '@playwright/test';

test.describe('busca rápida autenticada', () => {
  test.skip(!process.env.HABITFLOW_TEST_EMAIL, 'Credenciais E2E não configuradas.');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/e-mail/i).fill(process.env.HABITFLOW_TEST_EMAIL);
    await page.getByLabel(/senha/i).fill(process.env.HABITFLOW_TEST_PASSWORD);
    await page.getByRole('button', { name: /entrar/i }).click();
    await expect(page).toHaveURL(/dashboard/);
  });

  test('abre com teclado, busca e devolve o foco ao fechar', async ({ page }) => {
    await page.keyboard.press(process.platform === 'darwin' ? 'Meta+K' : 'Control+K');
    const dialog = page.getByRole('dialog', { name: /onde você quer chegar/i });
    await expect(dialog).toBeVisible();
    await page.getByRole('searchbox').fill('privacidade');
    await expect(page.getByRole('option', { name: /privacidade/i })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(page.getByRole('button', { name: /buscar/i })).toBeFocused();
  });

  test('não cria overflow horizontal no mobile', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 720 });
    await page.getByRole('button', { name: /buscar/i }).click();
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(320);
  });
});
