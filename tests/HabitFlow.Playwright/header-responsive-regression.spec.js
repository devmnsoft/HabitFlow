import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { assertNoHorizontalOverflow, assertNoHeaderOverlap, assertVisibleOrIntentionallyHidden, assertDrawerWorks, assertBottomNavSafeArea } from './helpers/header-responsive.js';

const viewports = [[192,256],[240,320],[280,653],[320,568],[360,800],[375,812],[390,844],[414,896],[430,932],[768,1024],[820,1180],[912,1368],[1024,768],[1180,820],[1280,720],[1366,768],[1440,900],[1600,900]];
const artifactDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../artifacts/header-responsive/v6115');
const results = [];

test.beforeAll(async () => fs.mkdir(artifactDirectory, { recursive: true }));
test.afterAll(async () => {
  const rows = results.map(result => `| ${result.viewport} | ${result.status} | ${result.overflow} | ${result.overlap} | [imagem](${result.screenshot}) | ${result.note} |`).join('\n');
  await fs.writeFile(path.join(artifactDirectory, 'report.md'), `# Header responsivo v6.11.5\n\n| Viewport | Status | Overflow | Sobreposição | Screenshot | Observação |\n|---|---|---|---|---|---|\n${rows}\n`);
});

for (const [width, height] of viewports) test(`dashboard authenticated header ${width}x${height}`, async ({ page }) => {
  await page.setViewportSize({ width, height });
  await page.goto('/dashboard');
  await expect(page, 'configure an authenticated Playwright storage state/session').not.toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('[data-app-header]')).toBeVisible();
  const screenshot = `header-${width}x${height}.png`;
  try {
    await assertNoHorizontalOverflow(page); await assertNoHeaderOverlap(page);
    await assertVisibleOrIntentionallyHidden(page, '[data-header-more]');
    await assertVisibleOrIntentionallyHidden(page, '[data-header-search]');
    await assertDrawerWorks(page); await assertBottomNavSafeArea(page);
    const userName = page.locator('.app-header-v2__user-name').first();
    if (await userName.isVisible()) expect(await userName.evaluate(node => node.scrollWidth <= node.clientWidth || getComputedStyle(node).textOverflow === 'ellipsis')).toBeTruthy();
    await page.screenshot({ path: path.join(artifactDirectory, screenshot), fullPage: true });
    results.push({ viewport: `${width}x${height}`, status: 'PASS', overflow: 'não', overlap: 'não', screenshot, note: 'Header, drawer e safe area validados.' });
  } catch (error) {
    await page.screenshot({ path: path.join(artifactDirectory, `FAILED-${screenshot}`), fullPage: true });
    results.push({ viewport: `${width}x${height}`, status: 'FAIL', overflow: 'ver teste', overlap: 'ver teste', screenshot: `FAILED-${screenshot}`, note: String(error.message).split('\n')[0] });
    throw error;
  }
});
