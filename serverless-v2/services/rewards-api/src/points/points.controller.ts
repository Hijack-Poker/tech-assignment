import { Controller, Get, HttpCode, HttpStatus, Post, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';

@Controller('points')
@UseGuards(AuthGuard)
export class PointsController {
  /**
   * POST /api/v1/points/award
   *
   * Award points to a player. Candidates implement this.
   *
   * Expected body:
   *   { playerId: string, points: number, reason: string }
   *
   * Expected response:
   *   { playerId, newBalance, tier, transaction }
   */
  @Post('award')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  awardPoints() {
    return {
      error: 'Not implemented',
      message: 'Implement point awarding logic here. See challenge docs for requirements.',
      hint: {
        input: { playerId: 'string', points: 'number', reason: 'string' },
        output: { playerId: 'string', newBalance: 'number', tier: 'string', transaction: 'object' },
      },
    };
  }

  /**
   * GET /api/v1/points/leaderboard
   *
   * Get the points leaderboard. Candidates implement this.
   *
   * Expected response:
   *   { leaderboard: [{ playerId, username, points, tier, rank }] }
   */
  @Get('leaderboard')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  getLeaderboard() {
    return {
      error: 'Not implemented',
      message: 'Implement leaderboard query here. See challenge docs for requirements.',
    };
  }
}
