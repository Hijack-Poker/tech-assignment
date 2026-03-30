import { Body, Controller, DefaultValuePipe, Get, ParseIntPipe, Post, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';
import { AwardPointsDto } from './dto/award-points.dto';
import { PointsService } from './points.service';
import type { AwardPointsResponse, LeaderboardResponse } from '../../../../shared/types/rewards';

/**
 * Points controller — core gameplay endpoints behind AuthGuard.
 *
 * These are called by the game processor (award) and the player-facing
 * frontend (leaderboard). The X-Player-Id header identifies the caller.
 */
@Controller('points')
@UseGuards(AuthGuard)
export class PointsController {
  constructor(private readonly pointsService: PointsService) {}

  /**
   * POST /api/v1/points/award
   *
   * Called by the game processor after each completed hand. Calculates
   * base points from the table's big-blind bracket, applies the player's
   * tier multiplier, writes an immutable transaction record, checks for
   * tier promotions/demotions and milestone triggers, and updates the
   * Redis leaderboard — all in a single request.
   */
  @Post('award')
  awardPoints(@PlayerId() playerId: string, @Body() dto: AwardPointsDto): Promise<AwardPointsResponse> {
    return this.pointsService.awardPoints(playerId, dto);
  }

  /**
   * GET /api/v1/points/leaderboard?limit=100&month=2026-03&view=nearby
   *
   * Player-scoped leaderboard. Returns display names and tiers but not
   * raw player IDs (privacy). Supports two views:
   *   - default: top N players by monthly points
   *   - view=nearby: 10 players surrounding the caller's rank
   *
   * Data source is Redis sorted sets, not DynamoDB — if Redis is lost
   * the leaderboard is empty but can be rebuilt from the players table.
   */
  @Get('leaderboard')
  getLeaderboard(
    @PlayerId() playerId: string,
    @Query('limit', new DefaultValuePipe(100), ParseIntPipe) limit: number,
    @Query('month') month?: string,
    @Query('view') view?: string,
  ): Promise<LeaderboardResponse> {
    return this.pointsService.getLeaderboard(playerId, limit, month, view);
  }
}
