'use strict';

jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(),
  saveGame: jest.fn(),
  savePlayers: jest.fn(),
}));
jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn(),
}));

const {
  gamePrep,
  setupDealer,
  setupSmallBlind,
  setupBigBlind,
  dealCards,
  bettingRound,
  applyAction,
  dealFlop,
  dealTurn,
  dealRiver,
  findHandWinners,
  payWinners,
  recordStatsAndNewHand,
} = require('../lib/process-table');
const { GAME_HAND, PLAYER_STATUS, ACTION } = require('../shared/games/common/constants');
const {
  isBettingRoundComplete,
  getValidActions,
  validateAction,
  getMinRaiseTo,
} = require('../shared/games/common/betting');
const { calculatePots, distributePots } = require('../shared/games/common/pots');
const { createDeck, shuffle } = require('../shared/games/common/cards');

// ─── Test helpers ─────────────────────────────────────────────────────

function createTestGame(overrides = {}) {
  return {
    id: 1,
    tableId: 1,
    gameNo: 1,
    handStep: GAME_HAND.GAME_PREP,
    dealerSeat: 0,
    smallBlindSeat: 0,
    bigBlindSeat: 0,
    communityCards: [],
    pot: 0,
    currentBet: 0,
    sidePots: [],
    move: 0,
    status: 'in_progress',
    smallBlind: 1,
    bigBlind: 2,
    maxSeats: 6,
    deck: [],
    winners: [],
    lastRaiseSize: 2,
    noReopenSeats: [],
    ...overrides,
  };
}

function createTestPlayers(count = 3, stack = 100) {
  return Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    gameId: 1,
    tableId: 1,
    playerId: i + 1,
    guid: `p${i + 1}-uuid`,
    username: `Player${i + 1}`,
    seat: i + 1,
    stack,
    bet: 0,
    totalBet: 0,
    status: PLAYER_STATUS.ACTIVE,
    action: '',
    cards: [],
    handRank: '',
    winnings: 0,
  }));
}

/** Run setup steps and return state ready for preflop betting. */
function setupHand(playerCount = 3, stack = 100) {
  const game = createTestGame({ maxSeats: 6 });
  const players = createTestPlayers(playerCount, stack);
  let state = gamePrep(game, players);
  state = setupDealer(state.game, state.players);
  state = setupSmallBlind(state.game, state.players);
  state = setupBigBlind(state.game, state.players);
  state = dealCards(state.game, state.players);
  return state;
}

/** Send one action to the betting round. */
function act(state, round, action, amount, seat) {
  const seatNum = seat || state.game.move;
  return bettingRound(state.game, state.players, round, {
    action,
    amount,
    seat: seatNum,
  });
}

/** Keep calling with an action until the hand step changes. */
function runRound(state, round, stepValue, action = ACTION.CALL) {
  let guard = 0;
  while (state.game.handStep === stepValue && guard < 20) {
    state = act(state, round, action, 0, state.game.move);
    if (state.error) throw new Error(JSON.stringify(state.error));
    guard++;
  }
  expect(guard).toBeLessThan(20);
  return state;
}

// ─── Tests ────────────────────────────────────────────────────────────

describe('Process Table — Hand State Machine', () => {
  // ── Setup steps ───────────────────────────────────────────────────

  describe('gamePrep', () => {
    it('resets player states and creates a shuffled deck', () => {
      const game = createTestGame();
      const players = createTestPlayers();
      const result = gamePrep(game, players);

      expect(result.game.handStep).toBe(GAME_HAND.SETUP_DEALER);
      expect(result.game.deck).toHaveLength(52);
      expect(result.game.pot).toBe(0);
      expect(result.game.lastRaiseSize).toBe(2);
      result.players.forEach((p) => {
        expect(p.bet).toBe(0);
        expect(p.action).toBe('');
        expect(p.cards).toEqual([]);
      });
    });
  });

  describe('setupDealer', () => {
    it('assigns dealer and advances to small blind', () => {
      const game = createTestGame({ handStep: GAME_HAND.SETUP_DEALER });
      const players = createTestPlayers();
      const result = setupDealer(game, players);

      expect(result.game.dealerSeat).toBeGreaterThan(0);
      expect(result.game.handStep).toBe(GAME_HAND.SETUP_SMALL_BLIND);
    });

    it('skips hand if fewer than 2 players', () => {
      const game = createTestGame({ handStep: GAME_HAND.SETUP_DEALER });
      const players = createTestPlayers(1);
      const result = setupDealer(game, players);

      expect(result.game.handStep).toBe(GAME_HAND.RECORD_STATS_AND_NEW_HAND);
    });
  });

  describe('setupSmallBlind', () => {
    it('deducts small blind from the correct player', () => {
      const game = createTestGame({
        handStep: GAME_HAND.SETUP_SMALL_BLIND,
        dealerSeat: 1,
      });
      const players = createTestPlayers();
      const result = setupSmallBlind(game, players);

      expect(result.game.handStep).toBe(GAME_HAND.SETUP_BIG_BLIND);
      const posted = result.players.filter((p) => p.bet > 0);
      expect(posted).toHaveLength(1);
      expect(posted[0].bet).toBe(1);
      expect(posted[0].stack).toBe(99);
    });
  });

  describe('setupBigBlind', () => {
    it('deducts big blind and sets current bet', () => {
      const game = createTestGame({
        handStep: GAME_HAND.SETUP_BIG_BLIND,
        dealerSeat: 1,
        smallBlindSeat: 2,
      });
      const players = createTestPlayers();
      players[1].bet = 1;
      players[1].totalBet = 1;
      players[1].stack = 99;

      const result = setupBigBlind(game, players);

      expect(result.game.handStep).toBe(GAME_HAND.DEAL_CARDS);
      expect(result.game.currentBet).toBe(2);
      expect(result.game.lastRaiseSize).toBe(2);
      const bbPlayer = result.players.find(
        (p) => p.seat === result.game.bigBlindSeat
      );
      expect(bbPlayer.bet).toBe(2);
    });
  });

  describe('dealCards', () => {
    it('deals 2 cards to each active player', () => {
      const game = createTestGame({
        handStep: GAME_HAND.DEAL_CARDS,
        bigBlindSeat: 3,
      });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers();
      const result = dealCards(game, players);

      expect(result.game.handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      result.players.forEach((p) => expect(p.cards).toHaveLength(2));
      expect(result.game.deck).toHaveLength(46);
    });
  });

  describe('dealFlop / dealTurn / dealRiver', () => {
    it('deals 3 community cards on flop', () => {
      const game = createTestGame({ dealerSeat: 1 });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers();
      const result = dealFlop(game, players);
      expect(result.game.communityCards).toHaveLength(3);
      expect(result.game.handStep).toBe(GAME_HAND.FLOP_BETTING_ROUND);
    });

    it('deals 1 card on turn', () => {
      const game = createTestGame({
        communityCards: ['AH', 'KD', 'QS'],
        dealerSeat: 1,
      });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers();
      const result = dealTurn(game, players);
      expect(result.game.communityCards).toHaveLength(4);
    });

    it('deals 1 card on river', () => {
      const game = createTestGame({
        communityCards: ['AH', 'KD', 'QS', 'JC'],
        dealerSeat: 1,
      });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers();
      const result = dealRiver(game, players);
      expect(result.game.communityCards).toHaveLength(5);
    });
  });

  // ── Action legality ───────────────────────────────────────────────

  describe('Action legality', () => {
    it('CHECK allowed when toCall === 0', () => {
      const game = createTestGame({ currentBet: 0, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).toContain(ACTION.CHECK);
    });

    it('CHECK not allowed when toCall > 0', () => {
      const game = createTestGame({ currentBet: 4, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).not.toContain(ACTION.CHECK);
    });

    it('BET allowed when currentBet === 0', () => {
      const game = createTestGame({ currentBet: 0, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).toContain(ACTION.BET);
    });

    it('BET not allowed when currentBet > 0', () => {
      const game = createTestGame({ currentBet: 4, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).not.toContain(ACTION.BET);
    });

    it('CALL allowed when toCall > 0', () => {
      const game = createTestGame({ currentBet: 4, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).toContain(ACTION.CALL);
    });

    it('RAISE allowed when currentBet > 0 and enough stack', () => {
      const game = createTestGame({ currentBet: 4, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).toContain(ACTION.RAISE);
    });

    it('RAISE not available when stack too small for min raise', () => {
      const game = createTestGame({ currentBet: 4, bigBlind: 2, lastRaiseSize: 2 });
      // minRaiseTo = 4+2 = 6. chipsNeeded = 6-0 = 6. stack = 5 < 6.
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 5, seat: 1 };
      const actions = getValidActions(player, game);
      expect(actions).not.toContain(ACTION.RAISE);
      expect(actions).toContain(ACTION.ALLIN);
    });

    it('FOLD always allowed', () => {
      const game = createTestGame({ currentBet: 0 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      expect(getValidActions(player, game)).toContain(ACTION.FOLD);
    });

    it('ALLIN available when stack > 0', () => {
      const game = createTestGame({ currentBet: 0, bigBlind: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 1, seat: 1 };
      expect(getValidActions(player, game)).toContain(ACTION.ALLIN);
    });

    it('normalizes CALL to CHECK when toCall=0', () => {
      const game = createTestGame({ currentBet: 2, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 2, stack: 98, seat: 1 };
      const result = validateAction(player, game, { action: 'call' });
      expect(result.valid).toBe(true);
      expect(result.action).toBe(ACTION.CHECK);
    });

    it('normalizes BET to RAISE when currentBet > 0', () => {
      const game = createTestGame({ currentBet: 2, bigBlind: 2, lastRaiseSize: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const result = validateAction(player, game, { action: 'bet', amount: 6 });
      expect(result.valid).toBe(true);
      expect(result.action).toBe(ACTION.RAISE);
    });
  });

  // ── Out-of-turn rejection ─────────────────────────────────────────

  describe('Out-of-turn rejection', () => {
    it('rejects action from wrong seat without mutating state', () => {
      const state = setupHand(3);
      const moveBefore = state.game.move;
      const stacksBefore = state.players.map((p) => p.stack);

      const result = bettingRound(state.game, state.players, 'preflop', {
        action: ACTION.FOLD,
        seat: 99,
      });

      expect(result.error).toBeDefined();
      expect(result.error.code).toBe('OUT_OF_TURN');
      expect(result.game.move).toBe(moveBefore);
      expect(result.players.map((p) => p.stack)).toEqual(stacksBefore);
    });

    it('rejects when no action is provided', () => {
      const state = setupHand(3);
      const result = bettingRound(state.game, state.players, 'preflop', null);

      expect(result.error).toBeDefined();
      expect(result.error.code).toBe('AWAITING_ACTION');
    });
  });

  // ── Min-raise and re-raise ────────────────────────────────────────

  describe('Min-raise and re-raise rules', () => {
    it('min raise is currentBet + lastRaiseSize', () => {
      const game = createTestGame({ currentBet: 6, lastRaiseSize: 4, bigBlind: 2 });
      expect(getMinRaiseTo(game)).toBe(10);
    });

    it('rejects raise below minimum', () => {
      // currentBet=2, lastRaiseSize=2, minRaiseTo=4
      const game = createTestGame({ currentBet: 2, lastRaiseSize: 2, bigBlind: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const result = validateAction(player, game, { action: 'raise', amount: 3 });
      expect(result.valid).toBe(false);
      expect(result.reason).toBe('RAISE_TOO_SMALL');
    });

    it('accepts raise at minimum', () => {
      const game = createTestGame({ currentBet: 2, lastRaiseSize: 2, bigBlind: 2 });
      const player = { status: PLAYER_STATUS.ACTIVE, bet: 0, stack: 100, seat: 1 };
      const result = validateAction(player, game, { action: 'raise', amount: 4 });
      expect(result.valid).toBe(true);
      expect(result.amount).toBe(4);
    });

    it('tracks lastRaiseSize through re-raises', () => {
      const state = setupHand(3);
      const round = 'preflop';

      // UTG raises to 6 (increment 4 over BB of 2)
      let s = act(state, round, ACTION.RAISE, 6);
      expect(s.game.lastRaiseSize).toBe(4);
      expect(s.game.currentBet).toBe(6);

      // Next player re-raises to 14 (increment 8)
      s = act(s, round, ACTION.RAISE, 14);
      expect(s.game.lastRaiseSize).toBe(8);
      expect(s.game.currentBet).toBe(14);

      // Min re-raise is now 14+8 = 22
      expect(getMinRaiseTo(s.game)).toBe(22);
    });
  });

  // ── Short all-in reopen ───────────────────────────────────────────

  describe('Short all-in reopen/no-reopen', () => {
    it('short all-in does NOT reopen action for previous actors', () => {
      // 3 players. P1 bets, P2 short all-in, P3 calls, back to P1: can't raise.
      const game = createTestGame({
        currentBet: 0,
        bigBlind: 2,
        lastRaiseSize: 2,
        handStep: GAME_HAND.FLOP_BETTING_ROUND,
        dealerSeat: 3,
        move: 1,
        noReopenSeats: [],
      });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers(3, 100);
      // Give cards so they're valid
      players.forEach((p) => (p.cards = ['AH', 'KD']));
      game.communityCards = ['2C', '3D', '5H'];

      // P1 bets 10
      let state = bettingRound(game, players, 'flop', {
        action: ACTION.BET,
        amount: 10,
        seat: 1,
      });
      expect(state.game.currentBet).toBe(10);
      expect(state.game.lastRaiseSize).toBe(10);

      // P2 all-in for 13 (short raise: increment 3 < lastRaise 10)
      state.players[1].stack = 13;
      state = bettingRound(state.game, state.players, 'flop', {
        action: ACTION.ALLIN,
        seat: 2,
      });
      expect(state.game.currentBet).toBe(13);
      // lastRaiseSize should NOT change (short raise)
      expect(state.game.lastRaiseSize).toBe(10);

      // P3 calls
      state = bettingRound(state.game, state.players, 'flop', {
        action: ACTION.CALL,
        seat: 3,
      });

      // P1 should have action reset (needs to match 13) but can't raise
      expect(state.game.noReopenSeats).toContain(1);
      const p1Actions = getValidActions(state.players[0], state.game);
      expect(p1Actions).not.toContain(ACTION.RAISE);
      expect(p1Actions).toContain(ACTION.CALL);
    });

    it('full raise DOES reopen action', () => {
      const game = createTestGame({
        currentBet: 0,
        bigBlind: 2,
        lastRaiseSize: 2,
        handStep: GAME_HAND.FLOP_BETTING_ROUND,
        dealerSeat: 3,
        move: 1,
        noReopenSeats: [],
      });
      game.deck = shuffle(createDeck());
      const players = createTestPlayers(3, 100);
      players.forEach((p) => (p.cards = ['AH', 'KD']));
      game.communityCards = ['2C', '3D', '5H'];

      // P1 bets 10
      let state = bettingRound(game, players, 'flop', {
        action: ACTION.BET,
        amount: 10,
        seat: 1,
      });

      // P2 raises to 20 (full raise: increment 10 >= lastRaise 10)
      state = bettingRound(state.game, state.players, 'flop', {
        action: ACTION.RAISE,
        amount: 20,
        seat: 2,
      });
      expect(state.game.noReopenSeats).toEqual([]);

      // P3 calls
      state = bettingRound(state.game, state.players, 'flop', {
        action: ACTION.CALL,
        seat: 3,
      });

      // P1 should be able to raise (action was reopened)
      const p1Actions = getValidActions(state.players[0], state.game);
      expect(p1Actions).toContain(ACTION.RAISE);
    });
  });

  // ── Betting round completion ──────────────────────────────────────

  describe('isBettingRoundComplete', () => {
    it('returns true when all active players have acted and matched', () => {
      const players = [
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 4 },
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 4 },
        { status: PLAYER_STATUS.FOLDED, action: 'fold', bet: 0 },
      ];
      expect(isBettingRoundComplete(players, 4)).toBe(true);
    });

    it('returns false when a player has not acted', () => {
      const players = [
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 4 },
        { status: PLAYER_STATUS.ACTIVE, action: '', bet: 4 },
      ];
      expect(isBettingRoundComplete(players, 4)).toBe(false);
    });

    it('returns false when a player bet does not match', () => {
      const players = [
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 4 },
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 2 },
      ];
      expect(isBettingRoundComplete(players, 4)).toBe(false);
    });

    it('ignores all-in players', () => {
      const players = [
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 10 },
        { status: PLAYER_STATUS.ALL_IN, action: 'allin', bet: 5 },
      ];
      expect(isBettingRoundComplete(players, 10)).toBe(true);
    });

    it('BB option: round NOT complete when BB has not acted (even though bet matches)', () => {
      const players = [
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 2, seat: 1 },
        { status: PLAYER_STATUS.ACTIVE, action: 'call', bet: 2, seat: 2 },
        { status: PLAYER_STATUS.ACTIVE, action: '', bet: 2, seat: 3 }, // BB
      ];
      expect(isBettingRoundComplete(players, 2)).toBe(false);
    });
  });

  // ── Pots ──────────────────────────────────────────────────────────

  describe('Pots — calculatePots', () => {
    it('single pot with no side pots', () => {
      const players = [
        { seat: 1, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
        { seat: 2, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
      ];
      const pots = calculatePots(players);
      expect(pots).toHaveLength(1);
      expect(pots[0].amount).toBe(20);
      expect(pots[0].eligible).toEqual([1, 2]);
    });

    it('side pot with one all-in short', () => {
      const players = [
        { seat: 1, totalBet: 5, status: PLAYER_STATUS.ALL_IN },
        { seat: 2, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
        { seat: 3, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
      ];
      const pots = calculatePots(players);
      expect(pots).toHaveLength(2);
      // Main: 5*3 = 15
      expect(pots[0].amount).toBe(15);
      expect(pots[0].eligible).toEqual(expect.arrayContaining([1, 2, 3]));
      // Side: 5*2 = 10
      expect(pots[1].amount).toBe(10);
      expect(pots[1].eligible).toEqual(expect.arrayContaining([2, 3]));
    });

    it('folded player money goes to pot but not eligible', () => {
      const players = [
        { seat: 1, totalBet: 5, status: PLAYER_STATUS.FOLDED },
        { seat: 2, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
      ];
      const pots = calculatePots(players);
      const total = pots.reduce((s, p) => s + p.amount, 0);
      expect(total).toBe(15);
      // Folded player not eligible
      for (const pot of pots) {
        expect(pot.eligible).not.toContain(1);
      }
    });

    it('total pot equals sum of all totalBets', () => {
      const players = [
        { seat: 1, totalBet: 3, status: PLAYER_STATUS.FOLDED },
        { seat: 2, totalBet: 5, status: PLAYER_STATUS.ALL_IN },
        { seat: 3, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
        { seat: 4, totalBet: 10, status: PLAYER_STATUS.ACTIVE },
      ];
      const pots = calculatePots(players);
      const potTotal = pots.reduce((s, p) => s + p.amount, 0);
      const betTotal = players.reduce((s, p) => s + p.totalBet, 0);
      expect(potTotal).toBe(betTotal);
    });
  });

  describe('Pots — distributePots remainder handling', () => {
    it('no chips lost in 3-way split', () => {
      const pots = [{ amount: 10, eligible: [1, 2, 3] }];
      const payouts = distributePots(pots, [1, 2, 3]);
      const total = Object.values(payouts).reduce((s, v) => s + v, 0);
      expect(total).toBeCloseTo(10, 2);
    });

    it('remainder goes to first winner', () => {
      const pots = [{ amount: 10, eligible: [1, 2, 3] }];
      const payouts = distributePots(pots, [1, 2, 3]);
      // 10/3 = 3.33 each, 0.01 remainder
      expect(payouts[1]).toBe(3.34);
      expect(payouts[2]).toBe(3.33);
      expect(payouts[3]).toBe(3.33);
    });
  });

  // ── All fold except one ───────────────────────────────────────────

  describe('All fold except one — immediate win', () => {
    it('awards pot immediately when all but one fold', () => {
      const state = setupHand(3);
      const round = 'preflop';

      // Everyone folds except the first player
      let s = act(state, round, ACTION.FOLD); // UTG folds
      // Next player also folds
      s = act(s, round, ACTION.FOLD);

      // Hand should jump to FIND_WINNERS
      expect(s.game.handStep).toBe(GAME_HAND.FIND_WINNERS);

      s = findHandWinners(s.game, s.players);
      expect(s.game.winners).toHaveLength(1);

      s = payWinners(s.game, s.players);
      const winner = s.players.find(
        (p) => p.seat === s.game.winners[0].seat
      );
      expect(winner.winnings).toBeGreaterThan(0);
    });
  });

  // ── Heads-up blinds and order ─────────────────────────────────────

  describe('Heads-up blinds and order', () => {
    it('dealer is SB in heads-up', () => {
      const state = setupHand(2);
      expect(state.game.smallBlindSeat).toBe(state.game.dealerSeat);
    });

    it('preflop: dealer/SB acts first in heads-up', () => {
      const state = setupHand(2);
      // First to act preflop = left of BB = dealer (SB) in HU
      expect(state.game.move).toBe(state.game.dealerSeat);
    });
  });

  // ── findHandWinners ───────────────────────────────────────────────

  describe('findHandWinners', () => {
    it('finds winner when one player remains', () => {
      const game = createTestGame({
        handStep: GAME_HAND.FIND_WINNERS,
        communityCards: ['AH', 'KD', 'QS', 'JC', '10H'],
      });
      const players = createTestPlayers();
      players[0].status = PLAYER_STATUS.FOLDED;
      players[1].status = PLAYER_STATUS.FOLDED;
      players[2].cards = ['9D', '8D'];

      const result = findHandWinners(game, players);
      expect(result.game.winners).toHaveLength(1);
      expect(result.game.winners[0].seat).toBe(3);
    });

    it('evaluates hands and finds the best', () => {
      const game = createTestGame({
        handStep: GAME_HAND.FIND_WINNERS,
        communityCards: ['2H', '7D', 'QS', 'JC', '3H'],
      });
      const players = createTestPlayers(2);
      players[0].cards = ['AH', 'AD']; // Pair of Aces
      players[1].cards = ['KH', '9D']; // King high

      const result = findHandWinners(game, players);
      expect(result.game.winners).toHaveLength(1);
      expect(result.game.winners[0].seat).toBe(1);
    });
  });

  // ── payWinners ────────────────────────────────────────────────────

  describe('payWinners', () => {
    it('pays pot to single winner', () => {
      const game = createTestGame({
        handStep: GAME_HAND.PAY_WINNERS,
        pot: 50,
        winners: [{ seat: 1, playerId: 1 }],
      });
      const players = createTestPlayers();
      players[1].status = PLAYER_STATUS.FOLDED;
      players[2].status = PLAYER_STATUS.FOLDED;
      players[0].cards = ['AH', 'AD'];
      game.communityCards = ['2H', '3D', '4S', '5C', '6H'];

      const result = payWinners(game, players);
      expect(result.players[0].stack).toBe(150);
      expect(result.game.pot).toBe(0);
    });

    it('distributes side pots correctly', () => {
      const game = createTestGame({
        handStep: GAME_HAND.PAY_WINNERS,
        pot: 30,
        communityCards: ['2H', '3D', '4S', '5C', '6H'],
      });
      const players = createTestPlayers(3);
      // P1 all-in for 5, P2 and P3 for 10 each
      players[0].totalBet = 5;
      players[0].stack = 0;
      players[0].status = PLAYER_STATUS.ALL_IN;
      players[0].cards = ['AH', 'AD']; // best hand (pair of aces)

      players[1].totalBet = 10;
      players[1].stack = 90;
      players[1].status = PLAYER_STATUS.ACTIVE;
      players[1].cards = ['KH', 'KD']; // second best (pair of kings)

      players[2].totalBet = 10;
      players[2].stack = 90;
      players[2].status = PLAYER_STATUS.ACTIVE;
      players[2].cards = ['7H', '8D']; // worst (no pair)

      game.communityCards = ['2H', '9D', 'QS', 'JC', '10C'];
      game.pot = 25;

      const result = payWinners(game, players);

      // P1 wins main pot (15), P2 wins side pot (10)
      expect(result.players[0].winnings).toBe(15); // main pot
      expect(result.players[1].winnings).toBe(10); // side pot
      expect(result.players[2].winnings).toBe(0);
    });
  });

  // ── Full 4-street hand ────────────────────────────────────────────

  describe('Full hand flow — 4 streets', () => {
    it('processes a complete hand from prep to completion', () => {
      let state = setupHand(3);
      expect(state.game.handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      state.players.forEach((p) => expect(p.cards).toHaveLength(2));

      // Preflop: everyone calls
      state = runRound(
        state,
        'preflop',
        GAME_HAND.PRE_FLOP_BETTING_ROUND,
        ACTION.CALL
      );
      expect(state.game.handStep).toBe(GAME_HAND.DEAL_FLOP);

      state = dealFlop(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(3);

      // Flop: everyone checks
      state = runRound(
        state,
        'flop',
        GAME_HAND.FLOP_BETTING_ROUND,
        ACTION.CHECK
      );
      expect(state.game.handStep).toBe(GAME_HAND.DEAL_TURN);

      state = dealTurn(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(4);

      // Turn: everyone checks
      state = runRound(
        state,
        'turn',
        GAME_HAND.TURN_BETTING_ROUND,
        ACTION.CHECK
      );
      expect(state.game.handStep).toBe(GAME_HAND.DEAL_RIVER);

      state = dealRiver(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(5);

      // River: everyone checks
      state = runRound(
        state,
        'river',
        GAME_HAND.RIVER_BETTING_ROUND,
        ACTION.CHECK
      );
      expect(state.game.handStep).toBe(GAME_HAND.AFTER_RIVER_BETTING_ROUND);

      state.game.handStep = GAME_HAND.FIND_WINNERS;
      state = findHandWinners(state.game, state.players);
      expect(state.game.winners.length).toBeGreaterThanOrEqual(1);

      state = payWinners(state.game, state.players);
      expect(state.game.handStep).toBe(GAME_HAND.RECORD_STATS_AND_NEW_HAND);

      state = recordStatsAndNewHand(state.game, state.players);
      expect(state.game.status).toBe('completed');
    });
  });

  // ── Mandatory scenario ────────────────────────────────────────────

  describe('Mandatory 6-player scenario', () => {
    it('processes the full specified scenario correctly', () => {
      // Setup: 6 players, blinds 1/2
      let state = setupHand(6, 100);
      const g = state.game;

      // Identify positions
      const dealer = g.dealerSeat;
      const sb = g.smallBlindSeat;
      const bb = g.bigBlindSeat;
      expect(g.handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);
      expect(g.currentBet).toBe(2);

      // UTG = first to act (left of BB)
      const utg = g.move;

      // Helper to find seat indices
      const findPlayer = (seat) => state.players.find((p) => p.seat === seat);

      // ── PREFLOP ────────────────────────────────────────────
      // UTG calls 2
      state = act(state, 'preflop', ACTION.CALL);
      expect(findPlayer(utg).totalBet).toBe(2);

      // HJ folds
      state = act(state, 'preflop', ACTION.FOLD);

      // CO raises to 6
      const coSeat = state.game.move;
      state = act(state, 'preflop', ACTION.RAISE, 6);
      expect(state.game.currentBet).toBe(6);
      expect(state.game.lastRaiseSize).toBe(4); // 6 - 2 = 4

      // BTN calls
      state = act(state, 'preflop', ACTION.CALL);

      // SB calls (had 1 posted, needs 5 more)
      state = act(state, 'preflop', ACTION.CALL);
      expect(findPlayer(sb).totalBet).toBe(6);

      // BB calls (had 2 posted, needs 4 more)
      state = act(state, 'preflop', ACTION.CALL);
      expect(findPlayer(bb).totalBet).toBe(6);

      // UTG calls the raise (had 2, needs 4 more)
      // After this call the round completes and collectBets resets bet to 0
      state = act(state, 'preflop', ACTION.CALL);
      expect(findPlayer(utg).totalBet).toBe(6);

      // Preflop should be done — 5 players called CO's raise, HJ folded
      expect(state.game.handStep).toBe(GAME_HAND.DEAL_FLOP);
      // Pot: 6 * 5 (callers) + 0 (folder) = 30
      expect(state.game.pot).toBe(30);

      // ── FLOP ───────────────────────────────────────────────
      state = dealFlop(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(3);
      expect(state.game.currentBet).toBe(0);

      // First to act postflop: left of dealer
      // SB checks
      state = act(state, 'flop', ACTION.CHECK);

      // BB bets 4
      state = act(state, 'flop', ACTION.BET, 4);
      expect(state.game.currentBet).toBe(4);

      // UTG folds
      state = act(state, 'flop', ACTION.FOLD);

      // CO raises to 12
      state = act(state, 'flop', ACTION.RAISE, 12);
      expect(state.game.currentBet).toBe(12);
      expect(state.game.lastRaiseSize).toBe(8); // 12 - 4

      // BTN folds
      state = act(state, 'flop', ACTION.FOLD);

      // SB folds
      state = act(state, 'flop', ACTION.FOLD);

      // BB calls (needs 8 more to match 12)
      state = act(state, 'flop', ACTION.CALL);

      expect(state.game.handStep).toBe(GAME_HAND.DEAL_TURN);
      // Flop bets collected: BB bet 4 then called to 12 = 12, CO raised to 12.
      // Total flop = 12 + 12 = 24
      expect(state.game.pot).toBe(30 + 24);

      // ── TURN ───────────────────────────────────────────────
      state = dealTurn(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(4);

      // BB and CO remaining. First to act: left of dealer.
      // BB checks
      state = act(state, 'turn', ACTION.CHECK);
      // CO bets 10
      state = act(state, 'turn', ACTION.BET, 10);
      // BB calls
      state = act(state, 'turn', ACTION.CALL);

      expect(state.game.handStep).toBe(GAME_HAND.DEAL_RIVER);
      expect(state.game.pot).toBe(54 + 20);

      // ── RIVER ──────────────────────────────────────────────
      state = dealRiver(state.game, state.players);
      expect(state.game.communityCards).toHaveLength(5);

      // Both check
      state = act(state, 'river', ACTION.CHECK);
      state = act(state, 'river', ACTION.CHECK);

      expect(state.game.handStep).toBe(GAME_HAND.AFTER_RIVER_BETTING_ROUND);

      // ── SHOWDOWN ───────────────────────────────────────────
      state.game.handStep = GAME_HAND.FIND_WINNERS;
      state = findHandWinners(state.game, state.players);
      expect(state.game.winners.length).toBeGreaterThanOrEqual(1);

      state = payWinners(state.game, state.players);
      expect(state.game.pot).toBe(0);

      // Total winnings should equal 74 (the pot)
      const totalWinnings = state.players.reduce(
        (s, p) => s + (p.winnings || 0),
        0
      );
      expect(totalWinnings).toBe(74);

      // Total stacks should equal starting stacks (600)
      const totalStacks = state.players.reduce((s, p) => s + p.stack, 0);
      expect(totalStacks).toBe(600);

      state = recordStatsAndNewHand(state.game, state.players);
      expect(state.game.status).toBe('completed');
    });
  });

  // ── Side pot multi-all-in ─────────────────────────────────────────

  describe('Side pot multi-all-in distribution', () => {
    it('correctly builds and distributes side pots', () => {
      // P1 has 30, P2 has 60, P3 has 100. All go all-in preflop.
      const game = createTestGame({ maxSeats: 6 });
      const players = [
        { ...createTestPlayers(1, 30)[0], seat: 1 },
        { ...createTestPlayers(1, 60)[0], seat: 2, id: 2, playerId: 2 },
        { ...createTestPlayers(1, 100)[0], seat: 3, id: 3, playerId: 3 },
      ];

      let state = gamePrep(game, players);
      state = setupDealer(state.game, state.players);
      state = setupSmallBlind(state.game, state.players);
      state = setupBigBlind(state.game, state.players);
      state = dealCards(state.game, state.players);

      // All players go all-in
      let round = 'preflop';
      let guard = 0;
      while (
        state.game.handStep === GAME_HAND.PRE_FLOP_BETTING_ROUND &&
        guard < 10
      ) {
        state = bettingRound(state.game, state.players, round, {
          action: ACTION.ALLIN,
          seat: state.game.move,
        });
        guard++;
      }

      // Should advance past preflop (all are all-in)
      // Build pots from totalBets
      const pots = calculatePots(state.players);
      const potTotal = pots.reduce((s, p) => s + p.amount, 0);
      const betTotal = state.players.reduce((s, p) => s + p.totalBet, 0);
      expect(potTotal).toBe(betTotal);

      // Should have multiple pot tiers
      expect(pots.length).toBeGreaterThanOrEqual(2);
    });
  });
});
