import { test } from '@playwright/test';
import { PokerApiClient } from '../../helpers/api-client.js';
import * as path from 'path';

const SCREENSHOT_DIR = path.resolve(__dirname, '../../../unity-client/Screenshots');
const api = new PokerApiClient();

test('capture README screenshots', async ({ page }) => {
  test.setTimeout(180_000);

  await page.goto('/');

  // Wait for Unity canvas to appear
  const canvas = page.locator('#unity-canvas');
  await canvas.waitFor({ state: 'visible', timeout: 30_000 });

  // Wait for WASM loading to complete (loading bar hidden)
  await page.waitForFunction(
    () => {
      const bar = document.querySelector('#unity-loading-bar') as HTMLElement;
      return bar && bar.style.display === 'none';
    },
    undefined,
    { timeout: 120_000 },
  );

  // Wait for lobby entrance animations to settle
  await page.waitForTimeout(8000);

  // Screenshot 1: Lobby
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '01-lobby.png') });

  // Click Table 1 (center of top-left preview card)
  await canvas.click({ position: { x: 250, y: 200 } });

  // Wait for scene transition + game initialization
  await page.waitForTimeout(8000);

  // Screenshot 2: Advance to pre-flop betting
  await api.advanceUntilStep(1, 'PRE_FLOP_BETTING_ROUND');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '02-preflop-betting.png') });

  // Screenshot 3: Advance to flop betting
  await api.advanceUntilStep(1, 'FLOP_BETTING_ROUND');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '03-flop-betting.png') });

  // Screenshot 4: Advance to showdown (PAY_WINNERS shows the showdown overlay)
  await api.advanceUntilStep(1, 'PAY_WINNERS');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '04-showdown.png') });

  // Dismiss showdown overlay by clicking "NEXT HAND" button (bottom-center of canvas)
  await canvas.click({ position: { x: 480, y: 520 } });
  await page.waitForTimeout(3000);

  // Go back to lobby by clicking "< Lobby" button (top-left of canvas)
  await canvas.click({ position: { x: 35, y: 25 } });
  await page.waitForTimeout(5000);

  // Screenshot 5: Current state (lobby with games in various states)
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, 'current-state.png') });
});
