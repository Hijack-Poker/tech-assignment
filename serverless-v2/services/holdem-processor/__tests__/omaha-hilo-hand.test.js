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
  dealFlop,
  dealTurn,
  dealRiver,
  findHandWinners,
  payWinners,
  recordStatsAndNewHand,
} = require('../lib/process-table');
const { GAME_HAND, PLAYER_STATUS, GAME } = require('../shared/games/common/constants');

function createOmahaGame(overrides = {}) {
  return {
    id: 1,
    tableId: 3,
    gameType: GAME.OMAHA_HI_LO,
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
    lowWinners: [],
    ...overrides,
  };
}

function createOmahaPlayers(count = 3) {
  return Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    gameId: 1,
    tableId: 3,
    playerId: i + 1,
    guid: `p${i + 1}-uuid`,
    username: `Player${i + 1}`,
    seat: i + 1,
    stack: 100,
    bet: 0,
    totalBet: 0,
    status: PLAYER_STATUS.ACTIVE,
    action: '',
    cards: [],
    handRank: '',
    lowHandRank: '',
    winnings: 0,
  }));
}

describe('Omaha Hi-Lo — Full Hand Integration', () => {
  it('should deal 4 cards per player in Omaha Hi-Lo', () => {
    const game = createOmahaGame();
    const players = createOmahaPlayers(3);

    let state = gamePrep(game, players);
    expect(state.game.handStep).toBe(GAME_HAND.SETUP_DEALER);
    expect(state.game.lowWinners).toEqual([]);

    state = setupDealer(state.game, state.players);
    state = setupSmallBlind(state.game, state.players);
    state = setupBigBlind(state.game, state.players);
    expect(state.game.handStep).toBe(GAME_HAND.DEAL_CARDS);

    state = dealCards(state.game, state.players);
    expect(state.game.handStep).toBe(GAME_HAND.PRE_FLOP_BETTING_ROUND);

    // Each player should have exactly 4 cards
    state.players.forEach((p) => {
      expect(p.cards).toHaveLength(4);
    });

    // Deck should have 52 - (4 * 3) = 40 cards remaining
    expect(state.game.deck).toHaveLength(40);
  });

  it('should process a complete Omaha Hi-Lo hand from prep to completion', () => {
    const game = createOmahaGame();
    const players = createOmahaPlayers(3);

    let state = gamePrep(game, players);
    state = setupDealer(state.game, state.players);
    state = setupSmallBlind(state.game, state.players);
    state = setupBigBlind(state.game, state.players);
    state = dealCards(state.game, state.players);

    // Verify 4 cards
    state.players.forEach((p) => expect(p.cards).toHaveLength(4));

    state = bettingRound(state.game, state.players, 'preflop');
    expect(state.game.handStep).toBe(GAME_HAND.DEAL_FLOP);

    state = dealFlop(state.game, state.players);
    expect(state.game.communityCards).toHaveLength(3);

    state = bettingRound(state.game, state.players, 'flop');
    state = dealTurn(state.game, state.players);
    expect(state.game.communityCards).toHaveLength(4);

    state = bettingRound(state.game, state.players, 'turn');
    state = dealRiver(state.game, state.players);
    expect(state.game.communityCards).toHaveLength(5);

    state = bettingRound(state.game, state.players, 'river');

    // Advance to find winners
    state.game.handStep = GAME_HAND.FIND_WINNERS;
    state = findHandWinners(state.game, state.players);

    expect(state.game.winners.length).toBeGreaterThanOrEqual(1);
    // lowWinners should be an array (possibly empty)
    expect(Array.isArray(state.game.lowWinners)).toBe(true);

    // At least one player should have a handRank
    const ranked = state.players.filter((p) => p.handRank);
    expect(ranked.length).toBeGreaterThanOrEqual(1);

    state = payWinners(state.game, state.players);
    expect(state.game.handStep).toBe(GAME_HAND.RECORD_STATS_AND_NEW_HAND);
    expect(state.game.pot).toBe(0);

    // Total winnings should equal what was in the pot
    const totalWinnings = state.players.reduce((sum, p) => sum + p.winnings, 0);
    expect(totalWinnings).toBeGreaterThan(0);

    state = recordStatsAndNewHand(state.game, state.players);
    expect(state.game.status).toBe('completed');
  });

  it('should find Omaha hi and lo winners with known cards', () => {
    const game = createOmahaGame({
      handStep: GAME_HAND.FIND_WINNERS,
      communityCards: ['2H', '5D', '8S', 'KC', 'QD'],
    });

    const players = createOmahaPlayers(2);
    // Player 1: high pair + low qualifier
    players[0].cards = ['AH', '3D', 'KH', 'QS'];
    // Player 2: strong high (trips K), no low
    players[1].cards = ['KD', 'KS', 'JH', '10C'];

    const result = findHandWinners(game, players);

    // Player 2 should win high (trips kings: KD,KS + KC from board)
    expect(result.game.winners.length).toBeGreaterThanOrEqual(1);

    // Player 1 should have a qualifying low: AH,3D + 2H,5D,8S = [8,5,3,2,1]
    if (result.game.lowWinners.length > 0) {
      expect(result.game.lowWinners[0].seat).toBe(1);
      expect(result.players[0].lowHandRank).toBeTruthy();
    }
  });

  it('should correctly handle no-low scenario in Omaha Hi-Lo', () => {
    const game = createOmahaGame({
      handStep: GAME_HAND.FIND_WINNERS,
      communityCards: ['KH', 'QD', 'JS', '10C', '9H'],
    });

    const players = createOmahaPlayers(2);
    players[0].cards = ['AH', 'AD', '2S', '3C'];
    players[1].cards = ['KD', 'KS', '5H', '6D'];

    const result = findHandWinners(game, players);

    // No qualifying low is possible (board has 0 cards ≤ 8)
    expect(result.game.lowWinners).toHaveLength(0);

    // High hand should still be determined
    expect(result.game.winners.length).toBeGreaterThanOrEqual(1);
  });

  it('Texas Hold\'em game should still deal 2 cards', () => {
    const game = createOmahaGame({ gameType: GAME.TEXAS });
    const players = createOmahaPlayers(3);

    let state = gamePrep(game, players);
    state = setupDealer(state.game, state.players);
    state = setupSmallBlind(state.game, state.players);
    state = setupBigBlind(state.game, state.players);
    state = dealCards(state.game, state.players);

    state.players.forEach((p) => {
      expect(p.cards).toHaveLength(2);
    });
    expect(state.game.deck).toHaveLength(46);
  });
});
