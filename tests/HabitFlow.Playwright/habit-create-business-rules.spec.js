import { test, expect } from '@playwright/test';

test.describe('v6.12.2 habit create business rules', () => {
  test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'authenticated storage is required');
  test.beforeEach(async ({ page }) => { await page.goto('/habits/create'); });

  for (const [frequency, preview, target] of [['Daily', 'todos os dias', '7'], ['Weekdays', 'segunda a sexta', '5'], ['Weekends', 'sábados e domingos', '2']]) {
    test(`${frequency} accepts an omitted SelectedDays payload`, async ({ page }) => {
      await page.locator('#Name').fill(`Teste ${frequency}`);
      await page.locator('#FrequencyType').selectOption(frequency);
      await expect(page.locator('[data-schedule-preview]')).toContainText(preview);
      await expect(page.locator('#TargetPerWeek')).toHaveValue(target);
      await expect(page.locator('[data-weekdays] input').first()).toBeDisabled();
      const responsePromise = page.waitForResponse(response => response.url().includes('/habits/create') && response.request().method() === 'POST');
      await page.locator('[data-submit]').click();
      const response = await responsePromise;
      expect(response.status()).not.toBe(500);
    });
  }

  test('CustomWeekly requires days and previews Monday, Wednesday and Friday', async ({ page }) => {
    await page.locator('#FrequencyType').selectOption('CustomWeekly');
    await page.locator('[data-submit]').click();
    await expect(page.locator('#SelectedDaysError')).toContainText('Selecione pelo menos um dia');
    for (const day of ['1', '3', '5']) await page.locator(`[name="SelectedDays"][value="${day}"]`).check();
    await expect(page.locator('[data-schedule-preview]')).toContainText('segunda, quarta, sexta');
    await expect(page.locator('#TargetPerWeek')).toHaveValue('3');
  });
});
