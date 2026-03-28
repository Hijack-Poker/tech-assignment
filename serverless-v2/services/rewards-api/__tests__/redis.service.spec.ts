const mockRedis = {
  connect: jest.fn(),
  zadd: jest.fn(),
  zrevrange: jest.fn(),
  zrevrank: jest.fn(),
};

jest.mock('../../../shared/config/redis', () => ({
  redisClient: mockRedis,
}));

import { RedisService } from '../src/redis/redis.service';

describe('RedisService', () => {
  let service: RedisService;

  beforeEach(() => {
    service = new RedisService();
    jest.clearAllMocks();
  });

  describe('onModuleInit', () => {
    it('connects to Redis', async () => {
      mockRedis.connect.mockResolvedValue(undefined);
      await service.onModuleInit();
      expect(mockRedis.connect).toHaveBeenCalled();
    });

    it('does not throw on connection failure', async () => {
      mockRedis.connect.mockRejectedValue(new Error('Connection refused'));
      await expect(service.onModuleInit()).resolves.not.toThrow();
    });
  });

  describe('updateLeaderboard', () => {
    it('calls zadd with correct key and score', async () => {
      mockRedis.zadd.mockResolvedValue(1);
      await service.updateLeaderboard('2026-03', 'p1', 500);

      expect(mockRedis.zadd).toHaveBeenCalledWith('leaderboard:2026-03', 500, 'p1');
    });

    it('does not throw on Redis failure', async () => {
      mockRedis.zadd.mockRejectedValue(new Error('Redis down'));
      await expect(service.updateLeaderboard('2026-03', 'p1', 500)).resolves.not.toThrow();
    });
  });

  describe('getTopPlayers', () => {
    it('returns top players in descending order', async () => {
      mockRedis.zrevrange.mockResolvedValue(['p1', '1000', 'p2', '800']);

      const result = await service.getTopPlayers('2026-03', 10);
      expect(result).toEqual([
        { playerId: 'p1', score: 1000 },
        { playerId: 'p2', score: 800 },
      ]);
    });

    it('passes correct range (0 to count-1)', async () => {
      mockRedis.zrevrange.mockResolvedValue([]);
      await service.getTopPlayers('2026-03', 5);

      expect(mockRedis.zrevrange).toHaveBeenCalledWith('leaderboard:2026-03', 0, 4, 'WITHSCORES');
    });

    it('returns empty array on Redis failure', async () => {
      mockRedis.zrevrange.mockRejectedValue(new Error('Redis down'));
      const result = await service.getTopPlayers('2026-03', 10);
      expect(result).toEqual([]);
    });

    it('parses scores as numbers', async () => {
      mockRedis.zrevrange.mockResolvedValue(['p1', '3500']);
      const result = await service.getTopPlayers('2026-03', 1);
      expect(typeof result[0].score).toBe('number');
      expect(result[0].score).toBe(3500);
    });
  });

  describe('getPlayersAroundRank', () => {
    it('calculates correct window for mid-ranked player', async () => {
      mockRedis.zrevrange.mockResolvedValue(['p1', '100', 'p2', '90']);
      await service.getPlayersAroundRank('2026-03', 10, 5, 4);

      // start = max(0, 10 - 1 - 5) = 4, stop = 10 - 1 + 4 = 13
      expect(mockRedis.zrevrange).toHaveBeenCalledWith('leaderboard:2026-03', 4, 13, 'WITHSCORES');
    });

    it('clamps start to 0 for top-ranked players', async () => {
      mockRedis.zrevrange.mockResolvedValue([]);
      await service.getPlayersAroundRank('2026-03', 1, 5, 4);

      // start = max(0, 1 - 1 - 5) = 0, stop = 1 - 1 + 4 = 4
      expect(mockRedis.zrevrange).toHaveBeenCalledWith('leaderboard:2026-03', 0, 4, 'WITHSCORES');
    });

    it('returns empty array on Redis failure', async () => {
      mockRedis.zrevrange.mockRejectedValue(new Error('Redis down'));
      const result = await service.getPlayersAroundRank('2026-03', 10, 5, 4);
      expect(result).toEqual([]);
    });
  });

  describe('getPlayerRank', () => {
    it('returns 1-indexed rank for existing player', async () => {
      mockRedis.zrevrank.mockResolvedValue(0); // 0-indexed rank 0 = rank 1
      const result = await service.getPlayerRank('2026-03', 'p1');
      expect(result).toBe(1);
    });

    it('returns correct rank for non-first player', async () => {
      mockRedis.zrevrank.mockResolvedValue(9); // 0-indexed rank 9 = rank 10
      const result = await service.getPlayerRank('2026-03', 'p1');
      expect(result).toBe(10);
    });

    it('returns null when player not in leaderboard', async () => {
      mockRedis.zrevrank.mockResolvedValue(null);
      const result = await service.getPlayerRank('2026-03', 'nonexistent');
      expect(result).toBeNull();
    });

    it('returns null on Redis failure', async () => {
      mockRedis.zrevrank.mockRejectedValue(new Error('Redis down'));
      const result = await service.getPlayerRank('2026-03', 'p1');
      expect(result).toBeNull();
    });
  });
});
