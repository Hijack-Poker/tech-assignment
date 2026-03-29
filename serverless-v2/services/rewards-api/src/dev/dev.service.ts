import { Injectable, NotFoundException } from '@nestjs/common';
import { DynamoService } from '../dynamo/dynamo.service';
import { RedisService } from '../redis/redis.service';
import { getTierForPoints, tierNumberToName, getNextTier } from '../config/constants';
import type {
  TierNumber,
  PlayerRewardsResponse,
  TransactionResponse,
} from '../../../../shared/types/rewards';

@Injectable()
export class DevService {
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
}
