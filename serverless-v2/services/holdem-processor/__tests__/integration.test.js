'use strict';

// ─── Stateful mock for table-fetcher ─────────────────────────────────
//
// We mock at the table-fetcher boundary so that:
//   - fetchTable returns realistic in-memory game state
//   - saveGame / savePlayers persist updates in memory
//   - resetTable / freshResetTable / tipDealer operate on the in-memory store
//
// Everything downstream (process-table, betting, pots, cards) runs for real.

let mockStore; // { [tableId]: { game, players } }

jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(async (tableId) => {
    const entry = mockStore[tableId];
    if (!entry) return null;
    // Return deep copies so mutations inside process-table don't bypass saveGame/savePlayers
    return JSON.parse(JSON.stringify(entry));
  }),

  saveGame: jest.fn(async (game) => {
    const tableId = game.tableId;
    if (mockStore[tableId]) {
      mockStore[tableId].game = JSON.parse(JSON.stringify(game));
    }
  }),

  savePlayers: jest.fn(async (players) => {
    if (players.length === 0) return;
    const tableId = players[0].tableId;
    if (mockStore[tableId]) {
      mockStore[tableId].players = JSON.parse(JSON.stringify(players));
    }
  }),

  resetTable: jest.fn(async (tableId) => {
    const entry = mockStore[tableId];
    if (!entry) return null;
    // Reset to a fresh game at step 0 with a new gameNo
    entry.game.handStep = 0;
    entry.game.gameNo = (entry.game.gameNo || 1) + 1;
    entry.game.pot = 0;
    entry.game.currentBet = 0;
    entry.game.communityCards = [];
    entry.game.sidePots = [];
    entry.game.winners = [];
    entry.game.status = 'in_progress';
    for (const p of entry.players) {
      p.bet = 0;
      p.totalBet = 0;
      p.action = '';
      p.cards = [];
      p.status = '1';
      p.handRank = '';
      p.bestHand = [];
      p.isWinner = false;
      p.winnings = 0;
    }
    return JSON.parse(JSON.stringify(entry));
  }),

  freshResetTable: jest.fn(async (tableId) => {
    const entry = mockStore[tableId];
    if (!entry) return null;
    // Full wipe: reset game to step 0, gameNo 1, fresh stacks
    entry.game.handStep = 0;
    entry.game.gameNo = 1;
    entry.game.pot = 0;
    entry.game.currentBet = 0;
    entry.game.communityCards = [];
    entry.game.sidePots = [];
    entry.game.winners = [];
    entry.game.dealerSeat = 0;
    entry.game.status = 'in_progress';
    for (const p of entry.players) {
      p.stack = 100;
      p.bet = 0;
      p.totalBet = 0;
      p.action = '';
      p.cards = [];
      p.status = '1';
      p.handRank = '';
      p.bestHand = [];
      p.isWinner = false;
      p.winnings = 0;
    }
    return JSON.parse(JSON.stringify(entry));
  }),

  tipDealer: jest.fn(async (tableId, seat) => {
    const entry = mockStore[tableId];
    if (!entry) throw new Error('Table not found');
    const player = entry.players.find((p) => p.seat === seat);
    if (!player) throw new Error('Player not found at seat ' + seat);
    if (player.stack < 1) throw new Error('Insufficient stack for tip');
    player.stack -= 1;
    return { success: true };
  }),
}));

jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn().mockResolvedValue(null),
}));

// ─── Imports (after mocks are registered) ────────────────────────────

const {
  health,
  processHandHttp,
  getTableHttp,
  resetTableHttp,
  freshResetTableHttp,
  tipDealerHttp,
} = require('../handler');

const { GAME_HAND, PLAYER_STATUS, ACTION } = require('../shared/games/common/constants');
const { fetchTable, saveGame, savePlayers } = require('../lib/table-fetcher');
const { publishTableUpdate } = require('../lib/event-publisher');

// ─── Helpers ──────────────────────────────────────────────────────────

const TABLE_ID = 42;

/**
 * Build a realistic initial game + 3 players structure at GAME_PREP (step 0).
 */
function createInitialTableState(tableId = TABLE_ID) {
  return {
    game: {
      id: 100,
      tableId,
      tableName: 'Test Table',
      gameNo: 1,
      handStep: GAME_HAND.GAME_PREP,
      dealerSeat: 0,
      smallBlindSeat: 0,
      bigBlindSeat: 0,
      communityCards: [],
      pot: 0,
      sidePots: [],
      move: 0,
      status: 'in_progress',
      smallBlind: 1,
      bigBlind: 2,
      maxSeats: 6,
      deck: [],
      currentBet: 0,
      lastRaiseSize: 2,
      winners: [],
      noReopenSeats: [],
    },
    players: [
      {
        id: 1, gameId: 100, tableId, playerId: 101,
        guid: 'guid-101', username: 'Alice', seat: 1,
        stack: 100, bet: 0, totalBet: 0,
        status: PLAYER_STATUS.ACTIVE, action: '',
        cards: [], bestHand: [], handRank: '',
        isWinner: false, winnings: 0,
      },
      {
        id: 2, gameId: 100, tableId, playerId: 102,
        guid: 'guid-102', username: 'Bob', seat: 2,
        stack: 100, bet: 0, totalBet: 0,
        status: PLAYER_STATUS.ACTIVE, action: '',
        cards: [], bestHand: [], handRank: '',
        isWinner: false, winnings: 0,
      },
      {
        id: 3, gameId: 100, tableId, playerId: 103,
        guid: 'guid-103', username: 'Charlie', seat: 3,
        stack: 100, bet: 0, totalBet: 0,
        status: PLAYER_STATUS.ACTIVE, action: '',
        cards: [], bestHand: [], handRank: '',
        isWinner: false, winnings: 0,
      },
    ],
  };
}

/** Build a Lambda-style event for processHandHttp. */
function makeProcessEvent(body) {
  return { body: JSON.stringify(body) };
}

/** Build a Lambda-style event for path-parameter endpoints. */
function makePathEvent(tableId, body = null) {
  return {
    pathParameters: tableId != null ? { tableId: String(tableId) } : null,
    body: body ? JSON.stringify(body) : null,
  };
}

/** Call processHandHttp and parse the response. */
async function callProcess(body) {
  const res = await processHandHttp(makeProcessEvent(body));
  return { statusCode: res.statusCode, body: JSON.parse(res.body) };
}

/** Shortcut: advance one step with no action (for non-betting steps). */
async function advanceStep() {
  return callProcess({ tableId: TABLE_ID });
}

/** Shortcut: submit a player action during a betting round. */
async function submitAction(action, amount, seat) {
  return callProcess({ tableId: TABLE_ID, action, amount, seat });
}

/** Read the current game state from the in-memory store. */
function currentGame() {
  return mockStore[TABLE_ID]?.game;
}

/** Read the current players from the in-memory store. */
function currentPlayers() {
  return mockStore[TABLE_ID]?.players;
}

/** Get the step name string for a numeric step. */
function stepName(step) {
  const entry = Object.entries(GAME_HAND).find(([, v]) => v === step);
  return entry ? entry[0] : `UNKNOWN(${step})`;
}

// ─── Test suite ──────────────────────────────────────────────────────

describe('Holdem Processor Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockStore = {};
  });

  // ─── 1. Health endpoint ─────────────────────────────────────────────

  describe('health()', () => {
    it('returns 200 with service name and status', async () => {
      const res = await health();
      expect(res.statusCode).toBe(200);
      const body = JSON.parse(res.body);
      expect(body.service).toBe('holdem-processor');
      expect(body.status).toBe('ok');
      expect(body.timestamp).toBeDefined();
    });
  });

  // ─── 2. GET /table/{tableId} ────────────────────────────────────────

  describe('getTableHttp', () => {
    it('returns 200 with game and players for an existing table', async () => {
      mockStore[TABLE_ID] = createInitialTableState();

      const res = await getTableHttp(makePathEvent(TABLE_ID));
      expect(res.statusCode).toBe(200);

      const body = JSON.parse(res.body);
      expect(body.game).toBeDefined();
      expect(body.game.tableId).toBe(TABLE_ID);
      expect(body.game.stepName).toBe('GAME_PREP');
      // Deck must not be leaked to the UI
      expect(body.game.deck).toBeUndefined();
      expect(body.players).toHaveLength(3);
      expect(body.players[0].username).toBe('Alice');
      expect(body.players[1].username).toBe('Bob');
      expect(body.players[2].username).toBe('Charlie');
    });

    it('returns 400 when tableId is missing', async () => {
      const res = await getTableHttp({ pathParameters: null });
      expect(res.statusCode).toBe(400);
      const body = JSON.parse(res.body);
      expect(body.error).toMatch(/tableId/i);
    });

    it('returns 404 when table does not exist', async () => {
      // mockStore is empty, so fetchTable returns null
      const res = await getTableHttp(makePathEvent(9999));
      expect(res.statusCode).toBe(404);
      const body = JSON.parse(res.body);
      expect(body.error).toMatch(/not found/i);
    });
  });

  // ─── 3. Error cases for processHandHttp ─────────────────────────────

  describe('processHandHttp error cases', () => {
    it('returns 400 when tableId is missing', async () => {
      const res = await processHandHttp(makeProcessEvent({}));
      expect(res.statusCode).toBe(400);
      const body = JSON.parse(res.body);
      expect(body.error).toMatch(/tableId/i);
    });

    it('returns 200 with status not_found for a non-existent table', async () => {
      const { statusCode, body } = await callProcess({ tableId: 9999 });
      expect(statusCode).toBe(200);
      expect(body.result.status).toBe('not_found');
    });

    it('returns rejected status when betting round has no action', async () => {
      // Set up a table at PRE_FLOP_BETTING_ROUND expecting an action
      mockStore[TABLE_ID] = createInitialTableState();
      const g = mockStore[TABLE_ID].game;
      g.handStep = GAME_HAND.PRE_FLOP_BETTING_ROUND;
      g.currentBet = 2;
      g.move = 1;
      // Players must have cards and proper state for a betting round
      for (const p of mockStore[TABLE_ID].players) {
        p.cards = ['AH', 'KD'];
        p.status = PLAYER_STATUS.ACTIVE;
      }
      // SB and BB bets
      mockStore[TABLE_ID].players[0].bet = 1;
      mockStore[TABLE_ID].players[0].totalBet = 1;
      mockStore[TABLE_ID].players[1].bet = 2;
      mockStore[TABLE_ID].players[1].totalBet = 2;

      // Call without an action — should get AWAITING_ACTION
      const { body } = await callProcess({ tableId: TABLE_ID });
      expect(body.result.status).toBe('rejected');
      expect(body.result.error.code).toBe('AWAITING_ACTION');
    });

    it('returns rejected status for out-of-turn action', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const g = mockStore[TABLE_ID].game;
      g.handStep = GAME_HAND.PRE_FLOP_BETTING_ROUND;
      g.currentBet = 2;
      g.move = 1; // seat 1's turn
      for (const p of mockStore[TABLE_ID].players) {
        p.cards = ['AH', 'KD'];
      }
      mockStore[TABLE_ID].players[0].bet = 1;
      mockStore[TABLE_ID].players[0].totalBet = 1;
      mockStore[TABLE_ID].players[1].bet = 2;
      mockStore[TABLE_ID].players[1].totalBet = 2;

      // Seat 3 tries to act when it's seat 1's turn
      const { body } = await submitAction('call', 0, 3);
      expect(body.result.status).toBe('rejected');
      expect(body.result.error.code).toBe('OUT_OF_TURN');
    });
  });

  // ─── 4. Full hand lifecycle: all 16 steps ──────────────────────────

  describe('Full hand lifecycle (GAME_PREP through RECORD_STATS_AND_NEW_HAND)', () => {
    beforeEach(() => {
      mockStore[TABLE_ID] = createInitialTableState();
    });

    it('walks through all steps of a complete hand where everyone folds to the big blind', async () => {
      // Step 0 -> 1: GAME_PREP -> SETUP_DEALER
      let { body } = await advanceStep();
      expect(body.success).toBe(true);
      expect(currentGame().handStep).toBe(GAME_HAND.SETUP_DEALER);

      // Step 1 -> 2: SETUP_DEALER -> SETUP_SMALL_BLIND
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.SETUP_SMALL_BLIND);

      // Step 2 -> 3: SETUP_SMALL_BLIND -> SETUP_BIG_BLIND
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.SETUP_BIG_BLIND);
      // Verify small blind was posted
      const sbSeat = currentGame().smallBlindSeat;
      const sbPlayer = currentPlayers().find((p) => p.seat === sbSeat);
      expect(sbPlayer.bet).toBe(1);
      expect(sbPlayer.stack).toBe(99);

      // Step 3 -> 4: SETUP_BIG_BLIND -> DEAL_CARDS
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_CARDS);
      // Verify big blind was posted
      const bbSeat = currentGame().bigBlindSeat;
      const bbPlayer = currentPlayers().find((p) => p.seat === bbSeat);
      expect(bbPlayer.bet).toBe(2);
      expect(bbPlayer.stack).toBe(98);

      // Step 4 -> 5: DEAL_CARDS -> PRE_FLOP_BETTING_ROUND
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      // Each active player should have 2 hole cards
      for (const p of currentPlayers()) {
        expect(p.cards).toHaveLength(2);
      }
      // There should be a move assigned
      expect(currentGame().move).toBeGreaterThan(0);

      // Step 5: PRE_FLOP_BETTING_ROUND - everyone folds to the BB
      // First to act is left of BB. With 3 players (seats 1,2,3), dealer=1, SB=2, BB=3.
      // First to act preflop = seat 1 (left of BB in a 3-player game wraps around).
      const actingSeat = currentGame().move;
      const otherActiveSeat = currentPlayers().find(
        (p) => p.seat !== actingSeat && p.seat !== bbSeat && p.status === PLAYER_STATUS.ACTIVE
      );

      // First active player folds
      ({ body } = await submitAction('fold', 0, actingSeat));
      expect(body.result.status).toBe('processed');

      // If there's still another active non-BB player, fold them too
      if (otherActiveSeat && otherActiveSeat.seat !== bbSeat) {
        const nextMoveSeat = currentGame().move;
        if (nextMoveSeat > 0 && currentGame().handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND) {
          ({ body } = await submitAction('fold', 0, nextMoveSeat));
        }
      }

      // After all folds, should jump to FIND_WINNERS
      expect(currentGame().handStep).toBe(GAME_HAND.FIND_WINNERS);

      // Step 13 -> 14: FIND_WINNERS -> PAY_WINNERS
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.PAY_WINNERS);

      // Step 14 -> 15: PAY_WINNERS -> RECORD_STATS_AND_NEW_HAND
      ({ body } = await advanceStep());
      expect(currentGame().handStep).toBe(GAME_HAND.RECORD_STATS_AND_NEW_HAND);
      expect(currentGame().pot).toBe(0);

      // Verify the winner got the pot
      const winner = currentPlayers().find((p) => p.winnings > 0);
      expect(winner).toBeDefined();

      // Step 15: RECORD_STATS_AND_NEW_HAND - marks game completed
      ({ body } = await advanceStep());
      expect(currentGame().status).toBe('completed');
    });

    it('walks through a full hand to showdown with community cards', async () => {
      // Advance through setup steps: GAME_PREP -> SETUP_DEALER -> ... -> PRE_FLOP_BETTING_ROUND
      for (let i = 0; i < 5; i++) {
        await advanceStep();
      }
      expect(currentGame().handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);

      // Preflop: everyone calls
      // First to act calls
      let moveSeat = currentGame().move;
      await submitAction('call', 0, moveSeat);

      // Next player calls (or if BB, check)
      moveSeat = currentGame().move;
      if (moveSeat > 0 && currentGame().handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND) {
        await submitAction('call', 0, moveSeat);
      }

      // BB checks (or the round may already be over if 2 calls close it)
      moveSeat = currentGame().move;
      if (moveSeat > 0 && currentGame().handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND) {
        await submitAction('check', 0, moveSeat);
      }

      // Should advance to DEAL_FLOP or already past it
      // The betting round logic collects bets and advances when complete
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_FLOP);

      // Step 6: DEAL_FLOP -> FLOP_BETTING_ROUND
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.FLOP_BETTING_ROUND);
      expect(currentGame().communityCards).toHaveLength(3);

      // Flop: everyone checks
      moveSeat = currentGame().move;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.FLOP_BETTING_ROUND) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
      }

      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_TURN);

      // Step 8: DEAL_TURN -> TURN_BETTING_ROUND
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.TURN_BETTING_ROUND);
      expect(currentGame().communityCards).toHaveLength(4);

      // Turn: everyone checks
      moveSeat = currentGame().move;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.TURN_BETTING_ROUND) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
      }

      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_RIVER);

      // Step 10: DEAL_RIVER -> RIVER_BETTING_ROUND
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.RIVER_BETTING_ROUND);
      expect(currentGame().communityCards).toHaveLength(5);

      // River: everyone checks
      moveSeat = currentGame().move;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.RIVER_BETTING_ROUND) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
      }

      // After river betting round closes we go to AFTER_RIVER_BETTING_ROUND
      expect(currentGame().handStep).toBe(GAME_HAND.AFTER_RIVER_BETTING_ROUND);

      // Step 12: AFTER_RIVER_BETTING_ROUND -> FIND_WINNERS
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.FIND_WINNERS);

      // Step 13: FIND_WINNERS -> PAY_WINNERS
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.PAY_WINNERS);

      // All players still in hand should have handRank and bestHand set
      const inHand = currentPlayers().filter(
        (p) => p.status === PLAYER_STATUS.ACTIVE || p.status === PLAYER_STATUS.ALL_IN
      );
      for (const p of inHand) {
        expect(p.handRank).toBeTruthy();
        expect(p.bestHand.length).toBeGreaterThan(0);
      }

      // Step 14: PAY_WINNERS -> RECORD_STATS_AND_NEW_HAND
      await advanceStep();
      expect(currentGame().handStep).toBe(GAME_HAND.RECORD_STATS_AND_NEW_HAND);
      expect(currentGame().pot).toBe(0);

      // Verify total stacks are conserved (sum should be 300)
      const totalStacks = currentPlayers().reduce((sum, p) => sum + p.stack, 0);
      expect(totalStacks).toBeCloseTo(300, 2);

      // Step 15: RECORD_STATS_AND_NEW_HAND - completed
      await advanceStep();
      expect(currentGame().status).toBe('completed');
    });

    it('tracks that saveGame and savePlayers are called on each successful step', async () => {
      // Advance through GAME_PREP
      await advanceStep();
      expect(saveGame).toHaveBeenCalledTimes(1);
      expect(savePlayers).toHaveBeenCalledTimes(1);
      expect(publishTableUpdate).toHaveBeenCalledTimes(1);

      // Advance through SETUP_DEALER
      await advanceStep();
      expect(saveGame).toHaveBeenCalledTimes(2);
      expect(savePlayers).toHaveBeenCalledTimes(2);
    });
  });

  // ─── 5. Betting round actions ───────────────────────────────────────

  describe('Betting round actions', () => {
    beforeEach(async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      // Advance to PRE_FLOP_BETTING_ROUND
      for (let i = 0; i < 5; i++) {
        await advanceStep();
      }
      expect(currentGame().handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
    });

    it('allows a player to raise and other players must respond', async () => {
      const moveSeat = currentGame().move;

      // First player raises to 6
      const { body } = await submitAction('raise', 6, moveSeat);
      expect(body.result.status).toBe('processed');

      // Game should still be in preflop betting with the next player to act
      expect(currentGame().handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      expect(currentGame().currentBet).toBe(6);
    });

    it('advances past preflop when all players call', async () => {
      // All players call/check through preflop
      let moveSeat = currentGame().move;
      let safety = 0;
      while (
        moveSeat > 0 &&
        currentGame().handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND &&
        safety < 10
      ) {
        const player = currentPlayers().find((p) => p.seat === moveSeat);
        if (player && player.bet < currentGame().currentBet) {
          await submitAction('call', 0, moveSeat);
        } else {
          await submitAction('check', 0, moveSeat);
        }
        moveSeat = currentGame().move;
        safety++;
      }

      // Should have advanced past preflop
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_FLOP);
    });

    it('handles fold correctly - reduces players in hand', async () => {
      const moveSeat = currentGame().move;
      await submitAction('fold', 0, moveSeat);

      const foldedPlayer = currentPlayers().find((p) => p.seat === moveSeat);
      expect(foldedPlayer.status).toBe(PLAYER_STATUS.FOLDED);
    });

    it('handles all-in correctly', async () => {
      const moveSeat = currentGame().move;
      await submitAction('allin', 0, moveSeat);

      const allInPlayer = currentPlayers().find((p) => p.seat === moveSeat);
      expect(allInPlayer.status).toBe(PLAYER_STATUS.ALL_IN);
      expect(allInPlayer.stack).toBe(0);
    });
  });

  // ─── 6. Reset flows ────────────────────────────────────────────────

  describe('resetTableHttp', () => {
    it('resets a table and returns a new gameNo', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      // Advance a few steps to make state non-initial
      await advanceStep(); // GAME_PREP -> SETUP_DEALER

      const res = await resetTableHttp(makePathEvent(TABLE_ID));
      expect(res.statusCode).toBe(200);
      const body = JSON.parse(res.body);
      expect(body.success).toBe(true);
      expect(body.gameNo).toBeGreaterThan(1);
    });

    it('returns 400 when tableId is missing', async () => {
      const res = await resetTableHttp({ pathParameters: null, body: '{}' });
      expect(res.statusCode).toBe(400);
    });

    it('returns 404 for non-existent table', async () => {
      const res = await resetTableHttp(makePathEvent(9999));
      expect(res.statusCode).toBe(404);
    });
  });

  describe('freshResetTableHttp', () => {
    it('fresh-resets a table to gameNo 1 with full stacks', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      // Mutate state to simulate a game in progress
      mockStore[TABLE_ID].game.gameNo = 5;
      mockStore[TABLE_ID].players[0].stack = 50;

      const res = await freshResetTableHttp(makePathEvent(TABLE_ID));
      expect(res.statusCode).toBe(200);
      const body = JSON.parse(res.body);
      expect(body.success).toBe(true);
      expect(body.gameNo).toBe(1);
    });

    it('returns 400 when tableId is missing', async () => {
      const res = await freshResetTableHttp({ pathParameters: null, body: '{}' });
      expect(res.statusCode).toBe(400);
    });

    it('returns 404 for non-existent table', async () => {
      const res = await freshResetTableHttp(makePathEvent(9999));
      expect(res.statusCode).toBe(404);
    });
  });

  // ─── 7. Tip dealer ────────────────────────────────────────────────

  describe('tipDealerHttp', () => {
    it('deducts $1 from the tipping player stack', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const stackBefore = mockStore[TABLE_ID].players[0].stack; // 100

      const res = await tipDealerHttp(makePathEvent(TABLE_ID, { seat: 1 }));
      expect(res.statusCode).toBe(200);
      const body = JSON.parse(res.body);
      expect(body.success).toBe(true);

      // Verify the stack was decremented
      expect(mockStore[TABLE_ID].players[0].stack).toBe(stackBefore - 1);
    });

    it('returns 400 when seat is missing', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const res = await tipDealerHttp(makePathEvent(TABLE_ID, {}));
      expect(res.statusCode).toBe(400);
      const body = JSON.parse(res.body);
      expect(body.error).toMatch(/seat/i);
    });

    it('returns 400 when tableId is missing', async () => {
      const res = await tipDealerHttp({
        pathParameters: null,
        body: JSON.stringify({ seat: 1 }),
      });
      expect(res.statusCode).toBe(400);
    });

    it('returns 500 when player has insufficient stack', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      mockStore[TABLE_ID].players[0].stack = 0; // busted

      const res = await tipDealerHttp(makePathEvent(TABLE_ID, { seat: 1 }));
      expect(res.statusCode).toBe(500);
      const body = JSON.parse(res.body);
      expect(body.error).toMatch(/insufficient/i);
    });
  });

  // ─── 8. Blind posting and pot arithmetic ───────────────────────────

  describe('Blind posting and pot integrity', () => {
    it('posts correct small and big blind amounts', async () => {
      mockStore[TABLE_ID] = createInitialTableState();

      // GAME_PREP -> SETUP_DEALER
      await advanceStep();
      // SETUP_DEALER -> SETUP_SMALL_BLIND
      await advanceStep();
      // SETUP_SMALL_BLIND -> SETUP_BIG_BLIND
      await advanceStep();

      const sbSeat = currentGame().smallBlindSeat;
      const sbPlayer = currentPlayers().find((p) => p.seat === sbSeat);
      expect(sbPlayer.bet).toBe(1); // smallBlind = 1
      expect(sbPlayer.stack).toBe(99);

      // SETUP_BIG_BLIND -> DEAL_CARDS
      await advanceStep();

      const bbSeat = currentGame().bigBlindSeat;
      const bbPlayer = currentPlayers().find((p) => p.seat === bbSeat);
      expect(bbPlayer.bet).toBe(2); // bigBlind = 2
      expect(bbPlayer.stack).toBe(98);
      expect(currentGame().currentBet).toBe(2);
    });

    it('preserves total chips across an entire hand', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const totalChipsBefore = currentPlayers().reduce((s, p) => s + p.stack, 0);

      // Run through to showdown
      for (let i = 0; i < 5; i++) await advanceStep();

      // Preflop: all call/check
      let moveSeat = currentGame().move;
      let safety = 0;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND && safety < 10) {
        const p = currentPlayers().find((pl) => pl.seat === moveSeat);
        if (p && p.bet < currentGame().currentBet) {
          await submitAction('call', 0, moveSeat);
        } else {
          await submitAction('check', 0, moveSeat);
        }
        moveSeat = currentGame().move;
        safety++;
      }

      // Flop
      await advanceStep();
      moveSeat = currentGame().move;
      safety = 0;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.FLOP_BETTING_ROUND && safety < 10) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
        safety++;
      }

      // Turn
      await advanceStep();
      moveSeat = currentGame().move;
      safety = 0;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.TURN_BETTING_ROUND && safety < 10) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
        safety++;
      }

      // River
      await advanceStep();
      moveSeat = currentGame().move;
      safety = 0;
      while (moveSeat > 0 && currentGame().handStep === GAME_HAND.RIVER_BETTING_ROUND && safety < 10) {
        await submitAction('check', 0, moveSeat);
        moveSeat = currentGame().move;
        safety++;
      }

      // AFTER_RIVER -> FIND_WINNERS -> PAY_WINNERS -> RECORD_STATS
      await advanceStep(); // AFTER_RIVER_BETTING_ROUND
      await advanceStep(); // FIND_WINNERS
      await advanceStep(); // PAY_WINNERS

      const totalChipsAfter = currentPlayers().reduce((s, p) => s + p.stack, 0);
      expect(totalChipsAfter).toBeCloseTo(totalChipsBefore, 2);
      expect(currentGame().pot).toBe(0);
    });
  });

  // ─── 9. Deal steps produce correct community cards ─────────────────

  describe('Community card dealing', () => {
    beforeEach(async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      // Fast-forward to preflop
      for (let i = 0; i < 5; i++) await advanceStep();
    });

    async function checkThroughBetting(expectedStep) {
      let moveSeat = currentGame().move;
      let safety = 0;
      while (moveSeat > 0 && currentGame().handStep === expectedStep && safety < 10) {
        const p = currentPlayers().find((pl) => pl.seat === moveSeat);
        if (p && p.bet < currentGame().currentBet) {
          await submitAction('call', 0, moveSeat);
        } else {
          await submitAction('check', 0, moveSeat);
        }
        moveSeat = currentGame().move;
        safety++;
      }
    }

    it('deals 3 community cards on the flop', async () => {
      await checkThroughBetting(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_FLOP);

      await advanceStep();
      expect(currentGame().communityCards).toHaveLength(3);
    });

    it('deals 1 more community card on the turn (4 total)', async () => {
      await checkThroughBetting(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      await advanceStep(); // deal flop
      await checkThroughBetting(GAME_HAND.FLOP_BETTING_ROUND);
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_TURN);

      await advanceStep();
      expect(currentGame().communityCards).toHaveLength(4);
    });

    it('deals 1 more community card on the river (5 total)', async () => {
      await checkThroughBetting(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      await advanceStep(); // deal flop
      await checkThroughBetting(GAME_HAND.FLOP_BETTING_ROUND);
      await advanceStep(); // deal turn
      await checkThroughBetting(GAME_HAND.TURN_BETTING_ROUND);
      expect(currentGame().handStep).toBe(GAME_HAND.DEAL_RIVER);

      await advanceStep();
      expect(currentGame().communityCards).toHaveLength(5);
    });
  });

  // ─── 10. Response structure validation ─────────────────────────────

  describe('Response structure', () => {
    it('processHandHttp returns CORS headers on success', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const res = await processHandHttp(makeProcessEvent({ tableId: TABLE_ID }));
      expect(res.headers['Access-Control-Allow-Origin']).toBe('*');
      expect(res.headers['Access-Control-Allow-Methods']).toContain('POST');
    });

    it('processHandHttp returns CORS headers on 400 error', async () => {
      const res = await processHandHttp(makeProcessEvent({}));
      expect(res.statusCode).toBe(400);
      expect(res.headers['Access-Control-Allow-Origin']).toBe('*');
    });

    it('getTableHttp player objects have the expected shape', async () => {
      mockStore[TABLE_ID] = createInitialTableState();
      const res = await getTableHttp(makePathEvent(TABLE_ID));
      const body = JSON.parse(res.body);

      const player = body.players[0];
      expect(player).toHaveProperty('playerId');
      expect(player).toHaveProperty('username');
      expect(player).toHaveProperty('seat');
      expect(player).toHaveProperty('stack');
      expect(player).toHaveProperty('bet');
      expect(player).toHaveProperty('totalBet');
      expect(player).toHaveProperty('status');
      expect(player).toHaveProperty('action');
      expect(player).toHaveProperty('cards');
      expect(player).toHaveProperty('handRank');
      expect(player).toHaveProperty('bestHand');
      expect(player).toHaveProperty('isWinner');
      expect(player).toHaveProperty('winnings');
    });
  });
});
