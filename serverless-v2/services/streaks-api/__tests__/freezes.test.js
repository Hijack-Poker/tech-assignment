'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(),
  getFreezeHistory: jest.fn(),
}));

const { getPlayer, getFreezeHistory } = require('../src/services/dynamo.service');
const freezesRoute = require('../src/routes/freezes');

// Extract the GET / handler from the router
function getHandler() {
  return freezesRoute.stack[0].route.stack[0].handle;
}

function mockRes() {
  const res = {
    statusCode: null,
    body: null,
    status(code) {
      this.statusCode = code;
      return this;
    },
    json(data) {
      this.body = data;
      return this;
    },
  };
  return res;
}

function makeReq(playerId = 'player-1') {
  return { playerId };
}

beforeEach(() => {
  jest.clearAllMocks();
  getPlayer.mockResolvedValue(null);
  getFreezeHistory.mockResolvedValue([]);
});

describe('GET /api/v1/player/streaks/freezes', () => {
  const handler = getHandler();

  describe('player with freeze data', () => {
    it('should return freezesAvailable, freezesUsedThisMonth, and history', async () => {
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        freezesAvailable: 2,
        freezesUsedThisMonth: 1,
      });
      getFreezeHistory.mockResolvedValue([
        { playerId: 'player-1', date: '2026-02-15', source: 'free_monthly', createdAt: '2026-02-15T10:00:00.000Z' },
        { playerId: 'player-1', date: '2026-01-20', source: 'purchased', createdAt: '2026-01-20T10:00:00.000Z' },
      ]);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.freezesAvailable).toBe(2);
      expect(res.body.freezesUsedThisMonth).toBe(1);
      expect(res.body.history).toHaveLength(2);
      expect(res.body.history[0]).toEqual({ date: '2026-02-15', source: 'free_monthly' });
      expect(res.body.history[1]).toEqual({ date: '2026-01-20', source: 'purchased' });
    });
  });

  describe('player with no freeze history', () => {
    it('should return zero balances and empty history', async () => {
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        freezesAvailable: 1,
        freezesUsedThisMonth: 0,
      });
      getFreezeHistory.mockResolvedValue([]);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.freezesAvailable).toBe(1);
      expect(res.body.freezesUsedThisMonth).toBe(0);
      expect(res.body.history).toEqual([]);
    });
  });

  describe('player with no record (never checked in)', () => {
    it('should return zero balances and empty history', async () => {
      getPlayer.mockResolvedValue(null);
      getFreezeHistory.mockResolvedValue([]);

      const res = mockRes();
      await handler(makeReq('new-player'), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.freezesAvailable).toBe(0);
      expect(res.body.freezesUsedThisMonth).toBe(0);
      expect(res.body.history).toEqual([]);
    });
  });

  describe('error handling', () => {
    it('should return 500 on unexpected error', async () => {
      getPlayer.mockRejectedValue(new Error('DynamoDB connection failed'));

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(500);
      expect(res.body.error).toBe('Internal server error');
    });
  });
});
