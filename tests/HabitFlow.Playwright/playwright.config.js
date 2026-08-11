import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: '.', outputDir: 'test-results', reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }], ['list']],
  use: { baseURL: process.env.HABITFLOW_BASE_URL || 'http://127.0.0.1:5097', ...(process.env.HABITFLOW_AUTH_STORAGE ? { storageState: process.env.HABITFLOW_AUTH_STORAGE } : {}), trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [
    { name: 'watch-192', use: { viewport: { width: 192, height: 320 } } },
    { name: 'watch-240', use: { viewport: { width: 240, height: 360 } } },
    { name: 'micro-280', use: { viewport: { width: 280, height: 480 } } },
    { name: 'mobile-320', use: { viewport: { width: 320, height: 800 } } },
    { name: 'mobile-390', use: { viewport: { width: 390, height: 844 } } },
    { name: 'mobile-430', use: { viewport: { width: 430, height: 900 } } },
    { name: 'tablet-768', use: { viewport: { width: 768, height: 1024 } } },
    { name: 'desktop-1024', use: { viewport: { width: 1024, height: 900 } } },
    { name: 'desktop-1440', use: { viewport: { width: 1440, height: 1000 } } },
    { name: 'desktop-1920', use: { viewport: { width: 1920, height: 1080 } } }
  ],
  webServer: process.env.CI ? { command: 'dotnet run --no-build -c Release --project ../../src/HabitFlow.Web', url: 'http://127.0.0.1:5097', reuseExistingServer: false, timeout: 120000 } : undefined
});
