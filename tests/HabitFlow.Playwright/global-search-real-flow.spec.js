import { test, expect } from '@playwright/test';

test.describe('global search real and safe flow', () => {
  test.skip(!process.env.HABITFLOW_AUTH_STORAGE, 'HABITFLOW_AUTH_STORAGE is required');
  test.beforeEach(async ({ page }) => { await page.goto('/dashboard'); });

  for (const shortcut of ['Control+K', 'Meta+K']) test(`opens with ${shortcut}`, async ({ page }) => {
    await page.keyboard.press(shortcut);
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog')).toBeHidden();
    await expect(page.locator('[data-global-search-open]').first()).toBeFocused();
  });

  test('renders malicious input as text and supports keyboard navigation', async ({ page }) => {
    let dialogs = 0; page.on('dialog', dialog => { dialogs++; return dialog.dismiss(); });
    await page.locator('[data-global-search-open]').first().click();
    const search = page.getByRole('searchbox');
    const payload = '<img src=x onerror=alert(1)>';
    await search.fill(payload);
    await expect(page.locator('[data-global-search-results] img')).toHaveCount(0);
    expect(await page.locator('[data-global-search-results]').evaluate(node => node.innerHTML.includes('<img'))).toBeFalsy();
    expect(dialogs).toBe(0);
    await search.fill('privacidade');
    const option = page.getByRole('option').first();
    await expect(option).toBeVisible();
    await page.keyboard.press('ArrowDown');
    await expect(option).toHaveAttribute('aria-selected', 'true');
    const href = await option.getAttribute('href');
    expect(href === '#' || href?.startsWith('/')).toBeTruthy();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(new RegExp(href.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
  });
});
