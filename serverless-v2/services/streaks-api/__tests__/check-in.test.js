'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(),
  putPlayer: jest.fn(),
  updatePlayer: jest.fn(),
  addActivity: jest.fn(),
}));

jest.mock('../src/services/rewards.service', () => ({
  checkAndAwardMilestone: jest.fn(),
}));

const { getPlayer, putPlayer, updatePlayer, addActivity } = require('../src/services/dynamo.service');
const { checkAndAwardMilestone } = require('../src/services/rewards.service');
const checkInRoute = require('../src/routes/check-in');

// Extract the POST handler from the router
function getHandler() {
  return checkInRoute.stack[0].route.stack[0].handle;
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

// Set a fixed date for deterministic testing
function setDate(isoString) {
  checkInRoute.setDateProvider(() => new Date(isoString));
}

beforeEach(() => {
  jest.clearAllMocks();
  checkInRoute.resetDateProvider();
  getPlayer.mockResolvedValue(null);
  putPlayer.mockResolvedValue(undefined);
  updatePlayer.mockResolvedValue(undefined);
  addActivity.mockResolvedValue(undefined);
  checkAndAwardMilestone.mockResolvedValue(null);
});

describe('POST /api/v1/streaks/check-in', () => {
  const handler = getHandler();

  describe('first-time player', () => {
    it('should create a new player with loginStreak=1 and lastLoginDate=today', async () => {
      setDate('2026-03-10T12:00:00Z');
      getPlayer.mockResolvedValue(null);

      const req = makeReq('new-player');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(200);
      expect(res.body.playerId).toBe('new-player');
      expect(res.body.loginStreak).toBe(1);
      expect(res.body.bestLoginStreak).toBe(1);
      expect(res.body.todayCheckedIn).toBe(true);

      expect(putPlayer).toHaveBeenCalledTimes(1);
      const playerArg = putPlayer.mock.calls[0][0];
      expect(playerArg.playerId).toBe('new-player');
      expect(playerArg.loginStreak).toBe(1);
      expect(playerArg.lastLoginDate).toBe('2026-03-10');
      expect(playerArg.freezesAvailable).toBe(0);

      expect(addActivity).toHaveBeenCalledWith('new-player', '2026-03-10', {
        loggedIn: true,
        played: false,
        freezeUsed: false,
        streakBroken: false,
        loginStreakAtDay: 1,
        playStreakAtDay: 0,
      });
    });

    it('should check for milestone reward on first check-in', async () => {
      setDate('2026-03-10T12:00:00Z');
      getPlayer.mockResolvedValue(null);
      const reward = { rewardId: 'r1', points: 50 };
      checkAndAwardMilestone.mockResolvedValue(reward);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(checkAndAwardMilestone).toHaveBeenCalledWith('player-1', 1, 'login');
      expect(res.body.milestone).toEqual(reward);
    });
  });

  describe('idempotency — already checked in today', () => {
    it('should return current state without changes', async () => {
      setDate('2026-03-10T15:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 5,
        bestLoginStreak: 10,
        lastLoginDate: '2026-03-10',
        freezesAvailable: 1,
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(5);
      expect(res.body.bestLoginStreak).toBe(10);
      expect(res.body.todayCheckedIn).toBe(true);
      expect(res.body.milestone).toBeNull();

      // No player writes should happen
      expect(putPlayer).not.toHaveBeenCalled();
      expect(updatePlayer).not.toHaveBeenCalled();
      // Activity is still written to ensure loggedIn=true (merge-safe)
      expect(addActivity).toHaveBeenCalledWith('player-1', '2026-03-10', { loggedIn: true });
    });
  });

  describe('consecutive day — lastLoginDate is yesterday', () => {
    it('should increment loginStreak by 1', async () => {
      setDate('2026-03-11T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 5,
        bestLoginStreak: 5,
        lastLoginDate: '2026-03-10',
        playStreak: 3,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(6);
      expect(res.body.bestLoginStreak).toBe(6);

      expect(updatePlayer).toHaveBeenCalledTimes(1);
      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.loginStreak).toBe(6);
      expect(updates.bestLoginStreak).toBe(6);
      expect(updates.lastLoginDate).toBe('2026-03-11');

      expect(addActivity).toHaveBeenCalledWith('player-1', '2026-03-11', {
        loggedIn: true,
        played: false,
        freezeUsed: false,
        streakBroken: false,
        loginStreakAtDay: 6,
        playStreakAtDay: 3,
      });
    });

    it('should not update bestLoginStreak if current best is higher', async () => {
      setDate('2026-03-11T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 3,
        bestLoginStreak: 20,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(4);
      expect(res.body.bestLoginStreak).toBe(20);
    });
  });

  describe('streak reset — 2+ days missed, no freeze', () => {
    it('should reset loginStreak to 1 and write streakBroken activity', async () => {
      setDate('2026-03-15T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 10,
        bestLoginStreak: 10,
        lastLoginDate: '2026-03-10',
        playStreak: 2,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1);
      expect(res.body.bestLoginStreak).toBe(10);

      expect(addActivity).toHaveBeenCalledWith('player-1', '2026-03-15', {
        loggedIn: true,
        played: false,
        freezeUsed: false,
        streakBroken: true,
        loginStreakAtDay: 1,
        playStreakAtDay: 2,
      });
    });

    it('should reset even with freeze if more than 2 days missed', async () => {
      setDate('2026-03-15T08:00:00Z'); // 5 days since last login
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 10,
        bestLoginStreak: 10,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 3,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(1);
      expect(res.body.bestLoginStreak).toBe(10);

      // Freeze should NOT be consumed for multi-day gaps
      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.freezesAvailable).toBe(3);
    });
  });

  describe('freeze consumption — exactly 2 days since last login', () => {
    it('should consume freeze, preserve streak, and record freezeUsed', async () => {
      setDate('2026-03-12T08:00:00Z'); // 2 days since 2026-03-10
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 7,
        bestLoginStreak: 7,
        lastLoginDate: '2026-03-10',
        playStreak: 4,
        freezesAvailable: 2,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(8);
      expect(res.body.bestLoginStreak).toBe(8);

      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.freezesAvailable).toBe(1);
      expect(updates.freezesUsedThisMonth).toBe(1);
      expect(updates.loginStreak).toBe(8);

      expect(addActivity).toHaveBeenCalledWith('player-1', '2026-03-12', {
        loggedIn: true,
        played: false,
        freezeUsed: true,
        streakBroken: false,
        loginStreakAtDay: 8,
        playStreakAtDay: 4,
      });
    });

    it('should reset streak if exactly 2 days missed but no freeze available', async () => {
      setDate('2026-03-12T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 7,
        bestLoginStreak: 7,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(1);
      expect(addActivity).toHaveBeenCalledWith('player-1', '2026-03-12', expect.objectContaining({
        freezeUsed: false,
        streakBroken: true,
        loginStreakAtDay: 1,
      }));
    });
  });

  describe('bestLoginStreak update', () => {
    it('should update bestLoginStreak when new streak exceeds it', async () => {
      setDate('2026-03-11T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 14,
        bestLoginStreak: 14,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(15);
      expect(res.body.bestLoginStreak).toBe(15);
    });
  });

  describe('365-day streak cap', () => {
    it('should cap loginStreak at 365', async () => {
      setDate('2026-03-11T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 365,
        bestLoginStreak: 365,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(365);
      expect(res.body.bestLoginStreak).toBe(365);
    });
  });

  describe('monthly freeze grant', () => {
    it('should grant free freeze on first check-in of a new month', async () => {
      setDate('2026-04-01T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 30,
        bestLoginStreak: 30,
        lastLoginDate: '2026-03-31',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 2,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.freezesAvailable).toBe(1); // 0 + 1 monthly grant
      expect(updates.freezesUsedThisMonth).toBe(0); // reset for new month
      expect(updates.lastFreezeGrantDate).toBe('2026-04');
    });

    it('should not grant freeze if already granted this month', async () => {
      setDate('2026-03-15T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 5,
        bestLoginStreak: 5,
        lastLoginDate: '2026-03-14',
        playStreak: 0,
        freezesAvailable: 1,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.freezesAvailable).toBe(1); // unchanged
    });
  });

  describe('UTC date handling', () => {
    it('should use UTC dates — midnight boundary belongs to new day', async () => {
      setDate('2026-02-20T00:00:00Z'); // exactly midnight Feb 20
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 3,
        bestLoginStreak: 3,
        lastLoginDate: '2026-02-19',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-02',
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(4);
      const updates = updatePlayer.mock.calls[0][1];
      expect(updates.lastLoginDate).toBe('2026-02-20');
    });
  });

  describe('milestone rewards', () => {
    it('should include milestone in response when awarded', async () => {
      setDate('2026-03-11T08:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 6,
        bestLoginStreak: 6,
        lastLoginDate: '2026-03-10',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
      });
      const reward = { rewardId: 'r1', type: 'login_milestone', milestone: 7, points: 150 };
      checkAndAwardMilestone.mockResolvedValue(reward);

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.body.loginStreak).toBe(7);
      expect(res.body.milestone).toEqual(reward);
      expect(checkAndAwardMilestone).toHaveBeenCalledWith('player-1', 7, 'login');
    });
  });

  describe('self-exclusion enforcement', () => {
    it('should return 403 when player is self-excluded', async () => {
      setDate('2026-03-10T12:00:00Z');
      const futureDate = '2026-04-10T00:00:00.000Z';
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 5,
        bestLoginStreak: 5,
        lastLoginDate: '2026-03-09',
        selfExcludedUntil: futureDate,
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(403);
      expect(res.body.error).toBe('Forbidden');
      expect(res.body.message).toContain('self-excluded until');
      expect(res.body.message).toContain(futureDate);

      // No writes should happen
      expect(putPlayer).not.toHaveBeenCalled();
      expect(updatePlayer).not.toHaveBeenCalled();
      expect(addActivity).not.toHaveBeenCalled();
    });

    it('should allow check-in when self-exclusion has expired', async () => {
      setDate('2026-03-10T12:00:00Z');
      getPlayer.mockResolvedValue({
        playerId: 'player-1',
        loginStreak: 5,
        bestLoginStreak: 5,
        lastLoginDate: '2026-03-09',
        playStreak: 0,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '2026-03',
        selfExcludedUntil: '2026-03-09T00:00:00.000Z', // in the past
      });

      const res = mockRes();
      await handler(makeReq(), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(6);
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

describe('utility functions', () => {
  const { getUTCDateString, getUTCMonthString, daysBetween } = checkInRoute._test;

  it('getUTCDateString returns YYYY-MM-DD', () => {
    expect(getUTCDateString(new Date('2026-03-10T23:59:59Z'))).toBe('2026-03-10');
    expect(getUTCDateString(new Date('2026-03-11T00:00:00Z'))).toBe('2026-03-11');
  });

  it('getUTCMonthString returns YYYY-MM', () => {
    expect(getUTCMonthString(new Date('2026-03-10T12:00:00Z'))).toBe('2026-03');
  });

  it('daysBetween calculates correct day differences', () => {
    expect(daysBetween('2026-03-10', '2026-03-10')).toBe(0);
    expect(daysBetween('2026-03-10', '2026-03-11')).toBe(1);
    expect(daysBetween('2026-03-10', '2026-03-12')).toBe(2);
    expect(daysBetween('2026-03-10', '2026-03-15')).toBe(5);
  });
});
