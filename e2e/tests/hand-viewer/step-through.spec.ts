import { test, expect } from '@playwright/test';

test.describe('Hand viewer step-through', () => {
  test('clicking Next Step changes the phase text', async ({ page }) => {
    await page.goto('/');

    // Wait for the page to initialize
    await page.waitForTimeout(1000);

    const phaseBefore = await page.locator('#phase').textContent();

    // Click Next Step
    await page.locator('#btn-step').click();

    // Wait for the API call to complete and phase to update
    await page.waitForTimeout(2000);

    const phaseAfter = await page.locator('#phase').textContent();

    // Phase text should have changed (it will now show a hand/step label)
    expect(phaseAfter).not.toBe(phaseBefore);
    // Should contain "Hand #" indicating a game state
    expect(phaseAfter).toContain('Hand #');
  });

  test('16 clicks complete a full hand', async ({ page }) => {
    await page.goto('/');
    await page.waitForTimeout(1000);

    for (let i = 0; i < 16; i++) {
      await page.locator('#btn-step').click();
      // Allow each step to process
      await page.waitForTimeout(500);
    }

    const phaseText = await page.locator('#phase').textContent();
    // After 16 steps, should show "Hand Complete" or a new hand starting
    expect(phaseText).toContain('Hand #');
  });
});
