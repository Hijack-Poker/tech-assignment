import { Body, Controller, DefaultValuePipe, Get, ParseIntPipe, Post, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';
import { AwardPointsDto } from './dto/award-points.dto';
import { PointsService } from './points.service';
import type { AwardPointsResponse, LeaderboardResponse } from '../../../../shared/types/rewards';

@Controller('points')
@UseGuards(AuthGuard)
export class PointsController {
  constructor(private readonly pointsService: PointsService) {}

  /**
   * POST /api/v1/points/award
   *
   * Award points to a player after a completed hand.
   */
  @Post('award')
  awardPoints(@PlayerId() playerId: string, @Body() dto: AwardPointsDto): Promise<AwardPointsResponse> {
    return this.pointsService.awardPoints(playerId, dto);
  }

  /**
   * GET /api/v1/points/leaderboard
   *
   * Get the points leaderboard.
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
