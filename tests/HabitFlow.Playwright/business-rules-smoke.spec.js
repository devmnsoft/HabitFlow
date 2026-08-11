import { test, expect } from '@playwright/test';

test.describe('v6.12.2 business rule smoke', () => {
  test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'authenticated storage is required');
  test('habit editor, global search and plans remain available', async ({ page }) => {
    const serverErrors = []; page.on('response', response => { if (response.status() >= 500) serverErrors.push(response.url()); });
    await page.goto('/habits/create'); await expect(page.locator('[data-habit-editor]')).toBeVisible();
    await page.goto('/dashboard'); await page.locator('[data-global-search-open]').first().click(); await expect(page.locator('#globalSearch')).toBeVisible();
    await page.goto('/plans'); await expect(page.locator('body')).not.toContainText('Contratar Evolução');
    expect(serverErrors).toEqual([]);
  });
});
