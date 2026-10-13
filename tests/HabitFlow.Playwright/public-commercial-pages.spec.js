import { test, expect } from '@playwright/test';

test('privacy is complete, navigable and responsive', async ({ page }) => {
  await page.goto('/privacy'); await expect(page).toHaveURL(/privacy/); await expect(page.getByRole('heading',{name:/Seus dados|Pol\u00edtica de Privacidade/i}).first()).toBeVisible();
  await page.getByRole('link',{name:'Seus direitos'}).click(); await expect(page.locator('#direitos')).toBeInViewport();
  await page.getByRole('button',{name:'O que s\u00e3o dados de uso?'}).click(); await expect(page.getByRole('dialog')).toBeVisible(); await page.keyboard.press('Escape');
  await expect(page.locator('body')).not.toHaveCSS('overflow-x','scroll');
});

test('plans commercial journey only offers eligible plans', async ({ page }) => {
  await page.goto('/plans'); await expect(page.getByRole('heading',{name:'Escolha o plano que combina com o seu ritmo.'})).toBeVisible();
  await expect(page.getByText('Evolu\u00e7\u00e3o',{exact:true})).toHaveCount(0);
  await page.getByRole('link',{name:'Comparar recursos'}).click(); await expect(page.locator('#comparacao')).toBeInViewport();
  await page.getByRole('button',{name:'Posso come\u00e7ar gr\u00e1tis?'}).click(); await expect(page.getByText(/15 dias/).first()).toBeVisible();
  await expect(page.getByRole('link',{name:'Come\u00e7ar 15 dias gr\u00e1tis'}).first()).toHaveAttribute('href','/register');
});

test('mobile public menu has the concise product journey', async ({ page }) => {
  await page.setViewportSize({width:390,height:844}); await page.goto('/', { waitUntil: 'networkidle' }); await page.getByRole('button',{name:'Abrir menu principal'}).click();
  const drawer = page.locator('#headerDrawer');
  await expect(drawer).toHaveClass(/show/);
  for (const label of ['In\u00edcio','Como funciona','Biblioteca','Planos','Privacidade','Ajuda']) await expect(drawer.getByRole('link',{name:new RegExp(label)})).toBeVisible();
});
