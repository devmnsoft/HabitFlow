import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';

const viewports = [[1600,900],[1440,900],[1366,768],[1180,820],[1024,768],[768,1024],[430,932],[390,844],[360,800],[320,568]];
const evidence = path.resolve('../../artifacts/habits-v6.16.2');

test.beforeAll(async () => {
  if (!process.env.HABITFLOW_AUTH_STORAGE) throw new Error('HABITFLOW_AUTH_STORAGE is required; run the autonomous CI authentication bootstrap.');
  await fs.mkdir(evidence, { recursive: true });
});

for (const [width, height] of viewports) test(`/habits is collision-free at ${width}x${height}`, async ({ page }) => {
  const errors = [];
  page.on('console', message => message.type() === 'error' && errors.push(message.text()));
  page.on('pageerror', error => errors.push(error.message));
  await page.setViewportSize({ width, height });
  await page.goto('/habits', { waitUntil: 'networkidle' });
  await expect(page.locator('.hf-habits-page')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBeTruthy();
  await expect(page.locator('.modal.show,.offcanvas.show,.dropdown-menu.show,dialog[open],.hf-search:not([hidden])')).toHaveCount(0);
  for (const card of await page.locator('.hf-habits-list-card').all()) {
    const box = await card.boundingBox(); expect(box.x).toBeGreaterThanOrEqual(0); expect(box.x + box.width).toBeLessThanOrEqual(width + 1);
    const button = card.getByRole('link', { name: 'Ver progresso' }); await expect(button).toBeVisible();
    const buttonBox = await button.boundingBox(); expect(buttonBox.y + buttonBox.height).toBeLessThanOrEqual(box.y + box.height + 1);
    const progress = card.locator('progress'); expect((await progress.boundingBox()).width).toBeGreaterThan(Math.min(180, box.width * .5));
  }
  const controls = page.locator('.hf-habit-filters .form-control,.hf-habit-filters .form-select');
  if (await controls.count()) expect(new Set(await controls.evaluateAll(nodes => nodes.map(n => n.getBoundingClientRect().height))).size).toBe(1);
  expect(errors).toEqual([]);
  await page.screenshot({ path: path.join(evidence, `habits-${width}x${height}.png`), fullPage: true });
});

test('header overlays are identified, non-empty and closable', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 }); await page.goto('/habits', { waitUntil: 'networkidle' });
  const cases = [
    ['[data-global-search-open]', '#globalSearch', 'Onde você quer chegar?'],
    ['[data-notification-trigger]', '.header-v4__notification-menu', 'Notificações'],
    ['.header-v4__user', '.header-v4__user-menu', 'Conta'],
    ['.header-v4__menu', '#headerDrawer', 'Navegação']
  ];
  for (const [trigger, overlay, title] of cases) {
    const button = page.locator(trigger).first(); if (!(await button.isVisible())) continue;
    await button.click(); const panel = page.locator(overlay).first(); await expect(panel).toBeVisible(); await expect(panel).toContainText(title);
    const box = await panel.boundingBox(); expect(box.x).toBeGreaterThanOrEqual(0); expect(box.y).toBeGreaterThanOrEqual(0); expect(box.x + box.width).toBeLessThanOrEqual(391); expect(box.y + box.height).toBeLessThanOrEqual(845);
    await page.screenshot({ path: path.join(evidence, `overlay-${overlay.replace(/\W/g,'_')}.png`) });
    await page.keyboard.press('Escape'); await expect(panel).toBeHidden();
  }
});
