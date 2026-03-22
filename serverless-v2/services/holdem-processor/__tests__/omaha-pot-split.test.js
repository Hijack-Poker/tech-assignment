'use strict';

jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(),
  saveGame: jest.fn(),
  savePlayers: jest.fn(),
}));
jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn(),
}));

const { payWinners, distributeHiLoPot } = require('../lib/process-table');
const { PLAYER_STATUS, GAME } = require('../shared/games/common/constants');
const { toMoney } = require('../shared/utils');

function createPlayer(seat, stack = 100) {
  return {
    id: seat,
    playerId: seat,
    seat,
    stack,
    bet: 0,
    totalBet: 0,
    status: PLAYER_STATUS.ACTIVE,
    action: '',
    cards: [],
    handRank: '',
    lowHandRank: '',
    winnings: 0,
  };
}

describe('Omaha Hi-Lo Pot Splitting', () => {
  describe('payWinners — Hi-Lo split', () => {
    it('should split pot 50/50 between high and low winner', () => {
      const game = {
        handStep: 14,
        pot: 100,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [{ seat: 2, playerId: 2 }],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2), createPlayer(3)];

      const result = payWinners(game, players);

      expect(result.players[0].winnings).toBe(50);
      expect(result.players[0].stack).toBe(150);
      expect(result.players[1].winnings).toBe(50);
      expect(result.players[1].stack).toBe(150);
      expect(result.game.pot).toBe(0);
    });

    it('should give entire pot to high when no qualifying low', () => {
      const game = {
        handStep: 14,
        pot: 100,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2)];

      const result = payWinners(game, players);

      expect(result.players[0].winnings).toBe(100);
      expect(result.players[0].stack).toBe(200);
      expect(result.players[1].winnings).toBe(0);
    });

    it('should let same player scoop when winning both hi and lo', () => {
      const game = {
        handStep: 14,
        pot: 100,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [{ seat: 1, playerId: 1 }],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2)];

      const result = payWinners(game, players);

      expect(result.players[0].winnings).toBe(100);
      expect(result.players[0].stack).toBe(200);
    });

    it('should give odd chip to high winner', () => {
      const game = {
        handStep: 14,
        pot: 101,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [{ seat: 2, playerId: 2 }],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2)];

      const result = payWinners(game, players);

      // Hi gets ceil(101/2)=50.50 or 51, Lo gets floor(101/2)=50.50 or 50
      const hiWin = result.players[0].winnings;
      const loWin = result.players[1].winnings;
      expect(hiWin + loWin).toBeCloseTo(101, 1);
      expect(hiWin).toBeGreaterThanOrEqual(loWin);
    });

    it('should split hi half among multiple high winners', () => {
      const game = {
        handStep: 14,
        pot: 200,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }, { seat: 2, playerId: 2 }],
        lowWinners: [{ seat: 3, playerId: 3 }],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2), createPlayer(3)];

      const result = payWinners(game, players);

      // Hi half = 100, split 2 ways = 50 each
      // Lo half = 100
      expect(result.players[0].winnings).toBe(50);
      expect(result.players[1].winnings).toBe(50);
      expect(result.players[2].winnings).toBe(100);
    });

    it('should handle quartering — 2 low winners', () => {
      const game = {
        handStep: 14,
        pot: 200,
        gameType: GAME.OMAHA_HI_LO,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [{ seat: 2, playerId: 2 }, { seat: 3, playerId: 3 }],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2), createPlayer(3)];

      const result = payWinners(game, players);

      // Hi = 100 to seat 1
      // Lo = 100 split 2 ways = 50 each
      expect(result.players[0].winnings).toBe(100);
      expect(result.players[1].winnings).toBe(50);
      expect(result.players[2].winnings).toBe(50);
    });
  });

  describe('payWinners — Texas (no split)', () => {
    it('should give full pot to winner without splitting', () => {
      const game = {
        handStep: 14,
        pot: 100,
        gameType: GAME.TEXAS,
        winners: [{ seat: 1, playerId: 1 }],
        lowWinners: [],
        sidePots: [],
      };
      const players = [createPlayer(1), createPlayer(2)];

      const result = payWinners(game, players);

      expect(result.players[0].winnings).toBe(100);
      expect(result.players[0].stack).toBe(200);
    });
  });

  describe('distributeHiLoPot()', () => {
    it('should split evenly for 1 hi + 1 lo winner', () => {
      const players = [createPlayer(1), createPlayer(2)];
      distributeHiLoPot(100, [1], [2], players);
      expect(players[0].stack).toBe(150);
      expect(players[1].stack).toBe(150);
    });

    it('should handle same player in both hi and lo', () => {
      const players = [createPlayer(1), createPlayer(2)];
      distributeHiLoPot(100, [1], [1], players);
      expect(players[0].stack).toBe(200);
      expect(players[0].winnings).toBe(100);
    });
  });
});
