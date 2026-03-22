import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID } from '../../helpers/constants.js';

test.describe('Multi-hand', () => {
  test('two consecutive hands: gameNo increments, chips conserved', async ({ pokerApi }) => {
    // Play first hand
    const afterFirst = await pokerApi.playFullHand(TABLE_ID);
    const firstGameNo = afterFirst.game.gameNo;
    const firstChips = pokerApi.totalChips(afterFirst);

    // Play second hand
    const afterSecond = await pokerApi.playFullHand(TABLE_ID);
    const secondGameNo = afterSecond.game.gameNo;
    const secondChips = pokerApi.totalChips(afterSecond);

    // Game number should increment
    expect(secondGameNo).toBeGreaterThan(firstGameNo);

    // Chips should be conserved across hands
    expect(secondChips).toBeCloseTo(firstChips, 1);
  });
});
