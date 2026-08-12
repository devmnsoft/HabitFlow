import { test, expect } from '@playwright/test';

for (const route of ['/habits', '/goals', '/reminders', '/notifications', '/account/privacy']) {
  test(`${route} has no technical exception`, async ({ page }) => {
    const response = await page.goto(route);
    expect(response.status()).toBeLessThan(500);
    await expect(page.locator('body')).not.toContainText(/Npgsql\.|Dapper\.|InvalidOperationException|Stack trace/i);
  });
}
