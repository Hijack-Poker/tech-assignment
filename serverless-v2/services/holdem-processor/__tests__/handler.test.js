'use strict';

// Mock database and event modules
jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(),
  saveGame: jest.fn(),
  savePlayers: jest.fn(),
}));
jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn(),
}));

const { health, getTableHttp } = require('../handler');
const { fetchTable } = require('../lib/table-fetcher');

describe('Handler', () => {
  describe('health', () => {
    it('should return 200 with service info', async () => {
      const result = await health();

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.service).toBe('holdem-processor');
      expect(body.status).toBe('ok');
      expect(body.timestamp).toBeDefined();
    });
  });

  describe('getTableHttp', () => {
    afterEach(() => jest.resetAllMocks());

    it('should return 200 with game state when table exists', async () => {
      fetchTable.mockResolvedValue({
        game: {
          id: 1, tableId: 1, tableName: 'Starter Table', gameNo: 1,
          handStep: 0, dealerSeat: 1, smallBlindSeat: 0, bigBlindSeat: 0,
          communityCards: [], pot: 0, sidePots: [], move: 0, status: 'in_progress',
          smallBlind: 1, bigBlind: 2, maxSeats: 6, deck: ['AH', 'KD'],
          currentBet: 0, winners: [],
        },
        players: [{
          playerId: 1, username: 'Alice', seat: 1, stack: 120,
          bet: 0, totalBet: 0, status: '1', action: '', cards: [],
          handRank: '', winnings: 0,
        }],
      });

      const result = await getTableHttp({ pathParameters: { tableId: '1' } });

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.game.tableId).toBe(1);
      expect(body.game.deck).toBeUndefined();
      expect(body.players).toHaveLength(1);
      expect(body.players[0].username).toBe('Alice');
    });

    it('should return 404 when table is not found', async () => {
      fetchTable.mockResolvedValue(null);

      const result = await getTableHttp({ pathParameters: { tableId: '999' } });

      expect(result.statusCode).toBe(404);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Table not found');
    });

    it('should return 500 with generic message on DB error', async () => {
      fetchTable.mockRejectedValue(new Error('ECONNREFUSED'));

      const result = await getTableHttp({ pathParameters: { tableId: '1' } });

      expect(result.statusCode).toBe(500);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Internal server error');
    });
  });
});
