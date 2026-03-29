import { Module } from '@nestjs/common';
import { DynamoModule } from './dynamo/dynamo.module';
import { HealthModule } from './health/health.module';
import { PointsModule } from './points/points.module';
import { PlayerModule } from './player/player.module';
import { AdminModule } from './admin/admin.module';
import { DevModule } from './dev/dev.module';

@Module({
  imports: [DynamoModule, HealthModule, PointsModule, PlayerModule, AdminModule, DevModule],
})
export class AppModule {}
