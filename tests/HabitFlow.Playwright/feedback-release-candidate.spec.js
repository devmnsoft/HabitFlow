import { test, expect } from '@playwright/test';

test.describe('release candidate feedback primitives', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await expect.poll(() => page.evaluate(() => Boolean(window.HabitFlowFeedback))).toBe(true);
  });

  test('toast is safe, announced, and does not execute markup', async ({ page }) => {
    const payload = '<img src=x onerror=alert(1)>'; let dialogs = 0;
    page.on('dialog', dialog => { dialogs++; return dialog.dismiss(); });
    await page.evaluate(message => window.HabitFlowFeedback.show({ severity: 'success', title: 'Salvo', message }), payload);
    const toast = page.locator('#hfToastHost .hf-toast').last();
    await expect(toast).toBeVisible(); await expect(toast).toHaveAttribute('role', 'status');
    await expect(toast).toContainText(payload); await expect(toast.locator('img')).toHaveCount(0); expect(dialogs).toBe(0);
  });

  test('destructive confirmation traps focus and Escape cancels', async ({ page }) => {
    const result = page.evaluate(() => window.HabitFlowFeedback.confirm({ title: 'Excluir?', message: 'Esta ação não pode ser desfeita.', destructive: true }));
    const modal = page.locator('#hfConfirmationDialog'); await expect(modal).toBeVisible();
    await expect(modal).toHaveAttribute('role', 'dialog');
    await expect(modal.locator('[data-confirm-submit]')).toHaveClass(/btn-danger/);
    await page.keyboard.press('Escape'); await expect(result).resolves.toBe(false); await expect(modal).toBeHidden();
  });

  test('native blocking dialogs are not used by feedback actions', async ({ page }) => {
    const nativeCalls = await page.evaluate(async () => {
      const calls = []; window.alert = () => calls.push('alert'); window.confirm = () => (calls.push('confirm'), false); window.prompt = () => (calls.push('prompt'), null);
      window.HabitFlowFeedback.show({ severity: 'error', title: 'Erro', message: 'Tente novamente.' });
      const confirmation = window.HabitFlowFeedback.confirm({ title: 'Confirmar', message: 'Continuar?' });
      document.querySelector('#hfConfirmationDialog [data-confirm-cancel]').click(); await confirmation; return calls;
    });
    expect(nativeCalls).toEqual([]);
  });
});
