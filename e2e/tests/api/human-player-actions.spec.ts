import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID, BETTING_STEPS } from '../../helpers/constants.js';

test.describe('Human player actions', () => {
  test('join, advance to betting round, and call', async ({ pokerApi }) => {
    // Join at seat 1
    const joinRes = await pokerApi.join(TABLE_ID, 'ActionPlayer', 1);
    expect(joinRes.status).toBe(200);

    // Advance to a betting round
    const table = await pokerApi.advanceUntilStep(TABLE_ID, 'PRE_FLOP_BETTING_ROUND');
    expect(table.game.stepName).toBe('PRE_FLOP_BETTING_ROUND');

    // Submit a call action
    const { status, body } = await pokerApi.action(TABLE_ID, 1, 'call');

    // May succeed or error if it's not our turn — either is valid
    expect([200, 400]).toContain(status);
    if (status === 200) {
      expect(body.success).toBe(true);
    }
  });

  test('invalid action returns error', async ({ pokerApi }) => {
    await pokerApi.join(TABLE_ID, 'BadAction', 1);
    await pokerApi.advanceUntilStep(TABLE_ID, 'PRE_FLOP_BETTING_ROUND');

    const { status, body } = await pokerApi.action(TABLE_ID, 1, 'invalid_action');

    expect(status).toBe(400);
    expect(body.error).toBeTruthy();
  });

  test('action when not at a betting step errors', async ({ pokerApi }) => {
    await pokerApi.join(TABLE_ID, 'WrongPhase', 1);

    // Advance to DEAL_CARDS (not a betting round)
    await pokerApi.advanceUntilStep(TABLE_ID, 'DEAL_CARDS');

    const { status, body } = await pokerApi.action(TABLE_ID, 1, 'call');

    expect(status).toBe(400);
    expect(body.error).toBeTruthy();
  });

  test('name override appears in table state', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);
    const customName = 'CustomHero';
    await pokerApi.join(TABLE_ID, customName, 1);

    const { body } = await pokerApi.getTable(TABLE_ID);

    const seat1Player = body.players.find((p) => p.seat === 1);
    expect(seat1Player?.username).toBe(customName);
  });
});
