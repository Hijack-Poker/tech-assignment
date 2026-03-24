import { Controller, Get, HttpCode, HttpStatus, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';

@Controller('player')
@UseGuards(AuthGuard)
export class PlayerController {
  /**
   * GET /api/v1/player/rewards
   *
   * Get a player's rewards summary. Candidates implement this.
   *
   * Uses playerId from auth guard (X-Player-Id header).
   *
   * Expected response:
   *   { playerId, tier, points, nextTierAt, recentTransactions: [...] }
   */
  @Get('rewards')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  getRewards(@PlayerId() playerId: string) {
    return {
      error: 'Not implemented',
      message: 'Implement player rewards lookup here. See challenge docs for requirements.',
      hint: {
        playerId,
        output: {
          playerId: 'string',
          tier: 'string',
          points: 'number',
          nextTierAt: 'number',
          recentTransactions: 'array',
        },
      },
    };
  }

  /**
   * GET /api/v1/player/history
   *
   * Get a player's point transaction history. Candidates implement this.
   *
   * Expected query: ?limit=20&offset=0
   * Expected response:
   *   { transactions: [{ timestamp, points, reason, balance }], total }
   */
  @Get('history')
  @HttpCode(HttpStatus.NOT_IMPLEMENTED)
  getHistory() {
    return {
      error: 'Not implemented',
      message: 'Implement transaction history here. See challenge docs for requirements.',
    };
  }
}
