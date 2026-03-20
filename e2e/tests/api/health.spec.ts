import { test, expect } from '../../fixtures/api-fixture.js';

test.describe('GET /health', () => {
  test('returns 200 with service info', async ({ pokerApi }) => {
    const { status, body } = await pokerApi.health();

    expect(status).toBe(200);
    expect(body.service).toBe('holdem-processor');
    expect(body.status).toBe('ok');
    expect(body.timestamp).toBeTruthy();
    // timestamp should be a valid ISO date
    expect(new Date(body.timestamp).getTime()).not.toBeNaN();
  });
});
