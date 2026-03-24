import {
  PutCommand,
  GetCommand,
  QueryCommand,
  UpdateCommand,
  ScanCommand,
  BatchWriteCommand,
} from '@aws-sdk/lib-dynamodb';

// Mock the shared dynamo config before importing the repository
const mockSend = jest.fn();
jest.mock('../shared/config/dynamo', () => ({
  docClient: { send: mockSend },
}));

import {
  getPlayerStreak,
  putPlayerStreak,
  updatePlayerStreak,
  getAllPlayerStreaks,
  putActivity,
  getActivity,
  queryActivityByDateRange,
  putReward,
  queryRewards,
  putFreezeHistory,
  queryFreezeHistory,
  batchPutActivities,
  batchPutRewards,
} from '../src/repositories/dynamo.repository';

import type { PlayerStreak, DailyActivity, StreakReward, FreezeHistory } from '../src/models/streak.model';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makePlayer(overrides: Partial<PlayerStreak> = {}): PlayerStreak {
  return {
    playerId: 'player-1',
    loginStreak: 5,
    playStreak: 3,
    bestLoginStreak: 10,
    bestPlayStreak: 7,
    lastLoginDate: '2026-03-20',
    lastPlayDate: '2026-03-19',
    freezesAvailable: 2,
    freezesUsedThisMonth: 0,
    lastFreezeGrantDate: '2026-03',
    updatedAt: '2026-03-20T12:00:00Z',
    ...overrides,
  };
}

function makeActivity(overrides: Partial<DailyActivity> = {}): DailyActivity {
  return {
    playerId: 'player-1',
    date: '2026-03-20',
    loggedIn: true,
    played: false,
    freezeUsed: false,
    streakBroken: false,
    loginStreakAtDay: 5,
    playStreakAtDay: 3,
    ...overrides,
  };
}

function makeReward(overrides: Partial<StreakReward> = {}): StreakReward {
  return {
    playerId: 'player-1',
    rewardId: 'reward-001',
    type: 'login_milestone',
    milestone: 7,
    points: 150,
    streakCount: 7,
    createdAt: '2026-03-20T12:00:00Z',
    ...overrides,
  };
}

function makeFreeze(overrides: Partial<FreezeHistory> = {}): FreezeHistory {
  return {
    playerId: 'player-1',
    date: '2026-03-15',
    source: 'free_monthly',
    ...overrides,
  };
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

beforeEach(() => {
  mockSend.mockReset();
});

describe('streaks-players repository', () => {
  describe('getPlayerStreak', () => {
    it('returns the player record when it exists', async () => {
      const player = makePlayer();
      mockSend.mockResolvedValueOnce({ Item: player });

      const result = await getPlayerStreak('player-1');

      expect(result).toEqual(player);
      expect(mockSend).toHaveBeenCalledTimes(1);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(GetCommand);
      expect(cmd.input.TableName).toBe('streaks-players');
      expect(cmd.input.Key).toEqual({ playerId: 'player-1' });
    });

    it('returns null when player does not exist', async () => {
      mockSend.mockResolvedValueOnce({ Item: undefined });

      const result = await getPlayerStreak('nonexistent');
      expect(result).toBeNull();
    });
  });

  describe('putPlayerStreak', () => {
    it('writes a player record to DynamoDB', async () => {
      mockSend.mockResolvedValueOnce({});
      const player = makePlayer();

      await putPlayerStreak(player);

      expect(mockSend).toHaveBeenCalledTimes(1);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(PutCommand);
      expect(cmd.input.TableName).toBe('streaks-players');
      expect(cmd.input.Item).toEqual(player);
    });
  });

  describe('updatePlayerStreak', () => {
    it('builds a SET expression for partial updates', async () => {
      mockSend.mockResolvedValueOnce({});

      await updatePlayerStreak('player-1', { loginStreak: 6, lastLoginDate: '2026-03-21' });

      expect(mockSend).toHaveBeenCalledTimes(1);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(UpdateCommand);
      expect(cmd.input.UpdateExpression).toContain('SET');
      expect(cmd.input.Key).toEqual({ playerId: 'player-1' });
    });

    it('does nothing when updates object is empty', async () => {
      await updatePlayerStreak('player-1', {});
      expect(mockSend).not.toHaveBeenCalled();
    });
  });

  describe('getAllPlayerStreaks', () => {
    it('scans and returns all players', async () => {
      const players = [makePlayer(), makePlayer({ playerId: 'player-2' })];
      mockSend.mockResolvedValueOnce({ Items: players });

      const result = await getAllPlayerStreaks();
      expect(result).toEqual(players);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(ScanCommand);
    });
  });
});

describe('streaks-activity repository', () => {
  describe('putActivity', () => {
    it('writes an activity record', async () => {
      mockSend.mockResolvedValueOnce({});
      const activity = makeActivity();

      await putActivity(activity);

      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(PutCommand);
      expect(cmd.input.TableName).toBe('streaks-activity');
      expect(cmd.input.Item).toEqual(activity);
    });
  });

  describe('getActivity', () => {
    it('returns a single day activity record', async () => {
      const activity = makeActivity();
      mockSend.mockResolvedValueOnce({ Item: activity });

      const result = await getActivity('player-1', '2026-03-20');
      expect(result).toEqual(activity);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(GetCommand);
      expect(cmd.input.Key).toEqual({ playerId: 'player-1', date: '2026-03-20' });
    });

    it('returns null when no activity exists', async () => {
      mockSend.mockResolvedValueOnce({ Item: undefined });

      const result = await getActivity('player-1', '2026-01-01');
      expect(result).toBeNull();
    });
  });

  describe('queryActivityByDateRange', () => {
    it('returns activities within a date range sorted by date', async () => {
      const activities = [
        makeActivity({ date: '2026-03-18' }),
        makeActivity({ date: '2026-03-19' }),
        makeActivity({ date: '2026-03-20' }),
      ];
      mockSend.mockResolvedValueOnce({ Items: activities });

      const result = await queryActivityByDateRange('player-1', '2026-03-18', '2026-03-20');

      expect(result).toEqual(activities);
      expect(result).toHaveLength(3);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(QueryCommand);
      expect(cmd.input.ScanIndexForward).toBe(true);
      expect(cmd.input.ExpressionAttributeValues).toEqual({
        ':pid': 'player-1',
        ':start': '2026-03-18',
        ':end': '2026-03-20',
      });
    });

    it('returns empty array when no activities match', async () => {
      mockSend.mockResolvedValueOnce({ Items: [] });

      const result = await queryActivityByDateRange('player-1', '2020-01-01', '2020-01-31');
      expect(result).toEqual([]);
    });
  });
});

describe('streaks-rewards repository', () => {
  describe('putReward', () => {
    it('writes a reward record', async () => {
      mockSend.mockResolvedValueOnce({});
      const reward = makeReward();

      await putReward(reward);

      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(PutCommand);
      expect(cmd.input.TableName).toBe('streaks-rewards');
      expect(cmd.input.Item).toEqual(reward);
    });
  });

  describe('queryRewards', () => {
    it('returns rewards sorted by createdAt descending', async () => {
      const rewards = [
        makeReward({ rewardId: 'r2', createdAt: '2026-03-20T12:00:00Z' }),
        makeReward({ rewardId: 'r1', createdAt: '2026-03-10T12:00:00Z' }),
      ];
      mockSend.mockResolvedValueOnce({ Items: rewards });

      const result = await queryRewards('player-1');

      expect(result).toEqual(rewards);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(QueryCommand);
      expect(cmd.input.ScanIndexForward).toBe(false);
    });

    it('returns empty array when player has no rewards', async () => {
      mockSend.mockResolvedValueOnce({ Items: [] });

      const result = await queryRewards('player-1');
      expect(result).toEqual([]);
    });
  });
});

describe('streaks-freeze-history repository', () => {
  describe('putFreezeHistory', () => {
    it('writes a freeze history record', async () => {
      mockSend.mockResolvedValueOnce({});
      const freeze = makeFreeze();

      await putFreezeHistory(freeze);

      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(PutCommand);
      expect(cmd.input.TableName).toBe('streaks-freeze-history');
      expect(cmd.input.Item).toEqual(freeze);
    });
  });

  describe('queryFreezeHistory', () => {
    it('returns all freeze records for a player', async () => {
      const freezes = [
        makeFreeze({ date: '2026-02-10' }),
        makeFreeze({ date: '2026-03-15' }),
      ];
      mockSend.mockResolvedValueOnce({ Items: freezes });

      const result = await queryFreezeHistory('player-1');

      expect(result).toEqual(freezes);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(QueryCommand);
      expect(cmd.input.ScanIndexForward).toBe(true);
    });

    it('returns empty array when player has no freeze history', async () => {
      mockSend.mockResolvedValueOnce({ Items: [] });

      const result = await queryFreezeHistory('nonexistent');
      expect(result).toEqual([]);
    });
  });
});

describe('batch operations', () => {
  describe('batchPutActivities', () => {
    it('writes a batch of activity records', async () => {
      mockSend.mockResolvedValueOnce({});
      const activities = [
        makeActivity({ date: '2026-03-18' }),
        makeActivity({ date: '2026-03-19' }),
      ];

      await batchPutActivities(activities);

      expect(mockSend).toHaveBeenCalledTimes(1);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(BatchWriteCommand);
      expect(cmd.input.RequestItems!['streaks-activity']).toHaveLength(2);
    });

    it('chunks requests when more than 25 items', async () => {
      mockSend.mockResolvedValue({});
      const activities = Array.from({ length: 30 }, (_, i) =>
        makeActivity({ date: `2026-01-${String(i + 1).padStart(2, '0')}` })
      );

      await batchPutActivities(activities);

      expect(mockSend).toHaveBeenCalledTimes(2);
      const first = mockSend.mock.calls[0][0];
      const second = mockSend.mock.calls[1][0];
      expect(first.input.RequestItems!['streaks-activity']).toHaveLength(25);
      expect(second.input.RequestItems!['streaks-activity']).toHaveLength(5);
    });
  });

  describe('batchPutRewards', () => {
    it('writes a batch of reward records', async () => {
      mockSend.mockResolvedValueOnce({});
      const rewards = [makeReward({ rewardId: 'r1' }), makeReward({ rewardId: 'r2' })];

      await batchPutRewards(rewards);

      expect(mockSend).toHaveBeenCalledTimes(1);
      const cmd = mockSend.mock.calls[0][0];
      expect(cmd).toBeInstanceOf(BatchWriteCommand);
      expect(cmd.input.RequestItems!['streaks-rewards']).toHaveLength(2);
    });
  });
});
