'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(),
  putPlayer: jest.fn().mockResolvedValue(undefined),
  updatePlayer: jest.fn().mockResolvedValue(undefined),
  getAllPlayers: jest.fn().mockResolvedValue([]),
  getRewards: jest.fn().mockResolvedValue([]),
}));

const { getPlayer, getRewards } = require('../src/services/dynamo.service');
const streaksRoute = require('../src/routes/streaks');

// Extract the GET / handler from the router
function getHandler() {
  return streaksRoute.stack[0].route.stack[0].handle;
}

// Extract the GET /share handler from the router
function getShareHandler() {
  return streaksRoute.stack[2].route.stack[0].handle;
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
});

describe('GET /api/v1/streaks', () => {
  const handler = getHandler();

  describe('player with existing streaks', () => {
    it('should return full streak state with next milestones', async () => {
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 12,
        playStreak: 5,
        bestLoginStreak: 45,
        bestPlayStreak: 22,
        freezesAvailable: 2,
        lastLoginDate: '2026-02-20',
        lastPlayDate: '2026-02-19',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({
        loginStreak: 12,
        playStreak: 5,
        bestLoginStreak: 45,
        bestPlayStreak: 22,
        freezesAvailable: 2,
        nextLoginMilestone: { days: 14, reward: 400, daysRemaining: 2 },
        nextPlayMilestone: { days: 7, reward: 300, daysRemaining: 2 },
        lastLoginDate: '2026-02-20',
        lastPlayDate: '2026-02-19',
        comboActive: true,
        comboMultiplier: 1.1,
      });
    });
  });

  describe('player with no record (never checked in)', () => {
    it('should return default zeros', async () => {
      getPlayer.mockResolvedValue(null);

      const res = mockRes();
      await handler(makeReq('new-player'), res);

      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({
        loginStreak: 0,
        playStreak: 0,
        bestLoginStreak: 0,
        bestPlayStreak: 0,
        freezesAvailable: 0,
        nextLoginMilestone: { days: 3, reward: 50, daysRemaining: 3 },
        nextPlayMilestone: { days: 3, reward: 100, daysRemaining: 3 },
        lastLoginDate: '',
        lastPlayDate: '',
        comboActive: false,
        comboMultiplier: 1,
      });
    });
  });

  describe('player past all milestones (streak > 90)', () => {
    it('should return null for nextLoginMilestone and nextPlayMilestone', async () => {
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 95,
        playStreak: 100,
        bestLoginStreak: 95,
        bestPlayStreak: 100,
        freezesAvailable: 3,
        lastLoginDate: '2026-02-20',
        lastPlayDate: '2026-02-20',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.nextLoginMilestone).toBeNull();
      expect(res.body.nextPlayMilestone).toBeNull();
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

describe('getNextMilestone utility', () => {
  const { getNextMilestone } = streaksRoute._test;

  it('returns next login milestone with correct reward and daysRemaining', () => {
    const result = getNextMilestone(12, 'login');
    expect(result).toEqual({ days: 14, reward: 400, daysRemaining: 2 });
  });

  it('returns next play milestone with correct reward and daysRemaining', () => {
    const result = getNextMilestone(5, 'play');
    expect(result).toEqual({ days: 7, reward: 300, daysRemaining: 2 });
  });

  it('returns first milestone for streak of 0', () => {
    const result = getNextMilestone(0, 'login');
    expect(result).toEqual({ days: 3, reward: 50, daysRemaining: 3 });
  });

  it('returns null when streak exceeds all milestones', () => {
    expect(getNextMilestone(90, 'login')).toBeNull();
    expect(getNextMilestone(91, 'login')).toBeNull();
    expect(getNextMilestone(100, 'play')).toBeNull();
  });

  it('returns milestone when streak equals a milestone boundary', () => {
    // streak=3 means next milestone is 7
    const result = getNextMilestone(3, 'login');
    expect(result).toEqual({ days: 7, reward: 150, daysRemaining: 4 });
  });

  it('returns correct milestone at streak=89 (just before last)', () => {
    const result = getNextMilestone(89, 'login');
    expect(result).toEqual({ days: 90, reward: 5000, daysRemaining: 1 });
  });
});

describe('GET /api/v1/streaks/share', () => {
  const handler = getShareHandler();

  it('should return share data for player with active streaks', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'player-1',
      displayName: 'TestPlayer',
      loginStreak: 30,
      playStreak: 15,
      bestLoginStreak: 45,
      bestPlayStreak: 22,
    });
    getRewards.mockResolvedValue([
      { rewardId: 'r1', points: 100 },
      { rewardId: 'r2', points: 200 },
      { rewardId: 'r3', points: 300 },
      { rewardId: 'r4', points: 400 },
      { rewardId: 'r5', points: 500 },
    ]);

    const res = mockRes();
    await handler(makeReq(), res);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({
      playerName: 'TestPlayer',
      loginStreak: 30,
      playStreak: 15,
      bestLoginStreak: 45,
      bestPlayStreak: 22,
      tier: 'gold',
      totalRewards: 5,
      shareText: "\uD83D\uDD25 30-day login streak and 15-day play streak on Hijack Poker! Gold tier. Can you beat me?\n\nhttps://hijackpoker.com",
    });
  });

  it('should return default share data for new player', async () => {
    getPlayer.mockResolvedValue(null);

    const res = mockRes();
    await handler(makeReq('new-player'), res);

    expect(res.statusCode).toBe(200);
    expect(res.body.playerName).toBe('New Player');
    expect(res.body.loginStreak).toBe(0);
    expect(res.body.playStreak).toBe(0);
    expect(res.body.tier).toBe('bronze');
    expect(res.body.totalRewards).toBe(0);
  });

  it('should use playerId prefix when displayName is missing', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'abcdefgh-1234',
      loginStreak: 5,
      playStreak: 3,
      bestLoginStreak: 5,
      bestPlayStreak: 3,
    });
    getRewards.mockResolvedValue([]);

    const res = mockRes();
    await handler(makeReq('abcdefgh-1234'), res);

    expect(res.statusCode).toBe(200);
    expect(res.body.playerName).toBe('abcdefgh');
  });

  it('should calculate correct tier based on combined score', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'player-1',
      displayName: 'Platinum Pro',
      loginStreak: 50,
      playStreak: 45,
      bestLoginStreak: 50,
      bestPlayStreak: 45,
    });
    getRewards.mockResolvedValue([]);

    const res = mockRes();
    await handler(makeReq(), res);

    expect(res.statusCode).toBe(200);
    expect(res.body.tier).toBe('platinum');
  });

  it('should return 500 on unexpected error', async () => {
    getPlayer.mockRejectedValue(new Error('DynamoDB connection failed'));

    const res = mockRes();
    await handler(makeReq(), res);

    expect(res.statusCode).toBe(500);
    expect(res.body.error).toBe('Internal server error');
  });
});
