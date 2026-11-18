import { defineConfig, devices } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const webUrl = 'http://localhost:5173';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: 'list',
  use: {
    baseURL: webUrl,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../../src/Sift.Api --urls http://localhost:5100',
      url: `${apiUrl}/health`,
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    },
    {
      command: 'yarn --cwd ../../src/Sift.Web dev',
      url: webUrl,
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
  ],
});
