import { defineConfig } from '@playwright/test';

const externalServer = process.env.HABITFLOW_EXTERNAL_SERVER === '1';
const authSetup = Boolean(process.env.HABITFLOW_AUTH_OUTPUT);
export default defineConfig({
  testDir: '.',
  testMatch: authSetup ? 'auth-state.setup.js' : '**/*.spec.js',
  outputDir: 'test-results',
  reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }], ['list']],
  use: {
    baseURL: process.env.HABITFLOW_BASE_URL || 'http://127.0.0.1:5097',
    ...(process.env.HABITFLOW_AUTH_STORAGE ? { storageState: process.env.HABITFLOW_AUTH_STORAGE } : {}),
    trace: 'retain-on-failure', screenshot: 'only-on-failure', video: 'retain-on-failure'
  },
  projects: [{ name: authSetup ? 'auth-setup' : 'chromium', use: { browserName: 'chromium', viewport: { width: 1440, height: 900 } } }],
  webServer: process.env.CI && !externalServer ? {
    command: 'dotnet run --no-build -c Release --project ../../src/HabitFlow.Web --urls http://127.0.0.1:5097',
    url: 'http://127.0.0.1:5097/health', reuseExistingServer: false, timeout: 120000
  } : undefined
});
