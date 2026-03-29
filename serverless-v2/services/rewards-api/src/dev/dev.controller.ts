import { Body, Controller, Get, Param, Post, Put } from '@nestjs/common';
import { DevService } from './dev.service';
import { ResetService } from './reset.service';
import { SetPointsDto } from './dto/set-points.dto';
import type {
  PlayerRewardsResponse,
  TierTimelineResponse,
  MonthlyResetResponse,
} from '../../../../shared/types/rewards';

/**
 * Dev/demo-only controller — NOT behind AuthGuard.
 *
 * Provides raw point manipulation, monthly reset simulation, and tier
 * timeline visualization for testing the rewards system end-to-end.
 * Would be removed or gated behind a feature flag in production.
 *
 * Separated from AdminController because these endpoints have no
 * production equivalent — they exist purely to exercise and demonstrate
 * the system without needing a real game processor or scheduler.
 */
@Controller('dev')
export class DevController {
  constructor(
    private readonly devService: DevService,
    private readonly resetService: ResetService,
  ) {}

  /**
   * GET /api/v1/dev/player/:playerId
   *
   * Unauthenticated player lookup. Same response shape as the
   * authenticated GET /player/rewards but accepts any playerId without
   * requiring the X-Player-Id header. Used by the dev UI (e.g. the
   * AdjustPointsModal) to fetch player state before editing.
   */
  @Get('player/:playerId')
  getPlayer(@Param('playerId') playerId: string): Promise<PlayerRewardsResponse> {
    return this.devService.getPlayerRewards(playerId);
  }

  /**
   * PUT /api/v1/dev/player/:playerId/points
   *
   * Raw point setter — sets exact values for points and totalEarned,
   * bypassing delta logic. Unlike the admin adjust endpoint (which
   * applies a relative delta), this overwrites absolute values. Useful
   * for testing specific tier boundaries or resetting a player to a
   * known state. Writes an adjustment transaction, records tier history
   * if the tier changes, and updates the Redis leaderboard.
   */
  @Put('player/:playerId/points')
  setPoints(
    @Param('playerId') playerId: string,
    @Body() dto: SetPointsDto,
  ): Promise<PlayerRewardsResponse> {
    return this.devService.setPlayerPoints(playerId, dto.points, dto.totalEarned, dto.reason);
  }

  /**
   * POST /api/v1/dev/monthly-reset
   *
   * Triggers the monthly reset cycle on demand. In production this would
   * run on a scheduler (e.g. EventBridge cron on the 1st of each month).
   * The reset: snapshots each player's current state to tier history,
   * applies tier floor protection (max 1 tier drop), creates downgrade
   * notifications, resets monthly points to 0, and initializes a fresh
   * Redis leaderboard for the new month.
   */
  @Post('monthly-reset')
  runMonthlyReset(): Promise<MonthlyResetResponse> {
    return this.resetService.runMonthlyReset();
  }

  /**
   * GET /api/v1/dev/tier-history/:playerId
   *
   * Returns a player's tier history for the last 6 months, used to
   * render the vertical tier timeline in the PlayerCard. Entries are
   * created on monthly resets, tier overrides, and organic tier changes
   * from gameplay — so mid-month promotions appear as distinct events.
   * The frontend appends a live "Now" entry from current player state.
   */
  @Get('tier-history/:playerId')
  getTierHistory(@Param('playerId') playerId: string): Promise<TierTimelineResponse> {
    return this.devService.getTierHistory(playerId);
  }
}
