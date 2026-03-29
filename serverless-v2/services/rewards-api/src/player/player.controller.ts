import { Controller, DefaultValuePipe, Get, Param, ParseBoolPipe, ParseIntPipe, Patch, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard';
import { PlayerId } from '../auth/player-id.decorator';
import { PlayerService } from './player.service';
import type { PlayerRewardsResponse, PlayerHistoryResponse, NotificationsResponse } from '../../../../shared/types/rewards';

/**
 * Player controller — authenticated, player-scoped endpoints.
 *
 * Every endpoint here operates on the calling player's own data,
 * identified by the X-Player-Id header validated by AuthGuard.
 * Players cannot view or modify other players' data through this
 * controller — that's what the admin controller is for.
 */
@Controller('player')
@UseGuards(AuthGuard)
export class PlayerController {
  constructor(private readonly playerService: PlayerService) {}

  /**
   * GET /api/v1/player/rewards
   *
   * Returns the calling player's rewards summary: current tier, points
   * balance, total lifetime earned, hands played, progress toward the
   * next tier, and the 10 most recent transactions. This is the primary
   * data source for the player card UI.
   */
  @Get('rewards')
  getRewards(@PlayerId() playerId: string): Promise<PlayerRewardsResponse> {
    return this.playerService.getRewards(playerId);
  }

  /**
   * GET /api/v1/player/history?limit=20&cursor=...
   *
   * Paginated transaction history for the calling player. Returns
   * gameplay, adjustment, and bonus transactions in reverse-chronological
   * order. Cursor-based pagination via DynamoDB's LastEvaluatedKey.
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
   * Returns the calling player's notifications with an unread count.
   * Notifications are created automatically on tier upgrades, tier
   * downgrades, and milestone achievements (e.g. first hand, 100 hands).
   * Sorted newest-first via ULID sort keys.
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
   * Marks a single notification as dismissed. Uses a DynamoDB
   * ConditionExpression to verify the notification exists — returns
   * 404 if not found (e.g. already deleted or wrong player).
   */
  @Patch('notifications/:id/dismiss')
  dismissNotification(
    @PlayerId() playerId: string,
    @Param('id') notificationId: string,
  ): Promise<{ success: true }> {
    return this.playerService.dismissNotification(playerId, notificationId);
  }
}
