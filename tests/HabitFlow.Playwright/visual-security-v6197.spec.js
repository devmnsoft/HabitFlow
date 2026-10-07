import { test, expect } from '@playwright/test';

const widths = [320, 375, 768, 1440];

test.describe('v6.19.7 public visual security', () => {
  test.beforeEach(async ({ page }) => {
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.errors = errors;
  });

  for (const width of widths) {
    test(`plans and login stay readable at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 800 });
      await page.goto('/plans');
      await expect(page.locator('body')).toBeVisible();
      const text = await page.locator('body').innerText();
      expect(text.length).toBeGreaterThan(40);
      expect(text).toMatch(/Free|Premium|Planos/i);
      await page.screenshot({ path: `artifacts/v6197/plans-${width}.png`, fullPage: true });
      await page.goto('/login');
      await expect(page.locator('input[type="password"], input[name="password"]')).toBeVisible();
      await page.screenshot({ path: `artifacts/v6197/login-${width}.png`, fullPage: true });
      expect(page.errors).toEqual([]);
    });
  }

  test('ai admin and chatbot require a session', async ({ page }) => {
    const ai = await page.goto('/superadmin/ai');
    expect(page.url()).toMatch(/login/i);
    expect(ai.status()).toBeLessThan(500);
    const chat = await page.goto('/assistant');
    expect(page.url()).toMatch(/login/i);
    expect(chat.status()).toBeLessThan(500);
  });
});
