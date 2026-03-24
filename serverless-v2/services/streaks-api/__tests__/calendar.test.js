'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getActivity: jest.fn(),
}));

const { getActivity } = require('../src/services/dynamo.service');
const calendarRoute = require('../src/routes/calendar');

// Extract the GET handler from the router
function getHandler() {
  return calendarRoute.stack[0].route.stack[0].handle;
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

function makeReq(playerId = 'player-1', month = '2026-02') {
  return { playerId, query: { month } };
}

beforeEach(() => {
  jest.clearAllMocks();
  getActivity.mockResolvedValue([]);
});

describe('GET /api/v1/player/streaks/calendar', () => {
  const handler = getHandler();

  describe('valid month parameter', () => {
    it('should return month field and days array with correct length', async () => {
      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(200);
      expect(res.body.month).toBe('2026-02');
      expect(res.body.days).toHaveLength(28); // Feb 2026 has 28 days
    });

    it('should return 31 days for January', async () => {
      const req = makeReq('player-1', '2026-01');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(200);
      expect(res.body.days).toHaveLength(31);
    });

    it('should return 29 days for February in a leap year', async () => {
      const req = makeReq('player-1', '2024-02');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(200);
      expect(res.body.days).toHaveLength(29);
    });

    it('should query activity with correct date range', async () => {
      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      expect(getActivity).toHaveBeenCalledWith('player-1', '2026-02-01', '2026-02-28');
    });
  });

  describe('days without activity', () => {
    it('should default to activity=none with zero streaks', async () => {
      getActivity.mockResolvedValue([]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(200);
      for (const day of res.body.days) {
        expect(day.activity).toBe('none');
        expect(day.loginStreak).toBe(0);
        expect(day.playStreak).toBe(0);
      }
    });
  });

  describe('activity type derivation', () => {
    it('should return activity=played when played=true', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-05',
          loggedIn: true,
          played: true,
          freezeUsed: false,
          streakBroken: false,
          loginStreakAtDay: 10,
          playStreakAtDay: 5,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day5 = res.body.days.find((d) => d.date === '2026-02-05');
      expect(day5.activity).toBe('played');
      expect(day5.loginStreak).toBe(10);
      expect(day5.playStreak).toBe(5);
    });

    it('should return activity=login_only when loggedIn=true and played=false', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-10',
          loggedIn: true,
          played: false,
          freezeUsed: false,
          streakBroken: false,
          loginStreakAtDay: 3,
          playStreakAtDay: 0,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day10 = res.body.days.find((d) => d.date === '2026-02-10');
      expect(day10.activity).toBe('login_only');
      expect(day10.loginStreak).toBe(3);
      expect(day10.playStreak).toBe(0);
    });

    it('should return activity=freeze when freezeUsed=true (highest priority)', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-15',
          loggedIn: true,
          played: true,
          freezeUsed: true,
          streakBroken: false,
          loginStreakAtDay: 7,
          playStreakAtDay: 2,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day15 = res.body.days.find((d) => d.date === '2026-02-15');
      expect(day15.activity).toBe('freeze');
    });

    it('should return activity=streak_broken when streakBroken=true and no other flags', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-20',
          loggedIn: false,
          played: false,
          freezeUsed: false,
          streakBroken: true,
          loginStreakAtDay: 0,
          playStreakAtDay: 0,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day20 = res.body.days.find((d) => d.date === '2026-02-20');
      expect(day20.activity).toBe('streak_broken');
    });
  });

  describe('activity type priority', () => {
    it('freeze takes priority over played', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-01',
          loggedIn: true,
          played: true,
          freezeUsed: true,
          streakBroken: false,
          loginStreakAtDay: 5,
          playStreakAtDay: 3,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day1 = res.body.days.find((d) => d.date === '2026-02-01');
      expect(day1.activity).toBe('freeze');
    });

    it('played takes priority over login_only', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-01',
          loggedIn: true,
          played: true,
          freezeUsed: false,
          streakBroken: false,
          loginStreakAtDay: 5,
          playStreakAtDay: 3,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      const day1 = res.body.days.find((d) => d.date === '2026-02-01');
      expect(day1.activity).toBe('played');
    });
  });

  describe('mixed activity days in a month', () => {
    it('should correctly map multiple days with different activity types', async () => {
      getActivity.mockResolvedValue([
        {
          playerId: 'player-1',
          date: '2026-02-01',
          loggedIn: true,
          played: true,
          freezeUsed: false,
          streakBroken: false,
          loginStreakAtDay: 1,
          playStreakAtDay: 1,
        },
        {
          playerId: 'player-1',
          date: '2026-02-02',
          loggedIn: true,
          played: false,
          freezeUsed: false,
          streakBroken: false,
          loginStreakAtDay: 2,
          playStreakAtDay: 0,
        },
        {
          playerId: 'player-1',
          date: '2026-02-04',
          loggedIn: true,
          played: false,
          freezeUsed: true,
          streakBroken: false,
          loginStreakAtDay: 3,
          playStreakAtDay: 0,
        },
      ]);

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      expect(res.body.days).toHaveLength(28);

      const day1 = res.body.days.find((d) => d.date === '2026-02-01');
      expect(day1.activity).toBe('played');
      expect(day1.loginStreak).toBe(1);
      expect(day1.playStreak).toBe(1);

      const day2 = res.body.days.find((d) => d.date === '2026-02-02');
      expect(day2.activity).toBe('login_only');
      expect(day2.loginStreak).toBe(2);

      const day3 = res.body.days.find((d) => d.date === '2026-02-03');
      expect(day3.activity).toBe('none');
      expect(day3.loginStreak).toBe(0);
      expect(day3.playStreak).toBe(0);

      const day4 = res.body.days.find((d) => d.date === '2026-02-04');
      expect(day4.activity).toBe('freeze');
      expect(day4.loginStreak).toBe(3);
    });
  });

  describe('invalid month parameter', () => {
    it('should return 400 when month is missing', async () => {
      const req = { playerId: 'player-1', query: {} };
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
      expect(res.body.error).toBe('Bad Request');
      expect(res.body.message).toContain('YYYY-MM');
    });

    it('should return 400 when month has invalid format', async () => {
      const req = makeReq('player-1', '2026-2');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
    });

    it('should return 400 when month is not a valid month number', async () => {
      const req = makeReq('player-1', '2026-13');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
    });

    it('should return 400 when month is full date instead of YYYY-MM', async () => {
      const req = makeReq('player-1', '2026-02-15');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
    });

    it('should return 400 when month is empty string', async () => {
      const req = makeReq('player-1', '');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
    });

    it('should return 400 for month=00', async () => {
      const req = makeReq('player-1', '2026-00');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(400);
    });
  });

  describe('error handling', () => {
    it('should return 500 on unexpected error', async () => {
      getActivity.mockRejectedValue(new Error('DynamoDB connection failed'));

      const req = makeReq('player-1', '2026-02');
      const res = mockRes();
      await handler(req, res);

      expect(res.statusCode).toBe(500);
      expect(res.body.error).toBe('Internal server error');
    });
  });
});

describe('utility functions', () => {
  const { parseMonth, daysInMonth, deriveActivityType } = calendarRoute._test;

  describe('parseMonth', () => {
    it('returns year and month for valid input', () => {
      expect(parseMonth('2026-02')).toEqual({ year: 2026, month: 2 });
      expect(parseMonth('2024-12')).toEqual({ year: 2024, month: 12 });
    });

    it('returns null for invalid input', () => {
      expect(parseMonth(null)).toBeNull();
      expect(parseMonth(undefined)).toBeNull();
      expect(parseMonth('')).toBeNull();
      expect(parseMonth('2026-2')).toBeNull();
      expect(parseMonth('2026-13')).toBeNull();
      expect(parseMonth('2026-00')).toBeNull();
      expect(parseMonth('abc')).toBeNull();
    });
  });

  describe('daysInMonth', () => {
    it('returns correct days for various months', () => {
      expect(daysInMonth(2026, 1)).toBe(31);
      expect(daysInMonth(2026, 2)).toBe(28);
      expect(daysInMonth(2024, 2)).toBe(29); // leap year
      expect(daysInMonth(2026, 4)).toBe(30);
    });
  });

  describe('deriveActivityType', () => {
    it('returns freeze when freezeUsed is true (highest priority)', () => {
      expect(deriveActivityType({ freezeUsed: true, played: true, loggedIn: true })).toBe('freeze');
    });

    it('returns played when played is true', () => {
      expect(deriveActivityType({ freezeUsed: false, played: true, loggedIn: true })).toBe('played');
    });

    it('returns login_only when only loggedIn is true', () => {
      expect(deriveActivityType({ freezeUsed: false, played: false, loggedIn: true })).toBe('login_only');
    });

    it('returns streak_broken when streakBroken is true', () => {
      expect(deriveActivityType({ freezeUsed: false, played: false, loggedIn: false, streakBroken: true })).toBe('streak_broken');
    });

    it('returns none when no flags are set', () => {
      expect(deriveActivityType({ freezeUsed: false, played: false, loggedIn: false, streakBroken: false })).toBe('none');
    });
  });
});
