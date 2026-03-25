import { Injectable } from '@nestjs/common';
import {
  PutCommand,
  GetCommand,
  QueryCommand,
  UpdateCommand,
  ScanCommand,
} from '@aws-sdk/lib-dynamodb';
import { PlayerRecord, TransactionRecord, NotificationRecord } from '../../../../shared/types/rewards';

/** Composite key for the rewards-transactions table. */
interface TransactionKey {
  playerId: string;
  timestamp: number;
}

// eslint-disable-next-line @typescript-eslint/no-var-requires
const { docClient } = require('../../../shared/config/dynamo');

const PLAYERS_TABLE = process.env.REWARDS_PLAYERS_TABLE || 'rewards-players';
const TRANSACTIONS_TABLE = process.env.REWARDS_TRANSACTIONS_TABLE || 'rewards-transactions';
const NOTIFICATIONS_TABLE = process.env.REWARDS_NOTIFICATIONS_TABLE || 'rewards-notifications';

@Injectable()
export class DynamoService {
  /**
   * Get a player's rewards profile.
   */
  async getPlayer(playerId: string): Promise<PlayerRecord | null> {
    const result = await docClient.send(
      new GetCommand({
        TableName: PLAYERS_TABLE,
        Key: { playerId },
      }),
    );
    return (result.Item as PlayerRecord) || null;
  }

  /**
   * Create or update a player's rewards profile.
   */
  async putPlayer(player: PlayerRecord): Promise<void> {
    await docClient.send(
      new PutCommand({
        TableName: PLAYERS_TABLE,
        Item: player,
      }),
    );
  }

  /**
   * Update specific attributes on a player record.
   */
  async updatePlayer(playerId: string, updates: Partial<Omit<PlayerRecord, 'playerId'>>): Promise<void> {
    const expressions: string[] = [];
    const names: Record<string, string> = {};
    const values: Record<string, unknown> = {};

    Object.entries(updates).forEach(([key, value], i) => {
      expressions.push(`#k${i} = :v${i}`);
      names[`#k${i}`] = key;
      values[`:v${i}`] = value;
    });

    await docClient.send(
      new UpdateCommand({
        TableName: PLAYERS_TABLE,
        Key: { playerId },
        UpdateExpression: `SET ${expressions.join(', ')}`,
        ExpressionAttributeNames: names,
        ExpressionAttributeValues: values,
      }),
    );
  }

  /**
   * Record a point transaction.
   */
  async addTransaction(playerId: string, transaction: Omit<TransactionRecord, 'playerId' | 'timestamp'>): Promise<void> {
    await docClient.send(
      new PutCommand({
        TableName: TRANSACTIONS_TABLE,
        Item: {
          playerId,
          timestamp: Date.now(),
          ...transaction,
        },
      }),
    );
  }

  /**
   * Get a player's transaction history with cursor-based pagination.
   */
  async getTransactions(
    playerId: string,
    limit = 20,
    cursor?: TransactionKey,
  ): Promise<{ items: TransactionRecord[]; lastKey: TransactionKey | undefined }> {
    const result = await docClient.send(
      new QueryCommand({
        TableName: TRANSACTIONS_TABLE,
        KeyConditionExpression: 'playerId = :pid',
        ExpressionAttributeValues: { ':pid': playerId },
        ScanIndexForward: false,
        Limit: limit,
        ...(cursor && { ExclusiveStartKey: cursor }),
      }),
    );
    return {
      items: (result.Items as TransactionRecord[]) || [],
      lastKey: result.LastEvaluatedKey as TransactionKey | undefined,
    };
  }

  /**
   * Count a player's total transactions.
   */
  async countTransactions(playerId: string): Promise<number> {
    const result = await docClient.send(
      new QueryCommand({
        TableName: TRANSACTIONS_TABLE,
        KeyConditionExpression: 'playerId = :pid',
        ExpressionAttributeValues: { ':pid': playerId },
        Select: 'COUNT',
      }),
    );
    return result.Count || 0;
  }

  /**
   * Get all players (for leaderboard).
   */
  async getAllPlayers(): Promise<PlayerRecord[]> {
    const result = await docClient.send(
      new ScanCommand({ TableName: PLAYERS_TABLE }),
    );
    return (result.Items as PlayerRecord[]) || [];
  }

  /**
   * Write a notification record.
   */
  async addNotification(notification: NotificationRecord): Promise<void> {
    await docClient.send(
      new PutCommand({
        TableName: NOTIFICATIONS_TABLE,
        Item: notification,
      }),
    );
  }
}
