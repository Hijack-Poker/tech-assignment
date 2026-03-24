import { Body, Controller, Get, HttpCode, HttpStatus, Post, UseGuards } from '@nestjs/common';
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
   * Get the points leaderboard. Candidates implement this.
   */
  @Get('leaderboard')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  getLeaderboard(): LeaderboardResponse {
    return {
      error: 'Not implemented',
      message: 'Implement leaderboard query here. See challenge docs for requirements.',
    } as unknown as LeaderboardResponse;
  }
}
