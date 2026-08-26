import { test, expect } from '@playwright/test';

for (const width of [320, 375, 768, 1440]) {
  test(`planos comerciais seguros em ${width}px`, async ({ page }) => {
    const consoleErrors = [];
    const pageErrors = [];
    page.on('console', message => message.type() === 'error' && consoleErrors.push(message.text()));
    page.on('pageerror', error => pageErrors.push(error.message));
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/plans');
    await expect(page.getByRole('heading', { name: /escolha o plano/i })).toBeVisible();
    await expect(page.getByTestId('checkout-unavailable')).toContainText('Checkout online indisponível');
    await expect(page.getByRole('link', { name: /Falar com a MNSOFT/i }).first()).toHaveAttribute('href', /mailto:comercial@mnsoft.com.br/);
    await expect(page.locator('body')).not.toContainText(/undefined|null/i);
    expect(consoleErrors).toEqual([]);
    expect(pageErrors).toEqual([]);
  });
}

test('minha assinatura e administração exigem autenticação', async ({ page }) => {
  await page.goto('/billing');
  await expect(page).toHaveURL(/login/i);
  await page.goto('/admin/finance');
  await expect(page).toHaveURL(/login|access-denied/i);
});
