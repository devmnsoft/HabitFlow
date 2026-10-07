import { test, expect } from '@playwright/test';

const viewports = [
  { width: 320, height: 800 },
  { width: 375, height: 800 },
  { width: 768, height: 900 },
  { width: 1024, height: 900 },
  { width: 1440, height: 900 }
];

const publicRoutes = ['/login', '/plans', '/help'];

test.describe('v6.20.0 production UI visual gate', () => {
  for (const viewport of viewports) {
    for (const route of publicRoutes) {
      test(`${route} is readable and stable at ${viewport.width}px`, async ({ page }) => {
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        page.on('console', message => {
          if (message.type() === 'error') errors.push(message.text());
        });

        await page.setViewportSize(viewport);
        await page.goto(route);
        await expect(page.locator('body')).toBeVisible();

        const metrics = await page.evaluate(() => {
          const openOverlay = document.querySelector('.dropdown-menu.show, .modal.show, .offcanvas.show, dialog[open]');
          const bodyText = document.body.innerText.trim();
          const main = document.querySelector('main');
          const mainBox = main?.getBoundingClientRect();
          const horizontalOverflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 1;
          const emptyWhiteBlocks = [...document.querySelectorAll('main div, main section, main article')]
            .filter(element => {
              const box = element.getBoundingClientRect();
              const style = getComputedStyle(element);
              return box.width > 120 && box.height > 80 && style.backgroundColor === 'rgb(255, 255, 255)' && element.innerText.trim().length === 0;
            }).length;

          return {
            bodyTextLength: bodyText.length,
            hasMain: Boolean(main),
            mainVisible: Boolean(mainBox && mainBox.width > 0 && mainBox.height > 0),
            horizontalOverflow,
            openOverlay: Boolean(openOverlay),
            emptyWhiteBlocks
          };
        });

        expect(metrics.bodyTextLength).toBeGreaterThan(30);
        expect(metrics.hasMain).toBeTruthy();
        expect(metrics.mainVisible).toBeTruthy();
        expect(metrics.horizontalOverflow).toBeFalsy();
        expect(metrics.openOverlay).toBeFalsy();
        expect(metrics.emptyWhiteBlocks).toBe(0);
        expect(errors).toEqual([]);

        await page.screenshot({ path: `artifacts/v6200/${route.replace(/\W+/g, '_')}-${viewport.width}.png`, fullPage: true });
      });
    }
  }
});
