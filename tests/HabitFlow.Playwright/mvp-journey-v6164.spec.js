import { test, expect } from '@playwright/test';

const viewports = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 768, height: 1024 },
  { width: 390, height: 844 },
  { width: 320, height: 568 }
];

test.beforeAll(() => {
  if (!process.env.HABITFLOW_AUTH_STORAGE)
    throw new Error('HABITFLOW_AUTH_STORAGE is required for the real authenticated MVP journey.');
});

for (const viewport of viewports) {
  test(`main MVP screens remain usable at ${viewport.width}x${viewport.height}`, async ({ page }) => {
    const clientErrors = [];
    page.on('console', message => message.type() === 'error' && clientErrors.push(message.text()));
    page.on('pageerror', error => clientErrors.push(error.message));
    await page.setViewportSize(viewport);

    for (const route of ['/dashboard', '/my-day', '/habits', '/reminders', '/notifications', '/plans']) {
      const response = await page.goto(route, { waitUntil: 'networkidle' });
      expect(response?.status(), `${route} must load without a server error`).toBeLessThan(500);
      await expect(page.locator('body')).not.toContainText(/Stack trace|InvalidOperationException|NpgsqlException/i);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1),
        `${route} must not overflow horizontally`).toBeTruthy();
    }
    expect(clientErrors).toEqual([]);
  });
}

test('My Day time mutation is protected by a rendered antiforgery token', async ({ page }) => {
  await page.goto('/my-day', { waitUntil: 'networkidle' });
  const menu = page.locator('.routine-menu').first();
  test.skip(await menu.count() === 0, 'The authenticated test account has no habit scheduled today.');
  await menu.getByRole('button', { name: /Mais ações/i }).click();
  const form = menu.locator('form[action$="/time"]');
  await expect(form.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);
  await expect(form.locator('input[name="preferredTime"]')).toBeVisible();
});
