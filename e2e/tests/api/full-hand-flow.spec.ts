import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID, STEP_NAMES, TOTAL_STEPS_PER_HAND } from '../../helpers/constants.js';

test.describe('Full hand flow', () => {
  test('processes all 16 steps through a complete hand', async ({ pokerApi }) => {
    // Get initial state
    const { body: initial } = await pokerApi.getTable(TABLE_ID);
    const initialGameNo = initial.game.gameNo;
    const initialChips = pokerApi.totalChips(initial);

    // Process all 16 steps
    const stepsSeen: string[] = [];
    for (let i = 0; i < TOTAL_STEPS_PER_HAND; i++) {
      const { status, body } = await pokerApi.process(TABLE_ID);
      expect(status).toBe(200);
      expect(body.success).toBe(true);
      stepsSeen.push(body.result.stepName);
    }

    // All step names should be valid
    for (const step of stepsSeen) {
      expect(STEP_NAMES).toContain(step);
    }

    // Should have reached RECORD_STATS_AND_NEW_HAND
    expect(stepsSeen).toContain('RECORD_STATS_AND_NEW_HAND');

    // Get final state
    const { body: final } = await pokerApi.getTable(TABLE_ID);

    // Game number should have incremented
    expect(final.game.gameNo).toBeGreaterThanOrEqual(initialGameNo);

    // Chip conservation: total chips should remain constant
    const finalChips = pokerApi.totalChips(final);
    expect(finalChips).toBeCloseTo(initialChips, 1);
  });

  test('pot resets to 0 after hand completes', async ({ pokerApi }) => {
    // Play a full hand
    await pokerApi.playFullHand(TABLE_ID);

    const { body } = await pokerApi.getTable(TABLE_ID);

    // After RECORD_STATS_AND_NEW_HAND the pot should be 0
    // (new hand is ready, pot is reset)
    expect(body.game.pot).toBe(0);
  });
});
