import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID } from '../../helpers/constants.js';

test.describe('GET /table/:tableId', () => {
  test('returns game and players for valid table', async ({ pokerApi }) => {
    // Ensure at least one step has been processed
    await pokerApi.process(TABLE_ID);

    const { status, body } = await pokerApi.getTable(TABLE_ID);

    expect(status).toBe(200);
    expect(body.game).toBeDefined();
    expect(body.players).toBeDefined();
    expect(Array.isArray(body.players)).toBe(true);
    expect(body.players.length).toBeGreaterThan(0);

    // Game shape
    expect(body.game).toHaveProperty('handStep');
    expect(body.game).toHaveProperty('stepName');
    expect(body.game).toHaveProperty('gameNo');
    expect(body.game).toHaveProperty('pot');
    expect(body.game).toHaveProperty('communityCards');
    expect(body.game).not.toHaveProperty('deck');

    // Player shape
    const player = body.players[0];
    expect(player).toHaveProperty('playerId');
    expect(player).toHaveProperty('username');
    expect(player).toHaveProperty('seat');
    expect(player).toHaveProperty('stack');
    expect(player).toHaveProperty('bet');
    expect(player).toHaveProperty('status');
  });

  test('returns 404 for nonexistent table', async ({ pokerApi }) => {
    const { status, body } = await pokerApi.getTable(99999);

    expect(status).toBe(404);
    expect((body as any).error).toBeTruthy();
  });

  test('returns 400 when tableId is missing', async () => {
    const res = await fetch('http://localhost:3030/table/');
    // serverless-offline may return 404 for missing route or 400
    expect([400, 404]).toContain(res.status);
  });
});
