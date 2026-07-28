import { test, expect } from '@playwright/test';
import { monitorPage, expectStableLayout } from './helpers/layout.js';
const routes = ['/', '/demo', '/habit-library', '/plans', '/help', '/login', '/register'];
for (const route of routes) test(`visitante: ${route}`, async ({ page }, testInfo) => {
  const errors = monitorPage(page);
  const response = await page.goto(route, { waitUntil: 'networkidle' });
  expect(response?.status()).toBeLessThan(400);
  await expect(page.locator('body')).toHaveAttribute('data-navigation-context', 'public');
  await expect(page.locator('h1').first()).toBeVisible();
  await expect(page.locator('[data-footer-context="public"]')).toBeVisible();
  await expectStableLayout(page);
  await page.screenshot({ path: testInfo.outputPath(`${route.replaceAll('/', '-') || 'home'}.png`), fullPage: true });
  expect(errors).toEqual([]);
});
