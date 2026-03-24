'use strict';

const { calculateStreakUpdate, toUTCDateString, subtractDays } = require('../src/services/streak.service');

// ----------------------------------------------------------------
// Unit tests for streak.service
// ----------------------------------------------------------------

describe('streak.service — calculateStreakUpdate', () => {
  const today = '2026-03-22';

  describe('first-time player', () => {
    it('should start streak at 1 when lastDate is null', () => {
      const result = calculateStreakUpdate({
        lastDate: null,
        today,
        currentStreak: 0,
        freezesAvailable: 0,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'increment',
        newStreak: 1,
        consumeFreeze: false,
        streakBroken: false,
      });
    });
  });

  describe('idempotent — already played today', () => {
    it('should return action none when lastDate === today', () => {
      const result = calculateStreakUpdate({
        lastDate: today,
        today,
        currentStreak: 5,
        freezesAvailable: 1,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'none',
        newStreak: 5,
        consumeFreeze: false,
        streakBroken: false,
      });
    });
  });

  describe('consecutive day — lastDate is yesterday', () => {
    it('should increment streak by 1', () => {
      const result = calculateStreakUpdate({
        lastDate: '2026-03-21',
        today,
        currentStreak: 5,
        freezesAvailable: 0,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'increment',
        newStreak: 6,
        consumeFreeze: false,
        streakBroken: false,
      });
    });
  });

  describe('2-day gap with freeze available', () => {
    it('should consume freeze and preserve streak', () => {
      const result = calculateStreakUpdate({
        lastDate: '2026-03-20',
        today,
        currentStreak: 5,
        freezesAvailable: 2,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'freeze',
        newStreak: 5,
        consumeFreeze: true,
        streakBroken: false,
      });
    });

    it('should not double-consume if freeze already used today', () => {
      const result = calculateStreakUpdate({
        lastDate: '2026-03-20',
        today,
        currentStreak: 5,
        freezesAvailable: 2,
        freezeAlreadyUsedToday: true,
      });
      expect(result).toEqual({
        action: 'reset',
        newStreak: 1,
        consumeFreeze: false,
        streakBroken: true,
      });
    });
  });

  describe('gap too large — reset', () => {
    it('should reset to 1 when gap is 2+ days with no freeze', () => {
      const result = calculateStreakUpdate({
        lastDate: '2026-03-19',
        today,
        currentStreak: 10,
        freezesAvailable: 0,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'reset',
        newStreak: 1,
        consumeFreeze: false,
        streakBroken: true,
      });
    });

    it('should reset when gap is 3+ days even with freeze', () => {
      const result = calculateStreakUpdate({
        lastDate: '2026-03-18',
        today,
        currentStreak: 10,
        freezesAvailable: 3,
        freezeAlreadyUsedToday: false,
      });
      expect(result).toEqual({
        action: 'reset',
        newStreak: 1,
        consumeFreeze: false,
        streakBroken: true,
      });
    });
  });
});

describe('streak.service — toUTCDateString', () => {
  it('should format a Date as YYYY-MM-DD in UTC', () => {
    expect(toUTCDateString(new Date('2026-03-22T15:30:00Z'))).toBe('2026-03-22');
  });

  it('should handle midnight boundary correctly', () => {
    expect(toUTCDateString(new Date('2026-02-20T00:00:00Z'))).toBe('2026-02-20');
  });
});

describe('streak.service — subtractDays', () => {
  it('should subtract days correctly', () => {
    expect(subtractDays('2026-03-22', 1)).toBe('2026-03-21');
    expect(subtractDays('2026-03-22', 2)).toBe('2026-03-20');
  });

  it('should handle month boundaries', () => {
    expect(subtractDays('2026-03-01', 1)).toBe('2026-02-28');
  });
});

// ----------------------------------------------------------------
// Integration tests for the route handler
// ----------------------------------------------------------------

jest.mock('../shared/config/dynamo', () => {
  const sendMock = jest.fn();
  return {
    docClient: { send: sendMock },
    __sendMock: sendMock,
  };
});

jest.mock('../src/services/rewards.service', () => ({
  checkAndAwardMilestone: jest.fn().mockResolvedValue(null),
}));

const { __sendMock: sendMock } = require('../shared/config/dynamo');
const { checkAndAwardMilestone } = require('../src/services/rewards.service');

describe('POST /internal/streaks/hand-completed — route', () => {
  let handler;

  beforeAll(() => {
    // Import the router and extract the POST /hand-completed handler
    const router = require('../src/routes/internal');
    const layer = router.stack.find(
      (l) => l.route && l.route.path === '/hand-completed' && l.route.methods.post
    );
    handler = layer.route.stack[0].handle;
  });

  beforeEach(() => {
    sendMock.mockReset();
    checkAndAwardMilestone.mockReset();
    checkAndAwardMilestone.mockResolvedValue(null);
  });

  function createMockRes() {
    const res = {
      statusCode: 200,
      _body: null,
      status(code) {
        this.statusCode = code;
        return this;
      },
      json(data) {
        this._body = data;
        return this;
      },
    };
    return res;
  }

  it('should return 400 when required fields are missing', async () => {
    const req = { body: { playerId: 'p1' } };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(400);
    expect(res._body.error).toBe('Bad Request');
  });

  it('should return 400 for invalid completedAt', async () => {
    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: 'not-a-date' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(400);
    expect(res._body.message).toContain('valid ISO 8601');
  });

  it('should create a new player and set playStreak to 1 on first hand', async () => {
    // getPlayer returns null
    sendMock.mockResolvedValueOnce({});
    // getActivity returns empty
    sendMock.mockResolvedValueOnce({ Items: [] });
    // putPlayer
    sendMock.mockResolvedValueOnce({});
    // addActivity
    sendMock.mockResolvedValueOnce({});

    const req = {
      body: { playerId: 'p-new', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.playStreak).toBe(1);
    expect(res._body.alreadyPlayedToday).toBe(false);
  });

  it('should increment playStreak when lastPlayDate is yesterday', async () => {
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        loginStreak: 3,
        playStreak: 5,
        bestLoginStreak: 3,
        bestPlayStreak: 5,
        lastLoginDate: '2026-03-21',
        lastPlayDate: '2026-03-21',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: '2026-03-21T10:00:00Z',
      },
    });
    // getActivity
    sendMock.mockResolvedValueOnce({ Items: [] });
    // updatePlayer
    sendMock.mockResolvedValueOnce({});
    // addActivity
    sendMock.mockResolvedValueOnce({});

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.playStreak).toBe(6);
    expect(res._body.bestPlayStreak).toBe(6);
    expect(res._body.streakBroken).toBe(false);
  });

  it('should be idempotent when already played today', async () => {
    // getPlayer
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        playStreak: 5,
        bestPlayStreak: 5,
        lastPlayDate: '2026-03-22',
        freezesAvailable: 0,
      },
    });
    // getActivity (freeze check still runs before streak calc)
    sendMock.mockResolvedValueOnce({ Items: [] });

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h2', completedAt: '2026-03-22T15:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.alreadyPlayedToday).toBe(true);
    expect(res._body.playStreak).toBe(5);
    // getPlayer + getActivity, no updates
    expect(sendMock).toHaveBeenCalledTimes(2);
  });

  it('should reset streak when gap is too large and no freeze', async () => {
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        loginStreak: 2,
        playStreak: 10,
        bestLoginStreak: 2,
        bestPlayStreak: 10,
        lastLoginDate: '2026-03-19',
        lastPlayDate: '2026-03-19',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: '2026-03-19T10:00:00Z',
      },
    });
    // getActivity
    sendMock.mockResolvedValueOnce({ Items: [] });
    // updatePlayer
    sendMock.mockResolvedValueOnce({});
    // addActivity
    sendMock.mockResolvedValueOnce({});

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.playStreak).toBe(1);
    expect(res._body.streakBroken).toBe(true);
  });

  it('should consume freeze when lastPlayDate is 2 days ago', async () => {
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        loginStreak: 2,
        playStreak: 7,
        bestLoginStreak: 2,
        bestPlayStreak: 7,
        lastLoginDate: '2026-03-20',
        lastPlayDate: '2026-03-20',
        freezesAvailable: 2,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: '2026-03-20T10:00:00Z',
      },
    });
    // getActivity — no freeze used today
    sendMock.mockResolvedValueOnce({ Items: [] });
    // updatePlayer
    sendMock.mockResolvedValueOnce({});
    // addActivity
    sendMock.mockResolvedValueOnce({});

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.playStreak).toBe(7);
    expect(res._body.freezeConsumed).toBe(true);
    expect(res._body.streakBroken).toBe(false);
  });

  it('should return 403 when player is self-excluded', async () => {
    const futureDate = '2026-04-10T00:00:00.000Z';
    // getPlayer returns self-excluded player
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        loginStreak: 3,
        playStreak: 5,
        bestLoginStreak: 3,
        bestPlayStreak: 5,
        lastLoginDate: '2026-03-21',
        lastPlayDate: '2026-03-21',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: '2026-03-21T10:00:00Z',
        selfExcludedUntil: futureDate,
      },
    });

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(403);
    expect(res._body.error).toBe('Forbidden');
    expect(res._body.message).toContain('self-excluded until');
    expect(res._body.message).toContain(futureDate);
    // Only getPlayer should have been called, no updates
    expect(sendMock).toHaveBeenCalledTimes(1);
  });

  it('should update bestPlayStreak when current exceeds best', async () => {
    sendMock.mockResolvedValueOnce({
      Item: {
        playerId: 'p1',
        loginStreak: 1,
        playStreak: 5,
        bestLoginStreak: 1,
        bestPlayStreak: 3,
        lastLoginDate: '2026-03-21',
        lastPlayDate: '2026-03-21',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: '2026-03-21T10:00:00Z',
      },
    });
    // getActivity
    sendMock.mockResolvedValueOnce({ Items: [] });
    // updatePlayer
    sendMock.mockResolvedValueOnce({});
    // addActivity
    sendMock.mockResolvedValueOnce({});

    const req = {
      body: { playerId: 'p1', tableId: 't1', handId: 'h1', completedAt: '2026-03-22T10:00:00Z' },
    };
    const res = createMockRes();

    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res._body.playStreak).toBe(6);
    expect(res._body.bestPlayStreak).toBe(6);
  });
});
