import { Controller, DefaultValuePipe, Get, Param, ParseBoolPipe, ParseIntPipe, Patch, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';
import { PlayerService } from './player.service';
import type { PlayerRewardsResponse, PlayerHistoryResponse, NotificationsResponse } from '../../../../shared/types/rewards';

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

  /**
   * GET /api/v1/player/notifications?unread=true
   *
   * Get a player's notifications (optionally unread only).
   */
  @Get('notifications')
  getNotifications(
    @PlayerId() playerId: string,
    @Query('unread', new DefaultValuePipe(false), ParseBoolPipe) unread: boolean,
  ): Promise<NotificationsResponse> {
    return this.playerService.getNotifications(playerId, unread);
  }

  /**
   * PATCH /api/v1/player/notifications/:id/dismiss
   *
   * Dismiss a notification.
   */
  @Patch('notifications/:id/dismiss')
  dismissNotification(
    @PlayerId() playerId: string,
    @Param('id') notificationId: string,
  ): Promise<{ success: true }> {
    return this.playerService.dismissNotification(playerId, notificationId);
  }
}
