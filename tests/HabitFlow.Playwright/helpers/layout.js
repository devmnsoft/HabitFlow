import { expect } from '@playwright/test';
export function monitorPage(page) {
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(`console: ${message.text()}`); });
  page.on('pageerror', error => errors.push(`page: ${error.message}`));
  page.on('requestfailed', request => errors.push(`network: ${request.url()} ${request.failure()?.errorText}`));
  return errors;
}
export async function expectStableLayout(page) {
  await expect(page.locator('header[data-layout-region="header"]')).toBeVisible();
  await expect(page.locator('main#conteudo')).toBeVisible();
  const metrics = await page.evaluate(() => ({ scroll: document.documentElement.scrollWidth, client: document.documentElement.clientWidth }));
  expect(metrics.scroll).toBeLessThanOrEqual(metrics.client + 1);
  const overlap = await page.evaluate(() => {
    const header = document.querySelector('[data-layout-region="header"]')?.getBoundingClientRect();
    const main = document.querySelector('main')?.getBoundingClientRect();
    const sidebar = document.querySelector('.hf-context-sidebar')?.getBoundingClientRect();
    if (header && main && main.top < header.bottom - 1) return 'header-main';
    if (sidebar && sidebar.width && main && sidebar.right > main.left + 1) return 'sidebar-main';
    return null;
  });
  expect(overlap).toBeNull();
}
