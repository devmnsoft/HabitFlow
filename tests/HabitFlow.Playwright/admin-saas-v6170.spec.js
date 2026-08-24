import { test, expect } from '@playwright/test';

const sections = ['/admin', '/admin/users', '/admin/roles', '/admin/audit', '/admin/feature-flags', '/admin/system-health', '/admin/privacy'];
test.describe('console SaaS v6.17.0', () => {
  test.skip(!process.env.HABITFLOW_ADMIN_STORAGE, 'admin storage state required');
  test.use({ storageState: process.env.HABITFLOW_ADMIN_STORAGE });

  test('admin navigates protected operational areas without client errors', async ({ page }) => {
    const errors = [];
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('pageerror', error => errors.push(error.message));
    for (const route of sections) {
      const response = await page.goto(route);
      expect(response?.status(), route).toBeLessThan(400);
      await expect(page.locator('main, section').first()).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1), route).toBeTruthy();
    }
    expect(errors).toEqual([]);
  });

  test('mobile console has no horizontal overflow or empty shell', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/admin/system-health');
    await expect(page.getByRole('heading', { name: 'Saúde operacional' })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBeTruthy();
  });
});

test('ordinary authenticated user cannot access admin', async ({ browser }) => {
  test.skip(!process.env.HABITFLOW_MEMBER_STORAGE, 'member storage state required');
  const context = await browser.newContext({ storageState: process.env.HABITFLOW_MEMBER_STORAGE });
  const response = await context.request.get('/admin/system-health', { maxRedirects: 0 });
  expect([302, 403]).toContain(response.status());
  await context.close();
});
