import { test, expect } from '@playwright/test';

test('privacy is complete, navigable and responsive', async ({ page }) => {
  await page.goto('/privacy'); await expect(page).toHaveURL(/privacy/); await expect(page.getByRole('heading',{name:/Seus dados|Política de Privacidade/i}).first()).toBeVisible();
  await page.getByRole('link',{name:'Seus direitos'}).click(); await expect(page.locator('#direitos')).toBeInViewport();
  await page.getByRole('button',{name:'O que são dados de uso?'}).click(); await expect(page.getByRole('dialog')).toBeVisible(); await page.keyboard.press('Escape');
  await expect(page.locator('body')).not.toHaveCSS('overflow-x','scroll');
});

test('plans commercial journey only offers eligible plans', async ({ page }) => {
  await page.goto('/plans'); await expect(page.getByRole('heading',{name:'Construa uma rotina que você consegue manter.'})).toBeVisible();
  await expect(page.getByText('Evolução',{exact:true})).toHaveCount(0); await page.getByRole('button',{name:'Anual'}).click();
  await page.getByRole('link',{name:'Comparar planos'}).click(); await expect(page.locator('#comparacao')).toBeInViewport();
  await page.getByRole('button',{name:'Preciso de cartão para começar?'}).click(); await expect(page.getByText(/plano Gratuito pode ser usado/)).toBeVisible();
  await expect(page.getByRole('link',{name:'Começar grátis'}).first()).toHaveAttribute('href','/register');
});

test('mobile public menu has the concise product journey', async ({ page }) => {
  await page.setViewportSize({width:390,height:844}); await page.goto('/'); await page.getByRole('button',{name:'Abrir menu principal'}).click();
  for (const label of ['Início','Como funciona','Biblioteca','Planos','Privacidade','Ajuda']) await expect(page.getByRole('link',{name:new RegExp(label)}).last()).toBeVisible();
});
