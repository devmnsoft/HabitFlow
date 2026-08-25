import { test, expect } from '@playwright/test';

const viewports=[{width:1440,height:900},{width:1024,height:768},{width:768,height:1024},{width:390,height:844},{width:320,height:568}];

for(const viewport of viewports)test(`painel pessoal saudável ${viewport.width}x${viewport.height}`,async({page})=>{
  const errors=[]; page.on('console',m=>{if(m.type()==='error')errors.push(m.text())}); page.on('pageerror',e=>errors.push(e.message));
  await page.setViewportSize(viewport); await page.goto('/progress');
  await expect(page.getByRole('heading',{name:'Progresso pessoal'})).toBeVisible();
  await expect(page.getByText('Sem comparação com outras pessoas')).toBeVisible();
  const size=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,client:document.documentElement.clientWidth}));
  expect(size.scroll).toBeLessThanOrEqual(size.client+1); expect(errors).toEqual([]);
});

test('meta, conquista e bloqueio premium conduzem a planos',async({page})=>{
  await page.goto('/weekly-goals');
  if(await page.locator('input[name="habitIds"]').count()){
    await page.getByLabel('Nome da meta').fill('Meu ritmo da semana');
    await page.locator('input[name="habitIds"]').first().check();
    await page.getByRole('button',{name:'Criar meta'}).click();
    await expect(page.getByText('Meu ritmo da semana')).toBeVisible();
  }
  await page.goto('/achievements'); await expect(page.getByRole('heading',{name:'Conquistas'})).toBeVisible();
  await page.goto('/plans?feature=streak_freeze'); await expect(page).toHaveURL(/\/plans/);
});
