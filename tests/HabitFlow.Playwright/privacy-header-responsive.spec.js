import { test, expect } from '@playwright/test';

test('header remains usable without horizontal overflow at every supported viewport', async ({ page }) => {
  await page.goto('/privacy');
  await expect(page.locator('.app-header-v2')).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  const drawerButton = page.locator('[data-bs-target="#appHeaderDrawer"]');
  if (await drawerButton.isVisible()) {
    await drawerButton.click();
    await expect(page.locator('#appHeaderDrawer')).toBeVisible();
    await page.keyboard.press('Escape');
  }
});

test('account privacy is protected rather than missing', async ({ page }) => {
  const response = await page.goto('/account/privacy');
  expect(response?.status()).not.toBe(404);
  expect(response?.status()).toBeLessThan(500);
  await expect(page).toHaveURL(/\/account\/privacy|\/login/);
});
