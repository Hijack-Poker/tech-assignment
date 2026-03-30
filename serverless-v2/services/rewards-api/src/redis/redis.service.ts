import { Injectable, Logger, OnModuleInit } from '@nestjs/common';

// eslint-disable-next-line @typescript-eslint/no-var-requires
const { redisClient } = require('../../../shared/config/redis');

@Injectable()
export class RedisService implements OnModuleInit {
  private readonly logger = new Logger(RedisService.name);

  async onModuleInit(): Promise<void> {
    try {
      await redisClient.connect();
      this.logger.log('Redis connected');
    } catch (err) {
      this.logger.warn(`Redis connection failed: ${(err as Error).message} — leaderboard updates will be skipped`);
    }
  }

  async updateLeaderboard(monthKey: string, playerId: string, score: number): Promise<void> {
    try {
      await redisClient.zadd(`leaderboard:${monthKey}`, score, playerId);
    } catch (err) {
      this.logger.warn(`Failed to update leaderboard: ${(err as Error).message}`);
    }
  }

  async getTopPlayers(monthKey: string, count: number): Promise<{ playerId: string; score: number }[]> {
    try {
      const results: string[] = await redisClient.zrevrange(`leaderboard:${monthKey}`, 0, count - 1, 'WITHSCORES');
      const entries: { playerId: string; score: number }[] = [];
      for (let i = 0; i < results.length; i += 2) {
        entries.push({ playerId: results[i], score: Number(results[i + 1]) });
      }
      return entries;
    } catch (err) {
      this.logger.warn(`Failed to get leaderboard: ${(err as Error).message}`);
      return [];
    }
  }

  async getPlayersAroundRank(monthKey: string, rank: number, above: number, below: number): Promise<{ playerId: string; score: number }[]> {
    try {
      const start = Math.max(0, rank - 1 - above);
      const stop = rank - 1 + below;
      const results: string[] = await redisClient.zrevrange(`leaderboard:${monthKey}`, start, stop, 'WITHSCORES');
      const entries: { playerId: string; score: number }[] = [];
      for (let i = 0; i < results.length; i += 2) {
        entries.push({ playerId: results[i], score: Number(results[i + 1]) });
      }
      return entries;
    } catch (err) {
      this.logger.warn(`Failed to get nearby players: ${(err as Error).message}`);
      return [];
    }
  }

  async getPlayerRank(monthKey: string, playerId: string): Promise<number | null> {
    try {
      const rank: number | null = await redisClient.zrevrank(`leaderboard:${monthKey}`, playerId);
      return rank !== null ? rank + 1 : null;
    } catch (err) {
      this.logger.warn(`Failed to get player rank: ${(err as Error).message}`);
      return null;
    }
  }
}
