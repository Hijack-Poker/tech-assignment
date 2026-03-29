import { NotFoundException } from '@nestjs/common';
import { AdminService } from '../src/admin/admin.service';
import { DynamoService } from '../src/dynamo/dynamo.service';
import { RedisService } from '../src/redis/redis.service';
import { PlayerRecord } from '../../../../shared/types/rewards';

jest.mock('../../../shared/config/dynamo', () => ({ docClient: { send: jest.fn() } }));
jest.mock('../../../shared/config/redis', () => ({ redisClient: {} }));

function makePlayer(overrides: Partial<PlayerRecord> = {}): PlayerRecord {
  return {
    playerId: 'p1',
    username: 'TestPlayer',
    tier: 1,
    points: 100,
    totalEarned: 400,
    handsPlayed: 10,
    tournamentsPlayed: 0,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-03-01T00:00:00Z',
    ...overrides,
  };
}

describe('AdminService', () => {
  let service: AdminService;
  let dynamo: jest.Mocked<DynamoService>;
  let redis: jest.Mocked<RedisService>;

  beforeEach(() => {
    dynamo = {
      getPlayer: jest.fn(),
      getPlayers: jest.fn(),
      putPlayer: jest.fn(),
      updatePlayer: jest.fn(),
      addTransaction: jest.fn(),
      getTransactions: jest.fn(),
      countTransactions: jest.fn(),
      getAllPlayers: jest.fn(),
      addNotification: jest.fn(),
      getNotifications: jest.fn(),
      dismissNotification: jest.fn(),
      putTierHistory: jest.fn(),
      getTierHistory: jest.fn(),
    } as unknown as jest.Mocked<DynamoService>;

    redis = {
      updateLeaderboard: jest.fn(),
      getTopPlayers: jest.fn(),
      getPlayersAroundRank: jest.fn(),
      getPlayerRank: jest.fn(),
    } as unknown as jest.Mocked<RedisService>;

    service = new AdminService(dynamo, redis);
  });

  describe('getAdminPlayerRewards', () => {
    it('returns createdAt and updatedAt', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-03-15T00:00:00Z' }))
        .mockResolvedValueOnce(makePlayer({ createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-03-15T00:00:00Z' }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getAdminPlayerRewards('p1');
      expect(result.createdAt).toBe('2026-01-01T00:00:00Z');
      expect(result.updatedAt).toBe('2026-03-15T00:00:00Z');
      expect(result.playerId).toBe('p1');
    });

    it('throws NotFoundException for missing player', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.getAdminPlayerRewards('p1')).rejects.toThrow(NotFoundException);
    });
  });

  describe('adjustPoints', () => {
    it('credits correctly with positive delta', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ points: 100, totalEarned: 400 }))
        .mockResolvedValueOnce(makePlayer({ points: 200, totalEarned: 500 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.adjustPoints('p1', 100, 'bonus');

      expect(dynamo.addTransaction).toHaveBeenCalledWith('p1', expect.objectContaining({
        reason: 'bonus',
      }));
    });

    it('debits correctly with negative delta', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ points: 500, totalEarned: 1000 }))
        .mockResolvedValueOnce(makePlayer({ points: 300, totalEarned: 1000 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.adjustPoints('p1', -200, 'correction');

      // Points should floor at 0, totalEarned unchanged for negative delta
      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({
        points: 300,
        totalEarned: 1000,
      }));
    });

    it('does not increase totalEarned on debit', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ points: 500, totalEarned: 1000 }))
        .mockResolvedValueOnce(makePlayer({ points: 400 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.adjustPoints('p1', -100, 'penalty');

      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({
        totalEarned: 1000,
      }));
    });

    it('throws NotFoundException for missing player', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.adjustPoints('p1', 100, 'test')).rejects.toThrow(NotFoundException);
    });

    it('calls putTierHistory when tier changes', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 100, totalEarned: 400 }))
        .mockResolvedValueOnce(makePlayer({ tier: 4, points: 10000, totalEarned: 10400 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.putTierHistory.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.adjustPoints('p1', 9900, 'boost');

      expect(dynamo.putTierHistory).toHaveBeenCalledWith(
        expect.objectContaining({
          playerId: 'p1',
          tier: 4,
          tierName: 'Platinum',
          reason: 'tier_change',
        }),
      );
    });

    it('does not call putTierHistory when tier stays the same', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 100, totalEarned: 200 }))
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 200, totalEarned: 300 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.adjustPoints('p1', 100, 'small bonus');

      expect(dynamo.putTierHistory).not.toHaveBeenCalled();
    });
  });

  describe('getAdminLeaderboard', () => {
    it('returns entries with playerId', async () => {
      redis.getTopPlayers.mockResolvedValue([
        { playerId: 'p1', score: 1000 },
        { playerId: 'p2', score: 800 },
      ]);
      dynamo.getPlayers.mockResolvedValue(
        new Map([
          ['p1', makePlayer({ playerId: 'p1', username: 'Ace' })],
          ['p2', makePlayer({ playerId: 'p2', username: 'King' })],
        ]),
      );

      const result = await service.getAdminLeaderboard(100);
      expect(result.leaderboard).toHaveLength(2);
      expect(result.leaderboard[0].playerId).toBe('p1');
      expect(result.leaderboard[0].displayName).toBe('Ace');
      expect(result.leaderboard[0].rank).toBe(1);
    });
  });

  describe('overrideTier', () => {
    it('updates player tier and writes history', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 100, totalEarned: 400 }))
        .mockResolvedValueOnce(makePlayer({ tier: 3 }));
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.putTierHistory.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.overrideTier('p1', 3, '2026-12-31T00:00:00Z');

      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 3 }));
      expect(dynamo.putTierHistory).toHaveBeenCalledWith(
        expect.objectContaining({
          playerId: 'p1',
          tier: 3,
          reason: 'tier_override',
        }),
      );
    });

    it('throws NotFoundException for missing player', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.overrideTier('p1', 3, '2026-12-31')).rejects.toThrow(NotFoundException);
    });
  });
});
