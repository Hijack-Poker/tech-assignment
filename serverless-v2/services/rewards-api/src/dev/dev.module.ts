import { Module } from '@nestjs/common';
import { DynamoModule } from '../dynamo/dynamo.module';
import { RedisModule } from '../redis/redis.module';
import { DevController } from './dev.controller';
import { DevService } from './dev.service';
import { ResetService } from './reset.service';

@Module({
  imports: [DynamoModule, RedisModule],
  controllers: [DevController],
  providers: [DevService, ResetService],
})
export class DevModule {}
