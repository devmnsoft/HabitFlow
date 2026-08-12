import { test, expect } from '@playwright/test';

const unique = () => `CRUD ${Date.now()}`;

test('habit and goal CRUD routes never expose a server exception', async ({ page }) => {
  const failures = [];
  page.on('response', response => { if (response.status() >= 500) failures.push(`${response.status()} ${response.url()}`); });
  await page.goto('/habits');
  await expect(page).not.toHaveURL(/\/auth\/login/);
  await page.goto('/habits/create');
  const name = unique();
  await page.getByLabel(/nome/i).fill(name);
  await page.getByRole('button', { name: /salvar|criar/i }).click();
  await expect(page.getByText(name)).toBeVisible();
  await page.getByRole('link', { name: /editar/i }).click();
  await page.getByLabel(/nome/i).fill(`${name} editado`);
  await page.getByRole('button', { name: /salvar/i }).click();
  await expect(page.getByText(`${name} editado`)).toBeVisible();
  expect(failures).toEqual([]);
});
