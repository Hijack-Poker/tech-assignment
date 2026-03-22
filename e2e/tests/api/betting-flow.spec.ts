import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID, BETTING_STEPS } from '../../helpers/constants.js';

test.describe('Betting flow — human player pause and resume', () => {
  test('process pauses at human seat during betting round', async ({ pokerApi }) => {
    // Join at seat 1
    const joinRes = await pokerApi.join(TABLE_ID, 'PauseTest', 1);
    expect(joinRes.status).toBe(200);
    const humanSeat = joinRes.body.seat;

    // Advance to a betting round where it's the human's turn
    const table = await pokerApi.advanceUntilHumanTurn(TABLE_ID, humanSeat);

    expect(BETTING_STEPS).toContain(table.game.stepName);
    expect(table.game.move).toBe(humanSeat);
  });

  test('repeated process calls do not skip past human turn', async ({ pokerApi }) => {
    const joinRes = await pokerApi.join(TABLE_ID, 'IdempotentTest', 1);
    const humanSeat = joinRes.body.seat;

    const table = await pokerApi.advanceUntilHumanTurn(TABLE_ID, humanSeat);
    const steppedStep = table.game.stepName;

    // Call process several more times — state should not change
    for (let i = 0; i < 3; i++) {
      await pokerApi.process(TABLE_ID);
    }

    const { body: afterExtra } = await pokerApi.getTable(TABLE_ID);
    expect(afterExtra.game.stepName).toBe(steppedStep);
    expect(afterExtra.game.move).toBe(humanSeat);
  });

  test('submitting an action advances the game past the pause', async ({ pokerApi }) => {
    const joinRes = await pokerApi.join(TABLE_ID, 'ActionTest', 1);
    const humanSeat = joinRes.body.seat;

    const before = await pokerApi.advanceUntilHumanTurn(TABLE_ID, humanSeat);
    const stepBefore = before.game.stepName;
    const moveBefore = before.game.move;

    // Submit a call action
    const { status, body } = await pokerApi.action(TABLE_ID, humanSeat, 'call');

    expect(status).toBe(200);
    expect(body.success).toBe(true);

    // After action: either the move advanced away from our seat,
    // or the step advanced to the next phase
    const gameAfter = body.game!;
    const stepChanged = gameAfter.stepName !== stepBefore;
    const moveChanged = gameAfter.move !== moveBefore;
    expect(stepChanged || moveChanged).toBe(true);
  });

  test('action from wrong seat is rejected', async ({ pokerApi }) => {
    await pokerApi.join(TABLE_ID, 'WrongSeat', 1);
    await pokerApi.advanceUntilHumanTurn(TABLE_ID, 1);

    // Try acting from a different seat
    const { status, body } = await pokerApi.action(TABLE_ID, 2, 'call');

    expect(status).toBe(400);
    expect(body.error).toBeTruthy();
  });

  test('fold action is accepted and game advances', async ({ pokerApi }) => {
    const joinRes = await pokerApi.join(TABLE_ID, 'FoldTest', 1);
    const humanSeat = joinRes.body.seat;

    await pokerApi.advanceUntilHumanTurn(TABLE_ID, humanSeat);

    const { status, body } = await pokerApi.action(TABLE_ID, humanSeat, 'fold');

    expect(status).toBe(200);
    expect(body.success).toBe(true);

    // After folding, the human player should be folded
    const foldedPlayer = body.players!.find((p) => p.seat === humanSeat);
    expect(foldedPlayer?.action).toBe('fold');
  });

  test('human can act in multiple betting rounds within one hand', async ({ pokerApi }) => {
    const joinRes = await pokerApi.join(TABLE_ID, 'MultiRound', 1);
    const humanSeat = joinRes.body.seat;

    const roundsSeen: string[] = [];

    // Try to act in up to 4 betting rounds (preflop, flop, turn, river)
    for (let round = 0; round < 4; round++) {
      let reachedHumanTurn = false;
      for (let i = 0; i < 32; i++) {
        const { body: table } = await pokerApi.getTable(TABLE_ID);

        // Check if hand completed (new hand started or past all betting)
        if (
          table.game.stepName === 'RECORD_STATS_AND_NEW_HAND' ||
          table.game.stepName === 'GAME_PREP'
        ) {
          break;
        }

        // Check if we're paused at human's turn in a new round
        if (
          BETTING_STEPS.includes(table.game.stepName as typeof BETTING_STEPS[number]) &&
          table.game.move === humanSeat &&
          !roundsSeen.includes(table.game.stepName)
        ) {
          roundsSeen.push(table.game.stepName);
          reachedHumanTurn = true;
          break;
        }

        await pokerApi.process(TABLE_ID);
      }

      if (!reachedHumanTurn) break;

      // Call in this round
      const { status } = await pokerApi.action(TABLE_ID, humanSeat, 'call');
      expect(status).toBe(200);
    }

    // Should have acted in at least 1 betting round
    expect(roundsSeen.length).toBeGreaterThanOrEqual(1);
  });

  test('chips are conserved after human actions', async ({ pokerApi }) => {
    await pokerApi.join(TABLE_ID, 'ChipTest', 1);

    // Get initial chip count
    const { body: initial } = await pokerApi.getTable(TABLE_ID);
    const initialChips = pokerApi.totalChips(initial);

    // Advance to human turn and submit an action
    await pokerApi.advanceUntilHumanTurn(TABLE_ID, 1);
    await pokerApi.action(TABLE_ID, 1, 'call');

    // Chips should still be conserved
    const { body: after } = await pokerApi.getTable(TABLE_ID);
    const afterChips = pokerApi.totalChips(after);
    expect(afterChips).toBeCloseTo(initialChips, 1);
  });

  test('full hand completes with human player actions', async ({ pokerApi }) => {
    const joinRes = await pokerApi.join(TABLE_ID, 'FullHand', 1);
    const humanSeat = joinRes.body.seat;

    const { body: initial } = await pokerApi.getTable(TABLE_ID);
    const initialGameNo = initial.game.gameNo;
    const initialChips = pokerApi.totalChips(initial);

    // Play through the hand: process steps, act when it's our turn
    for (let i = 0; i < 64; i++) {
      const { body: table } = await pokerApi.getTable(TABLE_ID);

      // Hand completed
      if (table.game.gameNo > initialGameNo) break;

      // It's our turn — call (or check if no bet to match)
      if (
        BETTING_STEPS.includes(table.game.stepName as typeof BETTING_STEPS[number]) &&
        table.game.move === humanSeat
      ) {
        const { status } = await pokerApi.action(TABLE_ID, humanSeat, 'call');
        if (status !== 200) {
          // If call fails, try check
          await pokerApi.action(TABLE_ID, humanSeat, 'check');
        }
        continue;
      }

      await pokerApi.process(TABLE_ID);
    }

    // Hand should have completed
    const { body: final } = await pokerApi.getTable(TABLE_ID);
    expect(final.game.gameNo).toBeGreaterThan(initialGameNo);

    // Chips conserved
    const finalChips = pokerApi.totalChips(final);
    expect(finalChips).toBeCloseTo(initialChips, 1);
  });
});
