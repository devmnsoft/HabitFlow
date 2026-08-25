import { test, expect } from '@playwright/test';

const publicRoutes = ['/', '/plans', '/support', '/login'];
const sizes = [[1440, 900], [1366, 768], [1024, 768], [768, 1024], [430, 932], [390, 844], [360, 800], [320, 568]];

for (const route of publicRoutes) for (const [width, height] of sizes) {
  test(`v6.17.2 ${route} ${width}x${height}`, async ({ page }, testInfo) => {
    const errors = [];
    page.on('console', message => message.type() === 'error' && errors.push(message.text()));
    page.on('pageerror', error => errors.push(error.message));
    await page.setViewportSize({ width, height });
    const response = await page.goto(route, { waitUntil: 'networkidle' });
    expect(response?.status()).toBeLessThan(400);
    await expect(page.locator('h1').first()).toBeVisible();
    await expect(page.locator('.modal.show, .offcanvas.show, .dropdown-menu.show, dialog[open]')).toHaveCount(0);
    const layout = await page.evaluate(() => ({
      overflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      emptyHeaderPanel: [...document.querySelectorAll('header *')].some(node => {
        const box = node.getBoundingClientRect();
        return box.width > 240 && box.height > 120 && !node.textContent.trim() && getComputedStyle(node).backgroundColor === 'rgb(255, 255, 255)';
      })
    }));
    expect(layout).toEqual({ overflow: false, emptyHeaderPanel: false });
    expect(errors).toEqual([]);
    await page.screenshot({ path: testInfo.outputPath(`${route.replaceAll('/', '-') || 'home'}-${width}.png`), fullPage: true });
  });
}

test('menu Mais preserves the accessible closed/open contract', async ({ page }) => {
  await page.goto('/');
  const trigger = page.getByRole('button', { name: 'Mais' });
  await trigger.click();
  await expect(trigger).toHaveAttribute('aria-expanded', 'true');
  await expect(page.locator('#public-more-menu')).toContainText('Suporte');
  await page.keyboard.press('Escape');
  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await expect(trigger).toBeFocused();
});
