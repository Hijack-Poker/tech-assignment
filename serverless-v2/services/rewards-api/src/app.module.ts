import { Module } from '@nestjs/common';
import { DynamoModule } from './dynamo/dynamo.module';
import { HealthModule } from './health/health.module';
import { PointsModule } from './points/points.module';
import { PlayerModule } from './player/player.module';

@Module({
  imports: [DynamoModule, HealthModule, PointsModule, PlayerModule],
})
export class AppModule {}
