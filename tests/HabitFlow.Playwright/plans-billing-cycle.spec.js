import { test, expect } from '@playwright/test';

test('Ritmo exposes monthly/yearly truthfully and preserves visitor intent', async ({ page }) => {
  await page.goto('/plans');
  const ritmo = page.locator('#ritmo');
  await expect(ritmo).toContainText('R$ 19,90/mês');
  await page.getByRole('button', { name: 'Anual' }).click();
  await expect(ritmo).toContainText('R$ 199,00/ano');
  await expect(ritmo.locator('[data-plan-cta="ritmo"]')).toHaveAttribute('href', /intent=ritmo&cycle=Yearly/);
  await expect(page.locator('#evolucao')).toHaveCount(0);
  for (const forbidden of ['Relatórios avançados', 'Rotinas compartilhadas', 'Suporte prioritário']) await expect(page.locator('body')).not.toContainText(forbidden);
});
