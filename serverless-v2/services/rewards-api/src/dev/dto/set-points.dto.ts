import { IsNumber, IsOptional, IsString } from 'class-validator';

export class SetPointsDto {
  @IsNumber()
  points!: number;

  @IsNumber()
  totalEarned!: number;

  @IsOptional()
  @IsString()
  reason?: string;
}
