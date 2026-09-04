import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env['E2E_BASE_URL'] ?? 'http://localhost:4200';
const isCi = !!process.env['CI'];

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  forbidOnly: isCi,
  retries: isCi ? 1 : 0,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  reporter: isCi ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm start -- --port 4200',
    url: baseURL,
    timeout: 180_000,
    reuseExistingServer: !isCi,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
