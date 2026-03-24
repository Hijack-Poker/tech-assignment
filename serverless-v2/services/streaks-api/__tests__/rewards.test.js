'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getRewards: jest.fn(),
}));

const { getRewards } = require('../src/services/dynamo.service');
const rewardsRoute = require('../src/routes/rewards');

// Extract the GET / handler from the router
function getHandler() {
  return rewardsRoute.stack[0].route.stack[0].handle;
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
  getRewards.mockResolvedValue([]);
});

describe('GET /api/v1/player/streaks/rewards', () => {
  const handler = getHandler();

  describe('player with earned rewards', () => {
    it('should return rewards sorted by createdAt descending', async () => {
      getRewards.mockResolvedValue([
        {
          playerId: 'player-1',
          rewardId: 'r1',
          type: 'login_milestone',
          milestone: 3,
          points: 50,
          streakCount: 3,
          createdAt: '2026-02-01T10:00:00.000Z',
        },
        {
          playerId: 'player-1',
          rewardId: 'r2',
          type: 'play_milestone',
          milestone: 7,
          points: 300,
          streakCount: 7,
          createdAt: '2026-02-10T10:00:00.000Z',
        },
        {
          playerId: 'player-1',
          rewardId: 'r3',
          type: 'login_milestone',
          milestone: 14,
          points: 400,
          streakCount: 14,
          createdAt: '2026-02-15T10:00:00.000Z',
        },
      ]);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.rewards).toHaveLength(3);
      // Sorted descending by createdAt
      expect(res.body.rewards[0]).toEqual({
        date: '2026-02-15T10:00:00.000Z',
        milestone: 14,
        type: 'login_milestone',
        points: 400,
      });
      expect(res.body.rewards[1]).toEqual({
        date: '2026-02-10T10:00:00.000Z',
        milestone: 7,
        type: 'play_milestone',
        points: 300,
      });
      expect(res.body.rewards[2]).toEqual({
        date: '2026-02-01T10:00:00.000Z',
        milestone: 3,
        type: 'login_milestone',
        points: 50,
      });
    });

    it('should include date, milestone, type, and points for each reward', async () => {
      getRewards.mockResolvedValue([
        {
          playerId: 'player-1',
          rewardId: 'r1',
          type: 'login_milestone',
          milestone: 7,
          points: 150,
          streakCount: 7,
          createdAt: '2026-03-01T12:00:00.000Z',
        },
      ]);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      const reward = res.body.rewards[0];
      expect(reward).toHaveProperty('date');
      expect(reward).toHaveProperty('milestone');
      expect(reward).toHaveProperty('type');
      expect(reward).toHaveProperty('points');
    });
  });

  describe('player with no rewards', () => {
    it('should return an empty array', async () => {
      getRewards.mockResolvedValue([]);

      const res = mockRes();
      await handler(makeReq('new-player'), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.rewards).toEqual([]);
    });
  });

  describe('error handling', () => {
    it('should return 500 on unexpected error', async () => {
      getRewards.mockRejectedValue(new Error('DynamoDB connection failed'));

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(500);
      expect(res.body.error).toBe('Internal server error');
    });
  });
});
