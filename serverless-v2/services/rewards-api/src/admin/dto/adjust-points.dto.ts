import { IsNumber, IsString } from 'class-validator';

export class AdjustPointsDto {
  @IsString()
  playerId!: string;

  @IsNumber()
  points!: number;

  @IsString()
  reason!: string;
}
