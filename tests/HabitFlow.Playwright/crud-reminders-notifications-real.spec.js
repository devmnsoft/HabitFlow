import { test, expect } from '@playwright/test';

test('reminders and notifications mutations do not return 500', async ({ page }) => {
  const failures = [];
  page.on('response', response => { if (response.status() >= 500) failures.push(`${response.status()} ${response.url()}`); });
  await page.goto('/reminders');
  await expect(page.locator('body')).not.toContainText(/Npgsql|InvalidOperationException|Stack trace/i);
  await page.goto('/notifications');
  await expect(page.locator('body')).not.toContainText(/Npgsql|InvalidOperationException|Stack trace/i);
  expect(failures).toEqual([]);
});
