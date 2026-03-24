import { Controller, Get, HttpCode, HttpStatus, UseGuards } from '@nestjs/common';
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
   * Get a player's point transaction history. Candidates implement this.
   */
  @Get('history')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  getHistory(): PlayerHistoryResponse {
    return {
      error: 'Not implemented',
      message: 'Implement transaction history here. See challenge docs for requirements.',
    } as unknown as PlayerHistoryResponse;
  }
}
