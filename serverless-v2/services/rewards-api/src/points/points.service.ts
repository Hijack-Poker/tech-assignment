import { Injectable, Logger, NotFoundException } from '@nestjs/common';
import { ulid } from 'ulid';
import { DynamoService } from '../dynamo/dynamo.service';
import { RedisService } from '../redis/redis.service';
import { AwardPointsDto } from './dto/award-points.dto';
import {
  getBasePointsForStakes,
  getTierForPoints,
  tierNumberToName,
  TIERS,
} from '../config/constants';
import type { AwardPointsResponse, LeaderboardEntry, LeaderboardResponse, TierNumber } from '../../../../shared/types/rewards';

@Injectable()
export class PointsService {
  private readonly logger = new Logger(PointsService.name);

  constructor(
    private readonly dynamo: DynamoService,
    private readonly redis: RedisService,
  ) {}

  async awardPoints(playerId: string, dto: AwardPointsDto): Promise<AwardPointsResponse> {
    const now = new Date().toISOString();
    const monthKey = now.slice(0, 7); // "YYYY-MM"

    // Step 1: Look up player
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    // Step 2: Calculate base points from big blind bracket
    const basePoints = getBasePointsForStakes(dto.bigBlind);

    // Step 3: Apply tier multiplier
    const tierDef = Object.values(TIERS).find((t) => t.number === player.tier);
    const multiplier = tierDef ? tierDef.multiplier : 1.0;
    const earnedPoints = Math.round(basePoints * multiplier);

    // Step 4: Compute new totals
    const newPoints = player.points + earnedPoints;
    const newTotalEarned = player.totalEarned + earnedPoints;

    // Step 5: Write immutable transaction record
    const timestamp = Date.now();
    await this.dynamo.addTransaction(playerId, {
      type: 'gameplay',
      basePoints,
      multiplier,
      earnedPoints,
      tableId: dto.tableId,
      tableStakes: dto.tableStakes,
      monthKey,
      createdAt: now,
      balanceAfter: newPoints,
    });

    const playerUpdates: Record<string, unknown> = {
      points: newPoints,
      totalEarned: newTotalEarned,
      updatedAt: now,
    };

    // Step 6: Check for tier upgrade
    const newTierDef = getTierForPoints(newPoints);
    const tierChanged = newTierDef.number > player.tier;

    if (tierChanged) {
      playerUpdates.tier = newTierDef.number;
    }

    await this.dynamo.updatePlayer(playerId, playerUpdates);

    // Step 7: Create notification on tier change
    if (tierChanged) {
      try {
        await this.dynamo.addNotification({
          playerId,
          notificationId: ulid(),
          type: 'tier_upgrade',
          title: `Upgraded to ${newTierDef.name}!`,
          description: `Congratulations! You've reached ${newTierDef.name} tier with ${newPoints} points. Enjoy your ${newTierDef.multiplier}x point multiplier!`,
          dismissed: false,
          createdAt: now,
        });
      } catch (err) {
        this.logger.warn(`Failed to create tier upgrade notification: ${(err as Error).message}`);
      }
    }

    // Step 8: Update Redis leaderboard (fire-and-forget)
    this.redis.updateLeaderboard(monthKey, playerId, newPoints);

    // Step 9: Return response
    const currentTierNumber = tierChanged ? newTierDef.number : player.tier;

    return {
      playerId,
      earnedPoints,
      newPoints,
      newTotalEarned,
      tier: tierNumberToName(currentTierNumber as TierNumber),
      transaction: {
        timestamp,
        type: 'gameplay',
        basePoints,
        multiplier,
        earnedPoints,
        tableId: dto.tableId,
        tableStakes: dto.tableStakes,
        balanceAfter: newPoints,
      },
    };
  }

  /**
   * Redis is the sole data source here — no DynamoDB fallback. Redis is a hard
   * dependency across the platform (holdem-processor, cash-game-broadcast, etc.),
   * so if it's down the entire game is down, not just the leaderboard. A separate
   * DynamoDB fallback table would add complexity for a scenario that never occurs
   * in isolation. If Redis data is ever lost, the rewards-players table contains
   * points and can be used to rebuild the sorted set.
   */
  async getLeaderboard(playerId: string, limit: number, month?: string, view?: string): Promise<LeaderboardResponse> {
    const monthKey = month || new Date().toISOString().slice(0, 7);
    const playerRank = await this.redis.getPlayerRank(monthKey, playerId) ?? undefined;

    let redisEntries: { playerId: string; score: number }[];
    let startRank: number;

    if (view === 'nearby' && playerRank) {
      redisEntries = await this.redis.getPlayersAroundRank(monthKey, playerRank, 5, 4);
      startRank = Math.max(1, playerRank - 5);
    } else {
      redisEntries = await this.redis.getTopPlayers(monthKey, limit);
      startRank = 1;
    }

    const leaderboard: LeaderboardEntry[] = redisEntries.map((entry, i) => ({
      rank: startRank + i,
      playerId: entry.playerId,
      displayName: entry.playerId,
      tier: getTierForPoints(entry.score).name,
      points: entry.score,
    }));

    return { leaderboard, playerRank };
  }
}
