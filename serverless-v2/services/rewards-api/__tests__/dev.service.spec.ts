import { NotFoundException } from '@nestjs/common';
import { DevService } from '../src/dev/dev.service';
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

describe('DevService', () => {
  let service: DevService;
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

    service = new DevService(dynamo, redis);
  });

  describe('getPlayerRewards', () => {
    it('throws NotFoundException when player does not exist', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.getPlayerRewards('p1')).rejects.toThrow(NotFoundException);
    });

    it('returns rewards data for existing player', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 2, points: 600, totalEarned: 1200 }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getPlayerRewards('p1');
      expect(result.playerId).toBe('p1');
      expect(result.tier).toBe('Silver');
      expect(result.points).toBe(600);
      expect(result.totalEarned).toBe(1200);
      expect(result.nextTierName).toBe('Gold');
      expect(result.nextTierAt).toBe(2000);
    });

    it('returns null nextTier for Platinum player', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 4, totalEarned: 15000 }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getPlayerRewards('p1');
      expect(result.tier).toBe('Platinum');
      expect(result.nextTierName).toBeNull();
      expect(result.nextTierAt).toBeNull();
    });
  });

  describe('setPlayerPoints', () => {
    it('throws NotFoundException when player does not exist', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.setPlayerPoints('p1', 500, 500)).rejects.toThrow(NotFoundException);
    });

    it('updates points and totalEarned on player', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer())
        .mockResolvedValueOnce(makePlayer({ points: 5000, totalEarned: 10000, tier: 4 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.setPlayerPoints('p1', 5000, 10000);
      expect(result.points).toBe(5000);
      expect(result.totalEarned).toBe(10000);
    });

    it('recalculates tier from totalEarned', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 100, totalEarned: 400 }))
        .mockResolvedValueOnce(makePlayer({ tier: 4, points: 10000, totalEarned: 10000 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 10000, 10000);

      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 4 }));
    });

    it('writes an adjustment transaction', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ points: 100 }))
        .mockResolvedValueOnce(makePlayer({ points: 500 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 500, 1000, 'test_reason');

      expect(dynamo.addTransaction).toHaveBeenCalledWith(
        'p1',
        expect.objectContaining({
          type: 'adjustment',
          earnedPoints: 400,
          reason: 'test_reason',
          balanceAfter: 500,
        }),
      );
    });

    it('updates Redis leaderboard with new points', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer())
        .mockResolvedValueOnce(makePlayer({ points: 3000 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 3000, 5000);

      expect(redis.updateLeaderboard).toHaveBeenCalledWith(
        expect.stringMatching(/^\d{4}-\d{2}$/),
        'p1',
        3000,
      );
    });

    it('uses default reason when none provided', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer())
        .mockResolvedValueOnce(makePlayer());
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 200, 500);

      expect(dynamo.addTransaction).toHaveBeenCalledWith(
        'p1',
        expect.objectContaining({ reason: 'dev_adjustment' }),
      );
    });

    it('calls putTierHistory when tier changes', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 100, totalEarned: 400 }))
        .mockResolvedValueOnce(makePlayer({ tier: 4, points: 10000, totalEarned: 10000 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.putTierHistory.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 10000, 10000);

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

      await service.setPlayerPoints('p1', 200, 300);

      expect(dynamo.putTierHistory).not.toHaveBeenCalled();
    });

    it('sets Bronze tier for low totalEarned', async () => {
      dynamo.getPlayer
        .mockResolvedValueOnce(makePlayer({ tier: 3 }))
        .mockResolvedValueOnce(makePlayer({ tier: 1, points: 50, totalEarned: 50 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.setPlayerPoints('p1', 50, 50);

      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 1 }));
    });
  });

  describe('getTierHistory', () => {
    it('returns formatted timeline', async () => {
      dynamo.getTierHistory.mockResolvedValue([
        {
          playerId: 'p1',
          monthKey: '2026-01',
          tier: 1,
          tierName: 'Bronze',
          points: 100,
          totalEarned: 100,
          reason: 'monthly_reset',
          createdAt: '2026-02-01T00:00:00Z',
        },
        {
          playerId: 'p1',
          monthKey: '2026-02',
          tier: 2,
          tierName: 'Silver',
          points: 600,
          totalEarned: 700,
          reason: 'monthly_reset',
          createdAt: '2026-03-01T00:00:00Z',
        },
      ]);

      const result = await service.getTierHistory('p1');
      expect(result.playerId).toBe('p1');
      expect(result.history).toHaveLength(2);
      expect(result.history[0].tier).toBe('Bronze');
      expect(result.history[1].tier).toBe('Silver');
      expect(result.history[0]).not.toHaveProperty('playerId');
    });
  });
});
