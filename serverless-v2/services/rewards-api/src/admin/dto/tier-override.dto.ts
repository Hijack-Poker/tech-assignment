import { IsNumber, IsString, Min, Max } from 'class-validator';

export class TierOverrideDto {
  @IsString()
  playerId!: string;

  @IsNumber()
  @Min(1)
  @Max(4)
  tier!: number;

  @IsString()
  expiry!: string;
}
