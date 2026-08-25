import { test, expect } from '@playwright/test';

test.describe('Planos v6.17.4', () => {
  test('visitor sees a commercial, honest and actionable page', async ({ page }) => {
    const consoleErrors=[]; const pageErrors=[];
    page.on('console', message => { if(message.type()==='error') consoleErrors.push(message.text()); });
    page.on('pageerror', error => pageErrors.push(error.message));
    await page.goto('/plans');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Escolha o plano');
    await expect(page.locator('#free')).toBeVisible();
    await expect(page.locator('#ritmo-monthly')).toContainText('R$ 19,90/mês');
    await expect(page.locator('#ritmo-yearly')).toContainText('R$ 199,00/ano');
    await expect(page.getByRole('heading', { name: 'Compare todos os recursos' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Perguntas frequentes' })).toBeVisible();
    await expect(page.locator('#ritmo-monthly [data-plan-cta="ritmo"]')).toHaveAttribute('href', /\/login\?returnUrl=/);
    await expect(page.locator('#free [data-plan-cta="free"]')).toHaveAttribute('href', '/register');
    expect(consoleErrors).toEqual([]); expect(pageErrors).toEqual([]);
  });

  for (const width of [320,360,390,430,768,1024,1366,1440]) test(`has no horizontal overflow at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 }); await page.goto('/plans');
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  });
});
