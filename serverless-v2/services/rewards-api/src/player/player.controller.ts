import { Controller, DefaultValuePipe, Get, ParseIntPipe, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';
import { PlayerService } from './player.service';
import type { PlayerRewardsResponse, PlayerHistoryResponse } from '../../../../shared/types/rewards';

@Controller('player')
@UseGuards(AuthGuard)
export class PlayerController {
  constructor(private readonly playerService: PlayerService) {}

  /**
   * GET /api/v1/player/rewards
   *
   * Get a player's rewards summary.
   */
  @Get('rewards')
  getRewards(@PlayerId() playerId: string): Promise<PlayerRewardsResponse> {
    return this.playerService.getRewards(playerId);
  }

  /**
   * GET /api/v1/player/history
   *
   * Get a player's point transaction history.
   */
  @Get('history')
  getHistory(
    @PlayerId() playerId: string,
    @Query('limit', new DefaultValuePipe(20), ParseIntPipe) limit: number,
    @Query('cursor') cursor?: string,
  ): Promise<PlayerHistoryResponse> {
    return this.playerService.getHistory(playerId, limit, cursor);
  }
}
