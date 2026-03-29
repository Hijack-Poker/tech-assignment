import { Body, Controller, Get, Param, Put } from '@nestjs/common';
import { DevService } from './dev.service';
import { SetPointsDto } from './dto/set-points.dto';
import type { PlayerRewardsResponse } from '../../../../shared/types/rewards';

/**
 * Dev/demo-only controller — NOT behind AuthGuard.
 *
 * Provides direct point adjustment for testing the rewards UI.
 * Would be removed or gated behind feature flags in production.
 */
@Controller('dev')
export class DevController {
  constructor(private readonly devService: DevService) {}

  @Get('player/:playerId')
  getPlayer(@Param('playerId') playerId: string): Promise<PlayerRewardsResponse> {
    return this.devService.getPlayerRewards(playerId);
  }

  @Put('player/:playerId/points')
  setPoints(
    @Param('playerId') playerId: string,
    @Body() dto: SetPointsDto,
  ): Promise<PlayerRewardsResponse> {
    return this.devService.setPlayerPoints(playerId, dto.points, dto.totalEarned, dto.reason);
  }
}
