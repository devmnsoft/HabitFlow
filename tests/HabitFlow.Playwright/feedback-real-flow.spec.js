import { test, expect } from '@playwright/test';

test.describe('real global feedback contract', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await expect.poll(() => page.evaluate(() => Boolean(window.HabitFlowFeedback))).toBe(true);
  });

  test('shows safe, accessible toast content', async ({ page }) => {
    const payload = '<img src=x onerror="window.__unsafe=true">';
    await page.evaluate(message => window.HabitFlowFeedback.show({ severity: 'success', title: 'Alteração salva', message }), payload);
    const toast = page.locator('#hfToastHost .hf-toast').last();
    await expect(toast).toBeVisible();
    await expect(toast).toHaveAttribute('role', 'status');
    await expect(toast.locator('p')).toHaveText(payload);
    await expect(toast.locator('img')).toHaveCount(0);
    expect(await page.evaluate(() => window.__unsafe)).toBeUndefined();
  });

  test('confirmation is keyboard accessible and resolves false on Escape', async ({ page }) => {
    const result = page.evaluate(() => window.HabitFlowFeedback.confirm({ title: 'Excluir lembrete?', message: 'Esta ação não pode ser desfeita.', destructive: true }));
    const dialog = page.locator('#hfConfirmationDialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('[data-confirm-submit]')).toHaveClass(/btn-danger/);
    await page.keyboard.press('Escape');
    await expect(result).resolves.toBe(false);
    await expect(dialog).toBeHidden();
  });
});
