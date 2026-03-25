import { Injectable, NotFoundException } from '@nestjs/common';
import { DynamoService } from '../dynamo/dynamo.service';
import { tierNumberToName, getNextTier } from '../config/constants';
import type {
  TierNumber,
  PlayerRewardsResponse,
  PlayerHistoryResponse,
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

    const tierName = tierNumberToName(player.tier as TierNumber);
    const nextTier = getNextTier(tierName);
    const { items: transactions } = await this.dynamo.getTransactions(playerId, 10);

    const recentTransactions: TransactionResponse[] = transactions.map((t) => ({
      timestamp: t.timestamp,
      type: t.type,
      basePoints: t.basePoints,
      multiplier: t.multiplier,
      earnedPoints: t.earnedPoints,
      tableId: t.tableId,
      tableStakes: t.tableStakes,
      reason: t.reason,
      balanceAfter: t.balanceAfter,
    }));

    return {
      playerId,
      tier: tierName,
      points: player.points,
      totalEarned: player.totalEarned,
      nextTierAt: nextTier ? nextTier.minPoints : null,
      nextTierName: nextTier ? nextTier.name : null,
      recentTransactions,
    };
  }

  async getHistory(playerId: string, limit: number, cursor?: string): Promise<PlayerHistoryResponse> {
    const player = await this.dynamo.getPlayer(playerId);
    if (!player) {
      throw new NotFoundException({ error: 'Not found', message: `Player ${playerId} not found` });
    }

    const decodedCursor = cursor
      ? JSON.parse(Buffer.from(cursor, 'base64url').toString())
      : undefined;

    const [{ items: transactions, lastKey }, total] = await Promise.all([
      this.dynamo.getTransactions(playerId, limit, decodedCursor),
      this.dynamo.countTransactions(playerId),
    ]);

    return {
      transactions: transactions.map((t) => ({
        timestamp: t.timestamp,
        type: t.type,
        basePoints: t.basePoints,
        multiplier: t.multiplier,
        earnedPoints: t.earnedPoints,
        tableId: t.tableId,
        tableStakes: t.tableStakes,
        reason: t.reason,
        balanceAfter: t.balanceAfter,
      })),
      total,
      limit,
      cursor: lastKey ? Buffer.from(JSON.stringify(lastKey)).toString('base64url') : null,
    };
  }
}
