import { test, expect } from '@playwright/test';

test.describe('v6.12.1 global search selector compatibility', () => {
  test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'authenticated storage is required');
  test.beforeEach(async ({ page }) => { await page.goto('/dashboard'); });

  test('button and both shortcuts open one safe keyboard-accessible dialog', async ({ page }) => {
    const errors = []; page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    const button = page.locator('[data-global-search-open]').first();
    await button.click(); await expect(page.locator('#globalSearch')).toBeVisible();
    await page.keyboard.press('Escape'); await expect(button).toBeFocused();
    for (const shortcut of ['Control+K', 'Meta+K']) {
      await page.keyboard.press(shortcut); await expect(page.locator('#globalSearch')).toBeVisible();
      await page.keyboard.press('Escape');
    }
    expect(errors).toEqual([]);
  });
});
