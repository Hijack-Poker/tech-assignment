import { Injectable, NotFoundException } from '@nestjs/common';
import { DynamoService } from '../dynamo/dynamo.service';
import { tierNumberToName, getNextTier } from '../config/constants';
import type {
  TierNumber,
  PlayerRewardsResponse,
  TransactionResponse,
} from '../../../../shared/types/rewards';

@Injectable()
export class PlayerService {
  constructor(private readonly dynamo: DynamoService) {}

  async getRewards(playerId: string): Promise<PlayerRewardsResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const tierName = tierNumberToName(player.currentTier as TierNumber);
    const nextTier = getNextTier(tierName);
    const transactions = await this.dynamo.getTransactions(playerId, 10);

    const recentTransactions: TransactionResponse[] = transactions.map((t) => ({
      timestamp: t.timestamp,
      type: t.type,
      basePoints: t.basePoints,
      multiplier: t.multiplier,
      earnedPoints: t.earnedPoints,
      tableId: t.tableId,
      tableStakes: t.tableStakes,
      reason: t.reason,
    }));

    return {
      playerId,
      tier: tierName,
      monthlyPoints: player.monthlyPoints,
      lifetimePoints: player.lifetimePoints,
      nextTierAt: nextTier ? nextTier.minPoints : null,
      nextTierName: nextTier ? nextTier.name : null,
      recentTransactions,
    };
  }
}
