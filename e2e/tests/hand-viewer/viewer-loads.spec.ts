import { test, expect } from '@playwright/test';

test.describe('Hand viewer loads', () => {
  test('page returns 200', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.status()).toBe(200);
  });

  test('core DOM elements exist', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('#felt')).toBeVisible();
    await expect(page.locator('#phase')).toBeVisible();
    await expect(page.locator('#btn-step')).toBeVisible();
  });

  test('control buttons are present', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('#btn-step')).toHaveText('Next Step');
    await expect(page.locator('#btn-auto')).toHaveText('Auto Play');
    await expect(page.locator('#btn-reset')).toHaveText('Reset');
    await expect(page.locator('#btn-speed')).toBeVisible();
  });
});
