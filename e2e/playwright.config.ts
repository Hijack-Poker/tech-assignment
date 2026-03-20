import { defineConfig, devices } from '@playwright/test';
import { API_BASE_URL, HAND_VIEWER_URL, WEBGL_URL } from './helpers/constants.js';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? 'github' : 'list',
  globalSetup: './global-setup.ts',

  projects: [
    {
      name: 'api',
      testDir: './tests/api',
      use: {
        baseURL: API_BASE_URL,
      },
      timeout: 30_000,
    },
    {
      name: 'hand-viewer',
      testDir: './tests/hand-viewer',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: HAND_VIEWER_URL,
      },
      timeout: 30_000,
    },
    {
      name: 'webgl-smoke',
      testDir: './tests/webgl',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: WEBGL_URL,
      },
      timeout: 60_000,
    },
  ],
});
