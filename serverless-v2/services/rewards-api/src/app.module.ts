import { Module } from '@nestjs/common';
import { HealthModule } from './health/health.module';
import { PointsModule } from './points/points.module';
import { PlayerModule } from './player/player.module';

@Module({
  imports: [HealthModule, PointsModule, PlayerModule],
})
export class AppModule {}
