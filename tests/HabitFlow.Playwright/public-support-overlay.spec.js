import { test, expect } from '@playwright/test';

const viewports = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
  { width: 320, height: 568 }
];

for (const viewport of viewports) {
  test(`public header starts closed and support works at ${viewport.width}x${viewport.height}`, async ({ page }, testInfo) => {
    const errors = [];
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('pageerror', error => errors.push(error.message));
    await page.setViewportSize(viewport);

    const home = await page.goto('/', { waitUntil: 'networkidle' });
    expect(home?.status()).toBe(200);
    await expect(page.locator('.dropdown-menu.show, .offcanvas.show, .modal.show, dialog[open], .hf-search-panel.is-open, .hf-user-menu.is-open, .hf-notification-menu.is-open')).toHaveCount(0);
    const domDiagnostic = await page.evaluate(() => ({
      atHeaderRight: document.elementFromPoint(innerWidth - 220, 120)?.outerHTML.slice(0, 500),
      openElements: [...document.querySelectorAll('.show, [open], .open, .active')].map(element => ({
        tag: element.tagName, id: element.id, className: element.className,
        role: element.getAttribute('role'), ariaLabel: element.getAttribute('aria-label'),
        ariaExpanded: element.getAttribute('aria-expanded'), rect: element.getBoundingClientRect().toJSON()
      }))
    }));
    await testInfo.attach('initial-dom-diagnostic', { body: JSON.stringify(domDiagnostic, null, 2), contentType: 'application/json' });
    await page.screenshot({ path: testInfo.outputPath('public-clean.png'), fullPage: true });

    if (viewport.width >= 1024) {
      const more = page.getByRole('button', { name: 'Mais' });
      await more.click();
      await expect(more).toHaveAttribute('aria-expanded', 'true');
      await expect(page.locator('#public-more-menu .dropdown-item')).not.toHaveCount(0);
      await expect(page.locator('#public-more-menu')).toContainText('Suporte');
      await page.keyboard.press('Escape');
      await expect(more).toHaveAttribute('aria-expanded', 'false');
      await more.click();
      await page.locator('main').click({ position: { x: 10, y: 10 } });
      await expect(more).toHaveAttribute('aria-expanded', 'false');
    } else {
      await page.getByRole('button', { name: 'Abrir menu principal' }).click();
      await expect(page.locator('#headerDrawer')).toHaveClass(/show/);
      await expect(page.locator('#headerDrawer')).toContainText('Suporte');
      await page.getByRole('button', { name: 'Fechar menu' }).click();
      await expect(page.locator('#headerDrawer')).not.toHaveClass(/show/);
    }
    await page.screenshot({ path: testInfo.outputPath('public-after-interaction.png'), fullPage: true });

    const support = await page.goto('/support', { waitUntil: 'networkidle' });
    expect(support?.status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Central de suporte' })).toBeVisible();
    expect(errors).toEqual([]);
  });
}
