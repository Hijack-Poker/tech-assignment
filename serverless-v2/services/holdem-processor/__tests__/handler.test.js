'use strict';

// Mock database and event modules
jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(),
  saveGame: jest.fn(),
  savePlayers: jest.fn(),
  resetTable: jest.fn(),
  freshResetTable: jest.fn(),
  tipDealer: jest.fn(),
}));
jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn(),
}));
jest.mock('../lib/process-table', () => ({
  processTable: jest.fn(),
}));

const {
  health,
  processHandHttp,
  getTableHttp,
  resetTableHttp,
  freshResetTableHttp,
  tipDealerHttp,
} = require('../handler');

const { fetchTable, resetTable, freshResetTable, tipDealer } = require('../lib/table-fetcher');
const { processTable } = require('../lib/process-table');

describe('Handler', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

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

  describe('processHandHttp', () => {
    it('should return 400 when tableId is missing from body', async () => {
      const event = { body: JSON.stringify({}) };

      const result = await processHandHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId is required');
    });

    it('should return 200 with success result when tableId is provided', async () => {
      const mockResult = { handStep: 1, gameNo: 5 };
      processTable.mockResolvedValue(mockResult);

      const event = { body: JSON.stringify({ tableId: 'table-123' }) };

      const result = await processHandHttp(event);

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.success).toBe(true);
      expect(body.result).toEqual(mockResult);
      expect(processTable).toHaveBeenCalledWith('table-123', null);
    });

    it('should pass action request to processTable when action is provided', async () => {
      processTable.mockResolvedValue({});

      const event = {
        body: JSON.stringify({
          tableId: 'table-123',
          action: 'call',
          amount: 100,
          seat: 3,
        }),
      };

      const result = await processHandHttp(event);

      expect(result.statusCode).toBe(200);
      expect(processTable).toHaveBeenCalledWith('table-123', {
        action: 'call',
        amount: 100,
        seat: 3,
      });
    });

    it('should return 500 when processTable throws', async () => {
      processTable.mockRejectedValue(new Error('Processing failed'));

      const event = { body: JSON.stringify({ tableId: 'table-123' }) };

      const result = await processHandHttp(event);

      expect(result.statusCode).toBe(500);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Processing failed');
    });
  });

  describe('getTableHttp', () => {
    it('should return 400 when tableId is missing', async () => {
      const event = { pathParameters: {}, queryStringParameters: {} };

      const result = await getTableHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId is required');
    });

    it('should return 404 when table not found', async () => {
      fetchTable.mockResolvedValue(null);

      const event = {
        pathParameters: { tableId: 'nonexistent' },
        queryStringParameters: {},
      };

      const result = await getTableHttp(event);

      expect(result.statusCode).toBe(404);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Table not found');
      expect(fetchTable).toHaveBeenCalledWith('nonexistent');
    });

    it('should return 200 with game and players when table exists', async () => {
      fetchTable.mockResolvedValue({
        game: {
          gameNo: 1,
          handStep: 0,
          deck: ['Ah', 'Kh', 'Qh'],
          pot: 100,
        },
        players: [
          {
            playerId: 'p1',
            username: 'Alice',
            seat: 1,
            stack: 500,
            bet: 10,
            totalBet: 10,
            status: 'active',
            action: 'call',
            cards: ['Ah', 'Kh'],
            handRank: 'Pair',
            bestHand: ['Ah', 'Kh', 'Qh', 'Jh', 'Th'],
            isWinner: true,
            winnings: 200,
          },
        ],
      });

      const event = {
        pathParameters: { tableId: 'table-123' },
        queryStringParameters: {},
      };

      const result = await getTableHttp(event);

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);

      // Deck should not be leaked to the UI
      expect(body.game.deck).toBeUndefined();
      expect(body.game.gameNo).toBe(1);
      expect(body.game.pot).toBe(100);
      expect(body.game.stepName).toBe('GAME_PREP');

      expect(body.players).toHaveLength(1);
      expect(body.players[0].playerId).toBe('p1');
      expect(body.players[0].username).toBe('Alice');
      expect(body.players[0].seat).toBe(1);
      expect(body.players[0].stack).toBe(500);
      expect(body.players[0].bet).toBe(10);
      expect(body.players[0].totalBet).toBe(10);
      expect(body.players[0].status).toBe('active');
      expect(body.players[0].action).toBe('call');
      expect(body.players[0].cards).toEqual(['Ah', 'Kh']);
      expect(body.players[0].handRank).toBe('Pair');
      expect(body.players[0].bestHand).toEqual(['Ah', 'Kh', 'Qh', 'Jh', 'Th']);
      expect(body.players[0].isWinner).toBe(true);
      expect(body.players[0].winnings).toBe(200);
    });

    it('should read tableId from queryStringParameters if pathParameters is missing', async () => {
      fetchTable.mockResolvedValue({
        game: { gameNo: 1, handStep: '', pot: 0 },
        players: [],
      });

      const event = {
        pathParameters: null,
        queryStringParameters: { tableId: 'table-qs' },
      };

      const result = await getTableHttp(event);

      expect(result.statusCode).toBe(200);
      expect(fetchTable).toHaveBeenCalledWith('table-qs');
    });
  });

  describe('resetTableHttp', () => {
    it('should return 400 when tableId is missing', async () => {
      const event = { pathParameters: {}, body: JSON.stringify({}) };

      const result = await resetTableHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId is required');
    });

    it('should return 404 when table not found', async () => {
      resetTable.mockResolvedValue(null);

      const event = {
        pathParameters: { tableId: 'nonexistent' },
        body: null,
      };

      const result = await resetTableHttp(event);

      expect(result.statusCode).toBe(404);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Table not found');
    });

    it('should return 200 with success and gameNo', async () => {
      resetTable.mockResolvedValue({ game: { gameNo: 42 } });

      const event = {
        pathParameters: { tableId: 'table-123' },
        body: null,
      };

      const result = await resetTableHttp(event);

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.success).toBe(true);
      expect(body.gameNo).toBe(42);
      expect(resetTable).toHaveBeenCalledWith('table-123');
    });
  });

  describe('freshResetTableHttp', () => {
    it('should return 400 when tableId is missing', async () => {
      const event = { pathParameters: {}, body: JSON.stringify({}) };

      const result = await freshResetTableHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId is required');
    });

    it('should return 404 when table not found', async () => {
      freshResetTable.mockResolvedValue(null);

      const event = {
        pathParameters: { tableId: 'nonexistent' },
        body: null,
      };

      const result = await freshResetTableHttp(event);

      expect(result.statusCode).toBe(404);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Table not found');
    });

    it('should return 200 with success and gameNo', async () => {
      freshResetTable.mockResolvedValue({ game: { gameNo: 1 } });

      const event = {
        pathParameters: { tableId: 'table-123' },
        body: null,
      };

      const result = await freshResetTableHttp(event);

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.success).toBe(true);
      expect(body.gameNo).toBe(1);
      expect(freshResetTable).toHaveBeenCalledWith('table-123');
    });
  });

  describe('tipDealerHttp', () => {
    it('should return 400 when seat is missing', async () => {
      const event = {
        pathParameters: { tableId: 'table-123' },
        body: JSON.stringify({}),
      };

      const result = await tipDealerHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId and seat are required');
    });

    it('should return 400 when tableId is missing', async () => {
      const event = {
        pathParameters: {},
        body: JSON.stringify({ seat: 3 }),
      };

      const result = await tipDealerHttp(event);

      expect(result.statusCode).toBe(400);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('tableId and seat are required');
    });

    it('should return 200 with success', async () => {
      tipDealer.mockResolvedValue({});

      const event = {
        pathParameters: { tableId: 'table-123' },
        body: JSON.stringify({ seat: 3 }),
      };

      const result = await tipDealerHttp(event);

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.success).toBe(true);
      expect(tipDealer).toHaveBeenCalledWith('table-123', 3);
    });

    it('should return 500 when tipDealer throws', async () => {
      tipDealer.mockRejectedValue(new Error('Tip failed'));

      const event = {
        pathParameters: { tableId: 'table-123' },
        body: JSON.stringify({ seat: 3 }),
      };

      const result = await tipDealerHttp(event);

      expect(result.statusCode).toBe(500);
      const body = JSON.parse(result.body);
      expect(body.error).toBe('Tip failed');
    });
  });
});
