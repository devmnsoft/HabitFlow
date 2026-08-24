import { test, expect } from '@playwright/test';
import path from 'node:path';

test('create authenticated storage state', async ({ page }) => {
  const output = process.env.HABITFLOW_AUTH_OUTPUT;
  if (!output) throw new Error('HABITFLOW_AUTH_OUTPUT is required.');
  const email = process.env.HABITFLOW_CI_EMAIL;
  const password = process.env.HABITFLOW_CI_PASSWORD;
  if (!email || !password) throw new Error('Ephemeral CI credentials are required.');
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Senha').fill(password);
  await page.getByRole('button', { name: /entrar/i }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
  await page.context().storageState({ path: path.resolve(output) });
});
