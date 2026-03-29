import { NotFoundException } from '@nestjs/common';
import { PointsService } from '../src/points/points.service';
import { DynamoService } from '../src/dynamo/dynamo.service';
import { RedisService } from '../src/redis/redis.service';
import { PlayerRecord } from '../../../../shared/types/rewards';

// Prevent actual AWS/Redis connections
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

describe('PointsService', () => {
  let service: PointsService;
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
    } as unknown as jest.Mocked<DynamoService>;

    redis = {
      updateLeaderboard: jest.fn(),
      getTopPlayers: jest.fn(),
      getPlayersAroundRank: jest.fn(),
      getPlayerRank: jest.fn(),
    } as unknown as jest.Mocked<RedisService>;

    service = new PointsService(dynamo, redis);
  });

  const defaultDto = { tableId: 1, tableStakes: '1/2', bigBlind: 2.0, handId: 'h1' };

  describe('awardPoints', () => {
    it('throws NotFoundException when player does not exist', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.awardPoints('p1', defaultDto)).rejects.toThrow(NotFoundException);
    });

    it('calculates correct base points for big blind bracket', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      // bb 0.1 → bracket 1pt, Bronze multiplier 1.0 → earned 1
      expect(result.earnedPoints).toBe(1);
    });

    it('applies tier multiplier to base points', async () => {
      // Gold tier (number: 3, multiplier: 1.5), bb 2.0 → base 5
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 3, points: 2500, totalEarned: 2500 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      expect(result.earnedPoints).toBe(Math.round(5 * 1.5)); // 8
    });

    it('updates points and totalEarned correctly', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ points: 100, totalEarned: 400 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      // 1pt earned: 100+1=101 points, 400+1=401 totalEarned
      expect(result.newPoints).toBe(101);
      expect(result.newTotalEarned).toBe(401);
    });

    it('triggers tier upgrade when totalEarned crosses threshold', async () => {
      // Player at Gold (tier 3), totalEarned 9995 + earning 5pts → crosses 10000 for Platinum
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 3, points: 5000, totalEarned: 9995 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      // Gold multiplier 1.5, base 5 → earned 8, totalEarned 10003
      expect(result.tier).toBe('Platinum');
      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 4 }));
    });

    it('creates notification on tier upgrade', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 1, points: 0, totalEarned: 495 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockResolvedValue();

      // bb 2.0 → base 5, Bronze 1.0x → 5pts, totalEarned 500 → Silver
      await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      expect(dynamo.addNotification).toHaveBeenCalledWith(
        expect.objectContaining({
          playerId: 'p1',
          type: 'tier_upgrade',
          title: expect.stringContaining('Silver'),
        }),
      );
    });

    it('does not upgrade tier when totalEarned is below threshold', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 1, points: 0, totalEarned: 100 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      expect(result.tier).toBe('Bronze');
      expect(dynamo.addNotification).not.toHaveBeenCalled();
    });

    it('does not downgrade tier when totalEarned still qualifies', async () => {
      // Player at Gold (tier 3), totalEarned 5000 → still Gold (min 2000)
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 3, points: 10, totalEarned: 5000 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      expect(result.tier).toBe('Gold');
    });

    it('creates tier downgrade notification when totalEarned drops tier', async () => {
      // Player manually set to Gold (tier 3) but totalEarned 400 → getTierForPoints returns Bronze
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 3, points: 10, totalEarned: 400, handsPlayed: 50 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockResolvedValue();

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      expect(result.tier).toBe('Bronze');
      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 1 }));
      expect(dynamo.addNotification).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'tier_downgrade',
          title: expect.stringContaining('Bronze'),
        }),
      );
    });

    it('increments handsPlayed in player update', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ handsPlayed: 10 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      await service.awardPoints('p1', defaultDto);
      expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ handsPlayed: 11 }));
    });

    it('creates milestone notification on first hand', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ handsPlayed: 0, totalEarned: 100 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockResolvedValue();

      await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      expect(dynamo.addNotification).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'milestone',
          title: 'First Hand!',
        }),
      );
    });

    it('creates milestone notification when totalEarned crosses 1000', async () => {
      // totalEarned 995 + 5pts (bb 2.0, Bronze) = 1000
      dynamo.getPlayer.mockResolvedValue(makePlayer({ handsPlayed: 50, totalEarned: 995 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockResolvedValue();

      await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      expect(dynamo.addNotification).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'milestone',
          title: 'Point Collector',
        }),
      );
    });

    it('milestone notification failure does not break the award', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ handsPlayed: 0, totalEarned: 100 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockRejectedValue(new Error('DynamoDB error'));

      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 0.1 });
      expect(result.earnedPoints).toBe(1);
    });

    it('writes immutable transaction record', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      await service.awardPoints('p1', defaultDto);
      expect(dynamo.addTransaction).toHaveBeenCalledWith(
        'p1',
        expect.objectContaining({
          type: 'gameplay',
          tableId: 1,
          tableStakes: '1/2',
        }),
      );
    });

    it('updates Redis leaderboard with new points', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ points: 100 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      expect(redis.updateLeaderboard).toHaveBeenCalledWith(
        expect.stringMatching(/^\d{4}-\d{2}$/), // YYYY-MM format
        'p1',
        105, // 100 + 5 (base 5 * Bronze 1.0x)
      );
    });

    it('returns complete AwardPointsResponse', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();

      const result = await service.awardPoints('p1', defaultDto);
      expect(result).toEqual(
        expect.objectContaining({
          playerId: 'p1',
          earnedPoints: expect.any(Number),
          newPoints: expect.any(Number),
          newTotalEarned: expect.any(Number),
          tier: expect.any(String),
          transaction: expect.objectContaining({
            type: 'gameplay',
            basePoints: expect.any(Number),
            multiplier: expect.any(Number),
          }),
        }),
      );
    });

    it('notification failure does not break the award', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 1, points: 0, totalEarned: 495 }));
      dynamo.addTransaction.mockResolvedValue();
      dynamo.updatePlayer.mockResolvedValue();
      dynamo.addNotification.mockRejectedValue(new Error('DynamoDB error'));

      // Should still succeed even though notification write fails
      const result = await service.awardPoints('p1', { ...defaultDto, bigBlind: 2.0 });
      expect(result.tier).toBe('Silver');
    });
  });

  describe('getLeaderboard', () => {
    it('returns top players by default', async () => {
      redis.getPlayerRank.mockResolvedValue(5);
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

      const result = await service.getLeaderboard('p1', 10);
      expect(result.leaderboard).toHaveLength(2);
      expect(result.leaderboard[0].rank).toBe(1);
      expect(result.leaderboard[0].displayName).toBe('Ace');
      expect(result.leaderboard[1].rank).toBe(2);
    });

    it('returns nearby view when requested', async () => {
      redis.getPlayerRank.mockResolvedValue(25);
      redis.getPlayersAroundRank.mockResolvedValue([
        { playerId: 'p20', score: 600 },
        { playerId: 'p1', score: 550 },
      ]);
      dynamo.getPlayers.mockResolvedValue(new Map());

      const result = await service.getLeaderboard('p1', 10, undefined, 'nearby');
      expect(redis.getPlayersAroundRank).toHaveBeenCalledWith(expect.any(String), 25, 5, 4);
      expect(result.leaderboard[0].rank).toBe(20); // max(1, 25 - 5) = 20
    });

    it('falls back to top view when player has no rank', async () => {
      redis.getPlayerRank.mockResolvedValue(null);
      redis.getTopPlayers.mockResolvedValue([]);
      dynamo.getPlayers.mockResolvedValue(new Map());

      await service.getLeaderboard('p1', 10, undefined, 'nearby');
      expect(redis.getTopPlayers).toHaveBeenCalled();
      expect(redis.getPlayersAroundRank).not.toHaveBeenCalled();
    });

    it('uses playerId as displayName fallback when username missing', async () => {
      redis.getPlayerRank.mockResolvedValue(1);
      redis.getTopPlayers.mockResolvedValue([{ playerId: 'p1', score: 1000 }]);
      dynamo.getPlayers.mockResolvedValue(new Map()); // no username found

      const result = await service.getLeaderboard('p1', 10);
      expect(result.leaderboard[0].displayName).toBe('p1');
    });

    it('calculates tier from score', async () => {
      redis.getPlayerRank.mockResolvedValue(1);
      redis.getTopPlayers.mockResolvedValue([{ playerId: 'p1', score: 10000 }]);
      dynamo.getPlayers.mockResolvedValue(new Map());

      const result = await service.getLeaderboard('p1', 10);
      expect(result.leaderboard[0].tier).toBe('Platinum');
    });

    it('includes playerRank in response', async () => {
      redis.getPlayerRank.mockResolvedValue(7);
      redis.getTopPlayers.mockResolvedValue([]);
      dynamo.getPlayers.mockResolvedValue(new Map());

      const result = await service.getLeaderboard('p1', 10);
      expect(result.playerRank).toBe(7);
    });

    it('uses current month when no month specified', async () => {
      redis.getPlayerRank.mockResolvedValue(null);
      redis.getTopPlayers.mockResolvedValue([]);
      dynamo.getPlayers.mockResolvedValue(new Map());

      await service.getLeaderboard('p1', 10);
      const monthKey = redis.getPlayerRank.mock.calls[0][0];
      expect(monthKey).toMatch(/^\d{4}-\d{2}$/);
    });
  });
});
