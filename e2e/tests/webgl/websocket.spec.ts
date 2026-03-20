import { test, expect } from '@playwright/test';

test.describe('WebGL WebSocket', () => {
  test('WS connects via /ws proxy', async ({ page }) => {
    // Skip if broadcast service isn't running
    const broadcastUp = await fetch('http://localhost:3032')
      .then(() => true)
      .catch(() => false);
    test.skip(!broadcastUp, 'Broadcast service (port 3032) not running');

    await page.goto('/');

    // Evaluate in browser context: open a WebSocket and check for connection
    const connected = await page.evaluate(() => {
      return new Promise<boolean>((resolve) => {
        const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
        const ws = new WebSocket(`${protocol}//${location.host}/ws`);

        const timeout = setTimeout(() => {
          ws.close();
          resolve(false);
        }, 10_000);

        ws.onopen = () => {
          // Send a subscription message
          ws.send(JSON.stringify({ action: 'subscribe', tableId: '1' }));
        };

        ws.onmessage = () => {
          clearTimeout(timeout);
          ws.close();
          resolve(true);
        };

        ws.onerror = () => {
          clearTimeout(timeout);
          resolve(false);
        };
      });
    });

    expect(connected).toBe(true);
  });
});
