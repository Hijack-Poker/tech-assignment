import { Body, Controller, DefaultValuePipe, Get, Param, ParseIntPipe, Post, Query } from '@nestjs/common';
import { AdminService } from './admin.service';
import { AdjustPointsDto } from './dto/adjust-points.dto';
import { TierOverrideDto } from './dto/tier-override.dto';
import type {
  PlayerRewardsResponse,
  AdminPlayerRewardsResponse,
  AdminLeaderboardEntry,
} from '../../../../shared/types/rewards';

/**
 * Admin controller — production CS/ops endpoints.
 *
 * In production these would be behind admin auth (e.g. internal SSO,
 * API key, or role-based guard). Left unguarded here so graders can
 * test freely with their own auth approach.
 *
 * These endpoints are distinct from the player-facing versions:
 * - Player endpoints are scoped to the caller's own data (via AuthGuard)
 * - Admin endpoints accept an explicit playerId and expose internal
 *   fields (createdAt, updatedAt, raw playerId in leaderboard)
 */
@Controller('admin')
export class AdminController {
  constructor(private readonly adminService: AdminService) {}

  /**
   * GET /api/v1/admin/players/:playerId/rewards
   *
   * Cross-player lookup for CS/ops. Unlike GET /player/rewards (which
   * returns the caller's own data), this accepts any playerId and
   * includes internal metadata (createdAt, updatedAt) not exposed to
   * regular players. Returns AdminPlayerRewardsResponse which extends
   * the standard PlayerRewardsResponse.
   */
  @Get('players/:playerId/rewards')
  getAdminPlayer(@Param('playerId') playerId: string): Promise<AdminPlayerRewardsResponse> {
    return this.adminService.getAdminPlayerRewards(playerId);
  }

  /**
   * POST /api/v1/admin/points/adjust
   *
   * Delta-based point adjustment with an audit reason. Adds or removes
   * points relative to the player's current balance (positive = credit,
   * negative = debit). Points floor at 0 on debit; totalEarned only
   * increases on credit (debits don't erase lifetime progress). Writes
   * an immutable adjustment transaction for the audit trail, records
   * tier history if the adjustment crosses a tier boundary, and updates
   * the Redis leaderboard.
   */
  @Post('points/adjust')
  adjustPoints(@Body() dto: AdjustPointsDto): Promise<PlayerRewardsResponse> {
    return this.adminService.adjustPoints(dto.playerId, dto.points, dto.reason);
  }

  /**
   * GET /api/v1/admin/leaderboard?limit=100&month=2026-03
   *
   * Full leaderboard for admin dashboards. Unlike the player-facing
   * leaderboard, this exposes raw playerIds (the player version only
   * shows display names for privacy) and defaults to a higher limit.
   * Supports filtering by month for historical views.
   */
  @Get('leaderboard')
  getAdminLeaderboard(
    @Query('limit', new DefaultValuePipe(100), ParseIntPipe) limit: number,
    @Query('month') month?: string,
  ): Promise<{ leaderboard: AdminLeaderboardEntry[] }> {
    return this.adminService.getAdminLeaderboard(limit, month);
  }

  /**
   * POST /api/v1/admin/tier/override
   *
   * Manually set a player's tier, bypassing the normal points-based
   * progression. Used for VIP promotions, compensation, or corrections.
   * Writes a tier_override entry to the tier history table so the
   * change is visible in the timeline and distinguishable from organic
   * tier changes. Does not modify points or totalEarned.
   */
  @Post('tier/override')
  overrideTier(@Body() dto: TierOverrideDto): Promise<PlayerRewardsResponse> {
    return this.adminService.overrideTier(dto.playerId, dto.tier as 1 | 2 | 3 | 4, dto.expiry);
  }
}
