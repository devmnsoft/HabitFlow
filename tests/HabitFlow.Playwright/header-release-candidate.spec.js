import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const evidence = path.join(root, 'artifacts/release-candidate/v6119/header');
const report = path.join(root, 'artifacts/release-candidate/v6119/header-report.md');
const sizes = [[1600,900],[1440,900],[1366,768],[1280,720],[1180,820],[1024,768],[912,1368],[820,1180],[768,1024],[430,932],[414,896],[390,844],[375,812],[360,800],[320,568],[280,653],[240,320],[192,256]];
const routes = {
  public: ['/', '/plans', '/privacy', '/help'],
  app: ['/dashboard','/my-day','/habits','/goals','/progress/calendar','/reports','/account/privacy','/account/plan/usage','/notifications','/reminders']
};
const rows = [];

function overlaps(a, b) {
  return a.left < b.right - 1 && a.right > b.left + 1 && a.top < b.bottom - 1 && a.bottom > b.top + 1;
}

test.beforeAll(async () => fs.mkdir(evidence, { recursive: true }));
test.afterAll(async () => fs.writeFile(report, [
  '# Header v6.11.9 — release candidate', '',
  '| Rota | Viewport | Contexto | Visíveis | Ocultos | Overflow | Overlap | Screenshot | Status |',
  '|---|---|---|---|---|---|---|---|---|', ...rows, ''
].join('\n')));

for (const [context, contextRoutes] of Object.entries(routes)) {
  for (const route of contextRoutes) {
    for (const [width, height] of sizes) {
      test(`${context} ${route} ${width}x${height}`, async ({ page }) => {
        test.skip(context === 'app' && !process.env.HABITFLOW_AUTH_STORAGE, 'HABITFLOW_AUTH_STORAGE is required for authenticated visual evidence');
        const browserErrors = [];
        page.on('console', message => message.type() === 'error' && browserErrors.push(message.text()));
        page.on('pageerror', error => browserErrors.push(error.message));
        await page.setViewportSize({ width, height });
        const response = await page.goto(route, { waitUntil: 'networkidle' });
        expect(response?.status(), `${route} returned an HTTP error`).toBeLessThan(400);
        const header = page.locator('[data-header-root]');
        await expect(header).toHaveClass(context === 'public' ? /header-v4--public/ : /header-v4--app/);
        expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBeTruthy();

        if (context === 'public') {
          await expect(header.locator('.header-v4__create, .header-v4__plan, [data-header-search]')).toHaveCount(0);
        } else {
          await expect(header.locator('.navigation-v4')).not.toContainText('Como funciona');
          const name = header.locator('.header-v4__user-name');
          if (await name.isVisible()) expect(await name.evaluate(node => node.scrollWidth <= node.clientWidth || getComputedStyle(node).textOverflow === 'ellipsis')).toBeTruthy();
        }

        const boxes = await header.locator('[data-header-zone]:visible').evaluateAll(nodes => nodes.map(node => node.getBoundingClientRect().toJSON()));
        for (let i = 0; i < boxes.length; i++) for (let j = i + 1; j < boxes.length; j++) expect(overlaps(boxes[i], boxes[j])).toBeFalsy();

        const dropdown = header.locator('.header-v4__user-menu');
        const user = header.locator('.header-v4__user');
        if (await user.isVisible()) {
          await user.click();
          await expect(dropdown).toBeVisible();
          const box = await dropdown.boundingBox();
          expect(box.left).toBeGreaterThanOrEqual(0); expect(box.x + box.width).toBeLessThanOrEqual(width + 1);
          await page.keyboard.press('Escape');
        }
        if (width < 1024) {
          await header.locator('.header-v4__menu').click();
          await expect(page.locator('#headerDrawer')).toHaveClass(/show/);
          await page.keyboard.press('Escape');
          await expect(page.locator('#headerDrawer')).not.toHaveClass(/show/);
        }
        const layout = await page.evaluate(() => {
          const h = document.querySelector('[data-header-root]')?.getBoundingClientRect();
          const m = document.querySelector('main')?.getBoundingClientRect();
          const b = document.querySelector('.mobile-bottom-nav-v4')?.getBoundingClientRect();
          return { mainHidden: Boolean(h && m && m.top < h.bottom - 1), bottomCovers: Boolean(b?.height && m && m.bottom > b.top && getComputedStyle(document.body).paddingBottom === '0px') };
        });
        expect(layout.mainHidden).toBeFalsy(); expect(layout.bottomCovers).toBeFalsy(); expect(browserErrors).toEqual([]);
        const filename = `${context}-${route.replaceAll('/', '_') || 'home'}-${width}x${height}.png`;
        await page.screenshot({ path: path.join(evidence, filename), fullPage: true });
        rows.push(`| ${route} | ${width}x${height} | ${context} | header-v4 contextual | ações do outro contexto | não | não | [screenshot](header/${filename}) | PASS |`);
      });
    }
  }
}
