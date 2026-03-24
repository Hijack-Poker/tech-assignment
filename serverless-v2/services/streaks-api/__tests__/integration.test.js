'use strict';

/**
 * Integration tests: check-in -> streak update -> milestone reward flow.
 *
 * We mock dynamo.service with in-memory stores so state persists across
 * multiple calls within each test.  We also mock the shared dynamo config
 * (docClient) used by rewards.service so we can capture the PutCommand
 * payloads without touching AWS.  Everything else (check-in logic,
 * rewards.service, streak.service, constants) runs for real.
 */

// ---------------------------------------------------------------------------
// Mocks — must be declared before any require()
// ---------------------------------------------------------------------------

// In-memory stores that survive across calls within a single test
let playerStore = {};
let activityStore = [];
let freezeHistoryStore = [];
let rewardsStore = [];

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(async (id) => playerStore[id] || null),
  putPlayer: jest.fn(async (p) => {
    playerStore[p.playerId] = { ...p };
  }),
  updatePlayer: jest.fn(async (id, updates) => {
    playerStore[id] = { ...(playerStore[id] || {}), ...updates };
  }),
  addActivity: jest.fn(async (playerId, date, data) => {
    // Merge with existing record for the same day (mirrors real behavior)
    const idx = activityStore.findIndex((a) => a.playerId === playerId && a.date === date);
    const merged = {
      ...(idx >= 0 ? activityStore[idx] : {}),
      playerId,
      date,
      ...data,
      loggedIn: (idx >= 0 && activityStore[idx].loggedIn) || (data && data.loggedIn) || false,
      played: (idx >= 0 && activityStore[idx].played) || (data && data.played) || false,
    };
    if (idx >= 0) {
      activityStore[idx] = merged;
    } else {
      activityStore.push(merged);
    }
  }),
  getActivity: jest.fn(async (playerId, startDate, endDate) => {
    return activityStore.filter(
      (a) => a.playerId === playerId && a.date >= startDate && a.date <= endDate
    );
  }),
  addFreezeHistory: jest.fn(async (playerId, date, source) => {
    freezeHistoryStore.push({ playerId, date, source });
  }),
  getRewards: jest.fn(async (playerId) => {
    return rewardsStore.filter((r) => r.playerId === playerId);
  }),
}));

// Mock the shared DynamoDB docClient used by rewards.service.
// Capture every PutCommand payload so we can verify reward records.
let rewardPutCaptures = [];

jest.mock('../shared/config/dynamo', () => ({
  docClient: {
    send: jest.fn(async (cmd) => {
      // The rewards service sends PutCommand — capture the input
      if (cmd && cmd.input && cmd.input.Item) {
        rewardPutCaptures.push(cmd.input.Item);
        rewardsStore.push(cmd.input.Item);
      }
      return {};
    }),
  },
}));

// ---------------------------------------------------------------------------
// Requires (after mocks)
// ---------------------------------------------------------------------------
const checkInRoute = require('../src/routes/check-in');
const internalRoute = require('../src/routes/internal');
const { setDateProvider, resetDateProvider } = checkInRoute;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Extract the POST handler from an Express Router. */
function getPostHandler(router) {
  return router.stack[0].route.stack[0].handle;
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

function makeCheckInReq(playerId = 'player-1') {
  return { playerId };
}

function makeHandCompletedReq(playerId, completedAt, tableId = 'table-1', handId = 'hand-1') {
  return {
    body: { playerId, tableId, handId, completedAt },
  };
}

/** Set the injectable date for check-in.js. */
function setDate(isoString) {
  setDateProvider(() => new Date(isoString));
}

// ---------------------------------------------------------------------------
// Lifecycle
// ---------------------------------------------------------------------------

afterEach(() => {
  // Reset in-memory stores
  playerStore = {};
  activityStore = [];
  freezeHistoryStore = [];
  rewardsStore = [];
  rewardPutCaptures = [];

  // Reset date provider
  resetDateProvider();

  // Clear mock call history
  jest.clearAllMocks();
});

// ---------------------------------------------------------------------------
// Test scenarios
// ---------------------------------------------------------------------------

describe('Integration: check-in -> streak update -> milestone reward flow', () => {
  const checkInHandler = getPostHandler(checkInRoute);

  // -----------------------------------------------------------------------
  // Scenario 1: Full 3-day streak flow
  // -----------------------------------------------------------------------
  describe('Scenario 1: Full 3-day streak flow', () => {
    it('should reach loginStreak=3 and trigger the 3-day milestone reward', async () => {
      const playerId = 'player-3day';

      // --- Day 1 ---
      setDate('2026-03-01T12:00:00Z');
      let res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1);
      expect(res.body.todayCheckedIn).toBe(true);
      // No milestone at day 1
      expect(res.body.milestone).toBeNull();

      // Verify player persisted in store
      expect(playerStore[playerId]).toBeDefined();
      expect(playerStore[playerId].loginStreak).toBe(1);
      expect(playerStore[playerId].lastLoginDate).toBe('2026-03-01');

      // --- Day 2 ---
      setDate('2026-03-02T12:00:00Z');
      res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(2);
      expect(res.body.milestone).toBeNull();

      expect(playerStore[playerId].loginStreak).toBe(2);
      expect(playerStore[playerId].lastLoginDate).toBe('2026-03-02');

      // --- Day 3 ---
      setDate('2026-03-03T12:00:00Z');
      res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(3);
      expect(res.body.bestLoginStreak).toBe(3);

      // 3-day milestone should be triggered
      expect(res.body.milestone).not.toBeNull();
      expect(res.body.milestone.type).toBe('login_milestone');
      expect(res.body.milestone.milestone).toBe(3);
      expect(res.body.milestone.points).toBe(50); // 3-day loginReward from constants
      expect(res.body.milestone.playerId).toBe(playerId);

      // Verify the reward was persisted via docClient.send
      expect(rewardPutCaptures.length).toBe(1);
      expect(rewardPutCaptures[0].playerId).toBe(playerId);
      expect(rewardPutCaptures[0].type).toBe('login_milestone');
      expect(rewardPutCaptures[0].points).toBe(50);
      expect(rewardPutCaptures[0].milestone).toBe(3);

      // Verify activity records were written for all 3 days
      const playerActivities = activityStore.filter((a) => a.playerId === playerId);
      expect(playerActivities.length).toBe(3);
      expect(playerActivities[0].date).toBe('2026-03-01');
      expect(playerActivities[1].date).toBe('2026-03-02');
      expect(playerActivities[2].date).toBe('2026-03-03');
      expect(playerActivities[2].loginStreakAtDay).toBe(3);
    });

    it('should be idempotent — checking in twice on the same day does not double-count', async () => {
      const playerId = 'player-idempotent';

      // Day 1
      setDate('2026-03-01T12:00:00Z');
      await checkInHandler(makeCheckInReq(playerId), mockRes());

      // Day 1 again
      const res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1);
      expect(res.body.milestone).toBeNull();

      // Only one activity should have been written
      const activities = activityStore.filter((a) => a.playerId === playerId);
      expect(activities.length).toBe(1);
    });
  });

  // -----------------------------------------------------------------------
  // Scenario 2: Streak break and restart
  // -----------------------------------------------------------------------
  describe('Scenario 2: Streak break and restart', () => {
    it('should reset streak to 1 when a day is skipped (2-day gap, no freeze)', async () => {
      const playerId = 'player-break';

      // Day 1 — March 1
      setDate('2026-03-01T12:00:00Z');
      let res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1);

      // Ensure the monthly freeze grant has already been applied so the
      // next check-in won't auto-grant a freeze that would preserve the streak.
      playerStore[playerId].lastFreezeGrantDate = '2026-03';
      playerStore[playerId].freezesAvailable = 0;

      // Skip March 2 entirely — check in on March 3 (gap of 2 days)
      setDate('2026-03-03T12:00:00Z');
      res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1); // streak resets
      expect(res.body.bestLoginStreak).toBe(1);

      // Verify the activity recorded a streak break
      const activities = activityStore.filter((a) => a.playerId === playerId);
      const breakActivity = activities.find((a) => a.date === '2026-03-03');
      expect(breakActivity.streakBroken).toBe(true);
      expect(breakActivity.loginStreakAtDay).toBe(1);
    });

    it('should reset streak when multiple days are missed even with freeze available', async () => {
      const playerId = 'player-bigbreak';

      // Day 1 — March 1
      setDate('2026-03-01T12:00:00Z');
      await checkInHandler(makeCheckInReq(playerId), mockRes());

      // Manually give the player a freeze
      playerStore[playerId].freezesAvailable = 2;

      // Skip 2 days — check in on March 4 (gap of 3 days)
      setDate('2026-03-04T12:00:00Z');
      const res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1); // resets despite having freezes
    });
  });

  // -----------------------------------------------------------------------
  // Scenario 3: Freeze protection flow
  // -----------------------------------------------------------------------
  describe('Scenario 3: Freeze protection flow', () => {
    it('should preserve streak and consume freeze when exactly 1 day is missed', async () => {
      const playerId = 'player-freeze';

      // Day 1 — March 1
      setDate('2026-03-01T12:00:00Z');
      let res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.body.loginStreak).toBe(1);

      // Day 2 — March 2 (consecutive)
      setDate('2026-03-02T12:00:00Z');
      res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.body.loginStreak).toBe(2);

      // Grant a freeze to the player
      playerStore[playerId].freezesAvailable = 1;

      // Skip March 3 — check in on March 4 (exactly 2 days since last login)
      setDate('2026-03-04T12:00:00Z');
      res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      // Streak should be preserved and incremented (2 + 1 = 3)
      expect(res.body.loginStreak).toBe(3);
      expect(res.body.bestLoginStreak).toBe(3);

      // Freeze should have been consumed
      expect(playerStore[playerId].freezesAvailable).toBe(0);
      expect(playerStore[playerId].freezesUsedThisMonth).toBe(1);

      // Activity should record freeze usage
      const activities = activityStore.filter((a) => a.playerId === playerId);
      const freezeActivity = activities.find((a) => a.date === '2026-03-04');
      expect(freezeActivity.freezeUsed).toBe(true);
      expect(freezeActivity.streakBroken).toBe(false);
      expect(freezeActivity.loginStreakAtDay).toBe(3);

      // 3-day milestone should also fire
      expect(res.body.milestone).not.toBeNull();
      expect(res.body.milestone.type).toBe('login_milestone');
      expect(res.body.milestone.milestone).toBe(3);
      expect(res.body.milestone.points).toBe(50);
    });

    it('should break streak when 1 day missed but no freeze available', async () => {
      const playerId = 'player-nofreeze';

      // Day 1 — March 1
      setDate('2026-03-01T12:00:00Z');
      await checkInHandler(makeCheckInReq(playerId), mockRes());

      // Day 2 — March 2 (consecutive)
      // Note: the monthly freeze grant fires on day 2 because lastFreezeGrantDate
      // is '' (from new player). We need to neutralise that freeze afterwards.
      setDate('2026-03-02T12:00:00Z');
      await checkInHandler(makeCheckInReq(playerId), mockRes());

      expect(playerStore[playerId].loginStreak).toBe(2);

      // Remove the auto-granted monthly freeze so the player truly has none
      playerStore[playerId].freezesAvailable = 0;

      // Skip March 3 — check in on March 4 with no freeze
      setDate('2026-03-04T12:00:00Z');
      const res = mockRes();
      await checkInHandler(makeCheckInReq(playerId), res);

      expect(res.statusCode).toBe(200);
      expect(res.body.loginStreak).toBe(1); // streak broken
    });
  });

  // -----------------------------------------------------------------------
  // Scenario 4: Play streak via hand-completed
  // -----------------------------------------------------------------------
  describe('Scenario 4: Play streak via hand-completed', () => {
    const handHandler = getPostHandler(internalRoute);

    it('should increment play streak on consecutive daily hand completions', async () => {
      const playerId = 'player-play';

      // Day 1 hand completion
      let res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-01T15:30:00Z'),
        res
      );

      expect(res.statusCode).toBe(200);
      expect(res.body.playStreak).toBe(1);
      expect(res.body.alreadyPlayedToday).toBe(false);

      // Verify player stored
      expect(playerStore[playerId]).toBeDefined();
      expect(playerStore[playerId].playStreak).toBe(1);
      expect(playerStore[playerId].lastPlayDate).toBe('2026-03-01');

      // Day 2 hand completion
      res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-02T10:00:00Z', 'table-2', 'hand-2'),
        res
      );

      expect(res.statusCode).toBe(200);
      expect(res.body.playStreak).toBe(2);

      // Day 3 hand completion — hits 3-day play milestone
      res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-03T18:00:00Z', 'table-3', 'hand-3'),
        res
      );

      expect(res.statusCode).toBe(200);
      expect(res.body.playStreak).toBe(3);
      expect(res.body.bestPlayStreak).toBe(3);

      // 3-day play milestone should be awarded
      expect(res.body.milestone).not.toBeNull();
      expect(res.body.milestone.type).toBe('play_milestone');
      expect(res.body.milestone.milestone).toBe(3);
      expect(res.body.milestone.points).toBe(100); // 3-day playReward from constants

      // Verify reward capture
      const playRewards = rewardPutCaptures.filter((r) => r.type === 'play_milestone');
      expect(playRewards.length).toBe(1);
      expect(playRewards[0].points).toBe(100);
    });

    it('should be idempotent — second hand on same day does not re-increment', async () => {
      const playerId = 'player-play-idem';

      // First hand on day 1
      let res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-01T10:00:00Z'),
        res
      );
      expect(res.body.playStreak).toBe(1);
      expect(res.body.alreadyPlayedToday).toBe(false);

      // Second hand on day 1
      res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-01T14:00:00Z', 'table-2', 'hand-2'),
        res
      );
      expect(res.body.playStreak).toBe(1);
      expect(res.body.alreadyPlayedToday).toBe(true);
    });

    it('should reset play streak when a day is skipped without freeze', async () => {
      const playerId = 'player-play-break';

      // Day 1
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-01T12:00:00Z'),
        mockRes()
      );

      // Skip March 2 — play on March 3 (2-day gap, no freeze)
      const res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-03T12:00:00Z', 'table-2', 'hand-2'),
        res
      );

      expect(res.statusCode).toBe(200);
      expect(res.body.playStreak).toBe(1); // streak resets
      expect(res.body.streakBroken).toBe(true);
    });

    it('should preserve play streak with freeze when exactly 1 day is missed', async () => {
      const playerId = 'player-play-freeze';

      // Day 1
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-01T12:00:00Z'),
        mockRes()
      );

      // Grant a freeze
      playerStore[playerId].freezesAvailable = 1;

      // Skip March 2 — play on March 3 (exactly 2 days since last play)
      const res = mockRes();
      await handHandler(
        makeHandCompletedReq(playerId, '2026-03-03T12:00:00Z', 'table-2', 'hand-2'),
        res
      );

      expect(res.statusCode).toBe(200);
      // calculateStreakUpdate returns same streak (preserved, not incremented) when freeze is used
      expect(res.body.playStreak).toBe(1);
      expect(res.body.freezeConsumed).toBe(true);
      expect(res.body.streakBroken).toBe(false);

      // Freeze should be consumed
      expect(playerStore[playerId].freezesAvailable).toBe(0);
    });

    it('should validate required fields on hand-completed', async () => {
      const res = mockRes();
      await handHandler({ body: { playerId: 'p1' } }, res);

      expect(res.statusCode).toBe(400);
      expect(res.body.error).toBe('Bad Request');
    });
  });
});
