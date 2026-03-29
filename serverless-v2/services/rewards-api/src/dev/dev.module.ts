import { Module } from '@nestjs/common';
import { DynamoModule } from '../dynamo/dynamo.module';
import { RedisModule } from '../redis/redis.module';
import { DevController } from './dev.controller';
import { DevService } from './dev.service';

@Module({
  imports: [DynamoModule, RedisModule],
  controllers: [DevController],
  providers: [DevService],
})
export class DevModule {}
