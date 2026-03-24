import { Module } from '@nestjs/common';
import { DynamoModule } from '../dynamo/dynamo.module';
import { PlayerController } from './player.controller';
import { PlayerService } from './player.service';

@Module({
  imports: [DynamoModule],
  controllers: [PlayerController],
  providers: [PlayerService],
})
export class PlayerModule {}
