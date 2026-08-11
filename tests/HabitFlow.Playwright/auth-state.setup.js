import { test, expect } from '@playwright/test';
import path from 'node:path';

test('manual authenticated storage state', async ({ page }) => {
  const output = process.env.HABITFLOW_AUTH_OUTPUT;
  if (!output) throw new Error('HABITFLOW_AUTH_OUTPUT is required.');
  await page.goto('/login');
  console.log('Conclua o login manualmente e clique em "Continuar" no Playwright Inspector.');
  await page.pause();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
  await page.context().storageState({ path: path.resolve(output) });
});
