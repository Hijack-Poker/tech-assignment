import { Injectable, Logger, NotFoundException } from '@nestjs/common';
import { DynamoService } from '../dynamo/dynamo.service';
import { RedisService } from '../redis/redis.service';
import { getTierForPoints, tierNumberToName, getNextTier } from '../config/constants';
import type {
  TierNumber,
  PlayerRewardsResponse,
  AdminPlayerRewardsResponse,
  AdminLeaderboardEntry,
  TransactionResponse,
} from '../../../../shared/types/rewards';

@Injectable()
export class AdminService {
  private readonly logger = new Logger(AdminService.name);

  constructor(
    private readonly dynamo: DynamoService,
    private readonly redis: RedisService,
  ) {}

  private async buildPlayerRewards(playerId: string): Promise<PlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const tierName = tierNumberToName(player.tier as TierNumber);
    const nextTier = getNextTier(tierName);
    const { items: transactions } = await this.dynamo.getTransactions(playerId, 10);

    const recentTransactions: TransactionResponse[] = transactions.map((t) => ({
      timestamp: t.timestamp,
      type: t.type,
      basePoints: t.basePoints,
      multiplier: t.multiplier,
      earnedPoints: t.earnedPoints,
      tableId: t.tableId,
      tableStakes: t.tableStakes,
      reason: t.reason,
      balanceAfter: t.balanceAfter,
    }));

    return {
      playerId,
      tier: tierName,
      points: player.points,
      totalEarned: player.totalEarned,
      handsPlayed: player.handsPlayed,
      nextTierAt: nextTier ? nextTier.minPoints : null,
      nextTierName: nextTier ? nextTier.name : null,
      recentTransactions,
    };
  }

  async getAdminPlayerRewards(playerId: string): Promise<AdminPlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const base = await this.buildPlayerRewards(playerId);
    return {
      ...base,
      createdAt: player.createdAt,
      updatedAt: player.updatedAt,
    };
  }

  async adjustPoints(playerId: string, delta: number, reason: string): Promise<PlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const now = new Date().toISOString();
    const monthKey = now.slice(0, 7);
    const newPoints = Math.max(0, player.points + delta);
    const newTotalEarned = delta > 0 ? player.totalEarned + delta : player.totalEarned;
    const newTierDef = getTierForPoints(newTotalEarned);

    // Write adjustment transaction
    await this.dynamo.addTransaction(playerId, {
      type: 'adjustment',
      basePoints: Math.abs(delta),
      multiplier: 1,
      earnedPoints: delta,
      monthKey,
      createdAt: now,
      reason,
      balanceAfter: newPoints,
    });

    // Record tier history if tier changed
    if (newTierDef.number !== player.tier) {
      try {
        await this.dynamo.putTierHistory({
          playerId,
          monthKey,
          tier: newTierDef.number,
          tierName: newTierDef.name,
          points: newPoints,
          totalEarned: newTotalEarned,
          reason: 'tier_change',
          createdAt: now,
        });
      } catch (err) {
        this.logger.warn(`Failed to record tier history: ${(err as Error).message}`);
      }
    }

    // Update player record
    await this.dynamo.updatePlayer(playerId, {
      points: newPoints,
      totalEarned: newTotalEarned,
      tier: newTierDef.number,
      updatedAt: now,
    });

    // Update Redis leaderboard
    this.redis.updateLeaderboard(monthKey, playerId, newPoints);

    return this.buildPlayerRewards(playerId);
  }

  async getAdminLeaderboard(limit: number, month?: string): Promise<{ leaderboard: AdminLeaderboardEntry[] }> {
    const monthKey = month || new Date().toISOString().slice(0, 7);
    const redisEntries = await this.redis.getTopPlayers(monthKey, limit);
    const players = await this.dynamo.getPlayers(redisEntries.map((e) => e.playerId));

    const leaderboard: AdminLeaderboardEntry[] = redisEntries.map((entry, i) => {
      const player = players.get(entry.playerId);
      return {
        rank: i + 1,
        playerId: entry.playerId,
        displayName: player?.username || entry.playerId,
        tier: player ? tierNumberToName(player.tier as TierNumber) : getTierForPoints(entry.score).name,
        points: entry.score,
      };
    });

    return { leaderboard };
  }

  async overrideTier(playerId: string, tier: TierNumber, expiry: string): Promise<PlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const now = new Date().toISOString();

    // Update player with overridden tier
    await this.dynamo.updatePlayer(playerId, {
      tier,
      updatedAt: now,
    });

    // Snapshot to tier history
    await this.dynamo.putTierHistory({
      playerId,
      monthKey: now.slice(0, 7),
      tier,
      tierName: tierNumberToName(tier),
      points: player.points,
      totalEarned: player.totalEarned,
      reason: 'tier_override',
      createdAt: now,
    });

    return this.buildPlayerRewards(playerId);
  }
}
