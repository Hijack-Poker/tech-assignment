import { Injectable, Logger } from '@nestjs/common';
import { ulid } from 'ulid';
import { DynamoService } from '../dynamo/dynamo.service';
import { RedisService } from '../redis/redis.service';
import { tierNumberToName } from '../config/constants';
import type { TierNumber, MonthlyResetResponse } from '../../../../shared/types/rewards';

@Injectable()
export class ResetService {
  private readonly logger = new Logger(ResetService.name);

  constructor(
    private readonly dynamo: DynamoService,
    private readonly redis: RedisService,
  ) {}

  /**
   * Run the monthly reset: snapshot tier history, apply tier floor protection,
   * reset monthly points to 0, and create downgrade notifications.
   */
  async runMonthlyReset(): Promise<MonthlyResetResponse> {
    const now = new Date();
    const nowIso = now.toISOString();

    // The month being closed out (previous month)
    const prevDate = new Date(now);
    prevDate.setMonth(prevDate.getMonth() - 1);
    const prevMonthKey = prevDate.toISOString().slice(0, 7);

    // The new month starting
    const newMonthKey = nowIso.slice(0, 7);

    const players = await this.dynamo.getAllPlayers();
    let downgrades = 0;

    for (const player of players) {
      // 1. Snapshot current state as previous month's tier history
      try {
        await this.dynamo.putTierHistory({
          playerId: player.playerId,
          monthKey: prevMonthKey,
          tier: player.tier,
          tierName: tierNumberToName(player.tier as TierNumber),
          points: player.points,
          totalEarned: player.totalEarned,
          reason: 'monthly_reset',
          createdAt: nowIso,
        });
      } catch (err) {
        this.logger.warn(`Failed to snapshot tier history for ${player.playerId}: ${(err as Error).message}`);
      }

      // 2. Apply tier floor protection: cannot drop more than one tier
      const floorTier = Math.max(1, player.tier - 1) as TierNumber;
      const newTier = floorTier;
      const tierDowngraded = newTier < player.tier;

      if (tierDowngraded) {
        downgrades++;

        // 3. Create downgrade notification
        try {
          const oldTierName = tierNumberToName(player.tier as TierNumber);
          const newTierName = tierNumberToName(newTier);
          await this.dynamo.addNotification({
            playerId: player.playerId,
            notificationId: ulid(),
            type: 'tier_downgrade',
            title: `Monthly Reset: Moved to ${newTierName}`,
            description: `Your tier was adjusted from ${oldTierName} to ${newTierName} as part of the monthly reset. Earn points to rank back up!`,
            dismissed: false,
            createdAt: nowIso,
          });
        } catch (err) {
          this.logger.warn(`Failed to create downgrade notification for ${player.playerId}: ${(err as Error).message}`);
        }
      }

      // 4. Reset monthly points and update tier
      await this.dynamo.updatePlayer(player.playerId, {
        points: 0,
        tier: newTier,
        updatedAt: nowIso,
      });

      // 5. Update Redis for new month
      this.redis.updateLeaderboard(newMonthKey, player.playerId, 0);
    }

    return {
      processed: players.length,
      downgrades,
      resetMonth: newMonthKey,
    };
  }
}
