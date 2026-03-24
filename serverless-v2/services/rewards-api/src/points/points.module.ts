import { Module } from '@nestjs/common';
import { DynamoModule } from '../dynamo/dynamo.module';
import { RedisModule } from '../redis/redis.module';
import { PointsController } from './points.controller';
import { PointsService } from './points.service';

@Module({
  imports: [DynamoModule, RedisModule],
  controllers: [PointsController],
  providers: [PointsService],
})
export class PointsModule {}
