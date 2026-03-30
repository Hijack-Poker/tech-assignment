import { IsNotEmpty, IsNumber, IsPositive, IsString } from 'class-validator';

export class AwardPointsDto {
  @IsNumber()
  @IsPositive()
  tableId!: number;

  @IsString()
  @IsNotEmpty()
  tableStakes!: string;

  @IsNumber()
  @IsPositive()
  bigBlind!: number;

  @IsString()
  @IsNotEmpty()
  handId!: string;
}
