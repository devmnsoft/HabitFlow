import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';

const viewports = [[1600,900],[1440,900],[1366,768],[1280,720],[1180,820],[1024,768],[912,1368],[820,1180],[768,1024],[430,932],[414,896],[390,844],[375,812],[360,800],[320,568],[280,653],[240,320],[192,256]];
const publicRoutes = ['/', '/plans', '/privacy', '/help'];
const appRoutes = ['/dashboard','/my-day','/habits','/goals','/progress/calendar','/reports','/account/privacy','/account/plan/usage','/notifications'];
const output = path.resolve('artifacts/header-context/v6118/after');
const rows = [];

export async function assertNoHorizontalOverflow(page) { expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBeTruthy(); }
export async function assertHeaderHasNoOverlap(page) {
  const boxes = await page.locator('[data-header-root] [data-header-zone]:visible').evaluateAll(nodes => nodes.map(n => { const r=n.getBoundingClientRect(); return {l:r.left,r:r.right,t:r.top,b:r.bottom}; }));
  for(let i=0;i<boxes.length;i++) for(let j=i+1;j<boxes.length;j++) expect(!(boxes[i].l < boxes[j].r-1 && boxes[i].r > boxes[j].l+1 && boxes[i].t < boxes[j].b-1 && boxes[i].b > boxes[j].t+1)).toBeTruthy();
}
export async function assertPublicHeaderDoesNotShowAppActions(page) { await expect(page.locator('.header-v4--public .header-v4__create')).toHaveCount(0); await expect(page.locator('.header-v4--public .header-v4__plan')).toHaveCount(0); await expect(page.locator('.header-v4--public [data-notification-trigger]')).toHaveCount(0); }
export async function assertAppHeaderDoesNotShowMarketingActions(page) { await expect(page.locator('.header-v4--app .navigation-v4')).not.toContainText('Como funciona'); await expect(page.locator('.header-v4--app .navigation-v4')).not.toContainText('Planos'); }
export async function assertSearchIsCompact(page) { const button=page.locator('[data-header-search]'); await expect(button).toHaveCount(1); await expect(button).not.toContainText('hábitos, objetivos'); expect((await button.boundingBox())?.width ?? 0).toBeLessThanOrEqual(125); }
export async function assertNewButtonIsContextual(page, context, width) { const button=page.locator('.header-v4__create'); if(context==='public'||width<1024) await expect(button).toBeHidden(); else await expect(button).toBeVisible(); }
export async function assertPlanBadgeDoesNotBreakHeader(page, width) { const badge=page.locator('.header-v4__plan'); if(width<1280) await expect(badge).toBeHidden(); if(await badge.isVisible()) expect((await badge.boundingBox()).width).toBeLessThan(100); }
export async function assertUserNameDoesNotOverflow(page) { const name=page.locator('.header-v4__user-name'); if(await name.isVisible()) expect(await name.evaluate(n=>n.scrollWidth<=n.clientWidth||getComputedStyle(n).textOverflow==='ellipsis')).toBeTruthy(); }
export async function assertDrawerWorks(page, width) { if(width>=1024)return; await page.locator('.header-v4__menu').click(); await expect(page.locator('#headerDrawer')).toHaveClass(/show/); await page.keyboard.press('Escape'); await expect(page.locator('#headerDrawer')).not.toHaveClass(/show/); }
export async function assertBottomNavSafeArea(page, context, width) { const nav=page.locator('.mobile-bottom-nav-v4'); if(context==='public'||width>=768||width<320){await expect(nav).toBeHidden();return;} await expect(nav).toBeVisible(); const b=await nav.boundingBox(); expect(b.y+b.height).toBeLessThanOrEqual((await page.viewportSize()).height+1); }

async function inspect(page, route, width, height, context) {
  await page.goto(route); await expect(page.locator('[data-header-context]')).toHaveAttribute('data-header-context', context==='public'?'public':/personal|account/);
  await assertNoHorizontalOverflow(page); await assertHeaderHasNoOverlap(page);
  if(context==='public') await assertPublicHeaderDoesNotShowAppActions(page); else { await assertAppHeaderDoesNotShowMarketingActions(page); await assertSearchIsCompact(page); await assertPlanBadgeDoesNotBreakHeader(page,width); await assertUserNameDoesNotOverflow(page); }
  await assertNewButtonIsContextual(page,context,width); await assertDrawerWorks(page,width); await assertBottomNavSafeArea(page,context,width);
  const file=`${context}-${route.replaceAll('/','_')||'home'}-${width}x${height}.png`; await page.screenshot({path:path.join(output,file),fullPage:true});
  rows.push(`| ${route} | ${width}x${height} | ${context} | header contextual | ações do outro contexto | não | não | [screenshot](after/${file}) | PASS |`);
}

test.beforeAll(async()=>fs.mkdir(output,{recursive:true}));
test.afterAll(async()=>fs.writeFile(path.resolve('artifacts/header-context/v6118/report.md'),`# Evidência visual do header v6.11.8\n\n| Rota | Viewport | Contexto | Visíveis | Ocultos | Overflow | Overlap | Screenshot | Resultado |\n|---|---|---|---|---|---|---|---|---|\n${rows.join('\n')}\n`));
for(const [width,height] of viewports){ for(const route of publicRoutes) test(`public ${route} ${width}x${height}`,async({page})=>{await page.setViewportSize({width,height});await inspect(page,route,width,height,'public');}); for(const route of appRoutes) test(`app ${route} ${width}x${height}`,async({page})=>{test.skip(!process.env.HABITFLOW_AUTH_STORAGE,'HABITFLOW_AUTH_STORAGE is required');await page.setViewportSize({width,height});await inspect(page,route,width,height,'app');}); }
