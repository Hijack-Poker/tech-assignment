import { Injectable, Logger, NotFoundException } from '@nestjs/common';
import { DynamoService } from '../dynamo/dynamo.service';
import { RedisService } from '../redis/redis.service';
import { getTierForPoints, tierNumberToName, getNextTier } from '../config/constants';
import type {
  TierNumber,
  PlayerRewardsResponse,
  TierTimelineResponse,
  TransactionResponse,
} from '../../../../shared/types/rewards';

@Injectable()
export class DevService {
  private readonly logger = new Logger(DevService.name);

  constructor(
    private readonly dynamo: DynamoService,
    private readonly redis: RedisService,
  ) {}

  async getPlayerRewards(playerId: string): Promise<PlayerRewardsResponse> {
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

  async setPlayerPoints(
    playerId: string,
    points: number,
    totalEarned: number,
    reason = 'dev_adjustment',
  ): Promise<PlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const now = new Date().toISOString();
    const monthKey = now.slice(0, 7);
    const newTierDef = getTierForPoints(totalEarned);
    const delta = points - player.points;

    // Write adjustment transaction
    await this.dynamo.addTransaction(playerId, {
      type: 'adjustment',
      basePoints: Math.abs(delta),
      multiplier: 1,
      earnedPoints: delta,
      monthKey,
      createdAt: now,
      reason,
      balanceAfter: points,
    });

    // Record tier history if tier changed
    if (newTierDef.number !== player.tier) {
      try {
        await this.dynamo.putTierHistory({
          playerId,
          monthKey,
          tier: newTierDef.number,
          tierName: newTierDef.name,
          points,
          totalEarned,
          reason: 'tier_change',
          createdAt: now,
        });
      } catch (err) {
        this.logger.warn(`Failed to record tier history: ${(err as Error).message}`);
      }
    }

    // Update player record
    await this.dynamo.updatePlayer(playerId, {
      points,
      totalEarned,
      tier: newTierDef.number,
      updatedAt: now,
    });

    // Update Redis leaderboard
    this.redis.updateLeaderboard(monthKey, playerId, points);

    return this.getPlayerRewards(playerId);
  }

  async getTierHistory(playerId: string): Promise<TierTimelineResponse> {
    const records = await this.dynamo.getTierHistory(playerId, 6);

    return {
      playerId,
      history: records.map((r) => ({
        monthKey: r.monthKey,
        tier: r.tierName,
        points: r.points,
        totalEarned: r.totalEarned,
        reason: r.reason,
        createdAt: r.createdAt,
      })),
    };
  }
}
