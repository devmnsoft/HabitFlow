import { test, expect } from '@playwright/test';

test.describe('Meu Dia v6.18.3', () => {
  for (const width of [320, 375, 768, 1440]) {
    test(`central premium responsiva em ${width}px`, async ({ page }) => {
      const consoleErrors = [];
      const pageErrors = [];
      page.on('console', message => message.type() === 'error' && consoleErrors.push(message.text()));
      page.on('pageerror', error => pageErrors.push(error.message));
      await page.setViewportSize({ width, height: 900 });
      await page.goto('/my-day');
      if (page.url().includes('/auth/login')) return;
      await expect(page.locator('[data-my-day]')).toBeVisible();
      await expect(page.getByText('Sugestão inteligente')).toBeVisible();
      await expect(page.getByRole('link', { name: 'Criar novo hábito' })).toBeVisible();
      await expect(page.locator('body')).not.toHaveCSS('overflow-x', 'scroll');
      expect(consoleErrors).toEqual([]);
      expect(pageErrors).toEqual([]);
    });
  }
});
