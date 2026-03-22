import { test, expect } from '@playwright/test';

test.describe('WebGL app loads', () => {
  test('page returns 200', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.status()).toBe(200);
  });

  test('unity canvas is present with correct dimensions', async ({ page }) => {
    await page.goto('/');

    const canvas = page.locator('#unity-canvas');
    await expect(canvas).toBeVisible({ timeout: 30_000 });

    const width = await canvas.getAttribute('width');
    const height = await canvas.getAttribute('height');
    expect(Number(width)).toBeGreaterThan(0);
    expect(Number(height)).toBeGreaterThan(0);
  });

  test('no JavaScript errors on load', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (err) => errors.push(err.message));

    await page.goto('/');
    // Give WASM time to start loading
    await page.waitForTimeout(5000);

    // Filter out expected WASM-related warnings
    const criticalErrors = errors.filter(
      (e) => !e.includes('wasm') && !e.includes('WebAssembly'),
    );
    expect(criticalErrors).toHaveLength(0);
  });
});
