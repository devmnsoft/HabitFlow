import { test, expect } from '@playwright/test';

test.describe('v6.12.2 habit quick templates', () => {
  test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'authenticated storage is required');
  test('all five templates fill the editor without console errors', async ({ page }) => {
    const errors = []; page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    await page.goto('/habits/create');
    for (const name of ['Beber água', 'Caminhar', 'Ler 10 minutos', 'Dormir mais cedo', 'Organizar o dia']) {
      await page.getByRole('button', { name }).click();
      await expect(page.locator('#Name')).toHaveValue(name);
      await expect(page.locator('[data-schedule-preview]')).not.toBeEmpty();
    }
    expect(errors).toEqual([]);
  });
});
