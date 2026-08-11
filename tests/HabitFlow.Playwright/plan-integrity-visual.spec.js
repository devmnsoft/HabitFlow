import { test, expect } from '@playwright/test';

test('public plans header stays commercial and layout does not overflow', async ({ page }) => {
  await page.goto('/plans');
  await expect(page.locator('[data-header-context="public"] [data-global-search-open]')).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
  await expect(page.locator('#ritmo')).toBeVisible();
  await expect(page.locator('#evolucao')).toHaveCount(0);
});
