import { test, expect } from '@playwright/test';

const publicRoutes = ['/', '/plans', '/support'];
const authenticatedRoutes = ['/notifications/preferences', '/admin/environment', '/habits'];
const openOverlay = '.dropdown-menu.show, .modal.show, .offcanvas.show, dialog[open]';

async function assertHealthyPage(page, route) {
  const response = await page.goto(route, { waitUntil: 'networkidle' });
  expect(response?.status(), `${route} must respond successfully`).toBeLessThan(400);
  await expect(page.locator('main')).toBeVisible();
  await expect(page.locator(openOverlay), `${route} must not start with a white/empty overlay`).toHaveCount(0);
}

for (const route of publicRoutes) {
  test(`quality gate public route ${route}`, async ({ page }) => {
    await assertHealthyPage(page, route);
  });
}

for (const route of authenticatedRoutes) {
  test(`quality gate authenticated route ${route}`, async ({ page }) => {
    test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'authenticated storage state is required');
    await assertHealthyPage(page, route);
    await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
  });
}
