'use strict';

jest.mock('../shared/config/dynamo', () => {
  const sendMock = jest.fn();
  return {
    docClient: { send: sendMock },
    __sendMock: sendMock,
  };
});

const { docClient, __sendMock: sendMock } = require('../shared/config/dynamo');
const {
  getPlayer,
  putPlayer,
  updatePlayer,
  addActivity,
  getActivity,
  getAllPlayers,
  PLAYERS_TABLE,
  ACTIVITY_TABLE,
  REWARDS_TABLE,
  FREEZE_HISTORY_TABLE,
} = require('../src/services/dynamo.service');

beforeEach(() => {
  sendMock.mockReset();
});

describe('Streaks — DynamoDB Service', () => {
  describe('table constants', () => {
    it('should define all four table names', () => {
      expect(PLAYERS_TABLE).toBe('streaks-players');
      expect(ACTIVITY_TABLE).toBe('streaks-activity');
      expect(REWARDS_TABLE).toBe('streaks-rewards');
      expect(FREEZE_HISTORY_TABLE).toBe('streaks-freeze-history');
    });
  });

  describe('getPlayer', () => {
    it('should return the player item when found', async () => {
      const player = { playerId: 'p1', currentStreak: 5 };
      sendMock.mockResolvedValue({ Item: player });

      const result = await getPlayer('p1');
      expect(result).toEqual(player);
      expect(sendMock).toHaveBeenCalledTimes(1);
    });

    it('should return null when player not found', async () => {
      sendMock.mockResolvedValue({});

      const result = await getPlayer('nonexistent');
      expect(result).toBeNull();
    });
  });

  describe('putPlayer', () => {
    it('should send a PutCommand with the player data', async () => {
      sendMock.mockResolvedValue({});
      const player = { playerId: 'p1', currentStreak: 1 };

      await putPlayer(player);
      expect(sendMock).toHaveBeenCalledTimes(1);
    });
  });

  describe('updatePlayer', () => {
    it('should build an UpdateCommand from the updates object', async () => {
      sendMock.mockResolvedValue({});

      await updatePlayer('p1', { currentStreak: 10, longestStreak: 15 });
      expect(sendMock).toHaveBeenCalledTimes(1);
    });
  });

  describe('addActivity', () => {
    it('should insert an activity record with checkedIn=true', async () => {
      sendMock.mockResolvedValue({});

      await addActivity('p1', '2024-01-15');
      // 1 GET (merge check) + 1 PUT
      expect(sendMock).toHaveBeenCalledTimes(2);
    });
  });

  describe('getActivity', () => {
    it('should return activity items for a date range', async () => {
      const items = [
        { playerId: 'p1', date: '2024-01-14', checkedIn: true },
        { playerId: 'p1', date: '2024-01-15', checkedIn: true },
      ];
      sendMock.mockResolvedValue({ Items: items });

      const result = await getActivity('p1', '2024-01-14', '2024-01-15');
      expect(result).toEqual(items);
      expect(result).toHaveLength(2);
    });

    it('should return empty array when no activity found', async () => {
      sendMock.mockResolvedValue({});

      const result = await getActivity('p1', '2024-01-01', '2024-01-02');
      expect(result).toEqual([]);
    });
  });

  describe('getAllPlayers', () => {
    it('should return all player items via scan', async () => {
      const items = [{ playerId: 'p1' }, { playerId: 'p2' }];
      sendMock.mockResolvedValue({ Items: items });

      const result = await getAllPlayers();
      expect(result).toHaveLength(2);
    });

    it('should return empty array when no players exist', async () => {
      sendMock.mockResolvedValue({});

      const result = await getAllPlayers();
      expect(result).toEqual([]);
    });
  });
});
