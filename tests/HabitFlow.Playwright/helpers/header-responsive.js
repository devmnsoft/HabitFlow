import { expect } from '@playwright/test';

export async function assertNoHorizontalOverflow(page) {
  const dimensions = await page.evaluate(() => ({ scrollWidth: document.documentElement.scrollWidth, innerWidth: window.innerWidth }));
  expect(dimensions.scrollWidth, `overflow: ${dimensions.scrollWidth}px > ${dimensions.innerWidth}px`).toBeLessThanOrEqual(dimensions.innerWidth);
}

const visibleBoxes = page => page.locator('[data-app-header] [data-header-zone], [data-app-header] [data-header-more], [data-app-header] [data-header-search]').evaluateAll(elements => elements.filter(element => {
  const style = getComputedStyle(element); const box = element.getBoundingClientRect();
  return style.display !== 'none' && style.visibility !== 'hidden' && box.width > 0 && box.height > 0;
}).map(element => ({ name: element.dataset.headerZone || (element.hasAttribute('data-header-more') ? 'more' : 'search'), box: element.getBoundingClientRect().toJSON() })));

export async function assertNoHeaderOverlap(page) {
  const boxes = await visibleBoxes(page);
  const intersections = [];
  for (let left = 0; left < boxes.length; left += 1) for (let right = left + 1; right < boxes.length; right += 1) {
    const a = boxes[left]; const b = boxes[right];
    if (a.name === 'navigation' && ['more'].includes(b.name) || b.name === 'navigation' && ['more'].includes(a.name)) continue;
    const overlaps = a.box.left < b.box.right - 1 && a.box.right > b.box.left + 1 && a.box.top < b.box.bottom - 1 && a.box.bottom > b.box.top + 1;
    if (overlaps) intersections.push(`${a.name}/${b.name}`);
  }
  expect(intersections, `header intersections: ${intersections.join(', ')}`).toEqual([]);
}

export async function assertVisibleOrIntentionallyHidden(page, selector) {
  const element = page.locator(selector).first();
  await expect(element).toHaveCount(1);
  const state = await element.evaluate(node => ({ visible: node.getClientRects().length > 0, hidden: getComputedStyle(node).display === 'none' || getComputedStyle(node).visibility === 'hidden' }));
  expect(state.visible || state.hidden).toBeTruthy();
}

export async function assertDrawerWorks(page) {
  const button = page.locator('[data-bs-target="#appHeaderDrawer"]');
  if (!(await button.isVisible())) return;
  await button.click(); await expect(page.locator('#appHeaderDrawer')).toBeVisible();
  await page.keyboard.press('Escape'); await expect(page.locator('#appHeaderDrawer')).toBeHidden(); await expect(button).toBeFocused();
}

export async function assertBottomNavSafeArea(page) {
  const nav = page.locator('.app-bottom-nav-v2');
  if (!(await nav.isVisible())) return;
  const safe = await nav.evaluate(element => { const box = element.getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.bottom <= innerHeight + 1; });
  expect(safe).toBeTruthy();
}
