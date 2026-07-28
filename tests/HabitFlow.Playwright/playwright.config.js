import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: '.', outputDir: 'test-results', reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }], ['list']],
  use: { baseURL: process.env.HABITFLOW_BASE_URL || 'http://127.0.0.1:5097', trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [
    { name: 'mobile-320', use: { viewport: { width: 320, height: 800 } } },
    { name: 'mobile-390', use: { viewport: { width: 390, height: 844 } } },
    { name: 'tablet-768', use: { viewport: { width: 768, height: 1024 } } },
    { name: 'desktop-1024', use: { viewport: { width: 1024, height: 900 } } },
    { name: 'desktop-1440', use: { viewport: { width: 1440, height: 1000 } } },
    { name: 'desktop-1920', use: { viewport: { width: 1920, height: 1080 } } }
  ],
  webServer: process.env.CI ? { command: 'dotnet run --no-build -c Release --project ../../src/HabitFlow.Web', url: 'http://127.0.0.1:5097', reuseExistingServer: false, timeout: 120000 } : undefined
});
