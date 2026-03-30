import { Injectable } from '@nestjs/common';
import {
  PutCommand,
  GetCommand,
  BatchGetCommand,
  QueryCommand,
  UpdateCommand,
  ScanCommand,
} from '@aws-sdk/lib-dynamodb';
import { PlayerRecord, TransactionRecord, NotificationRecord, TierHistoryRecord } from '../../../../shared/types/rewards';

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
const TIER_HISTORY_TABLE = process.env.REWARDS_TIER_HISTORY_TABLE || 'rewards-tier-history';

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
   * Batch-get multiple players by ID. Returns a map of playerId → PlayerRecord.
   * DynamoDB limits BatchGetItem to 100 keys per request.
   */
  async getPlayers(playerIds: string[]): Promise<Map<string, PlayerRecord>> {
    const map = new Map<string, PlayerRecord>();
    if (playerIds.length === 0) return map;

    const result = await docClient.send(
      new BatchGetCommand({
        RequestItems: {
          [PLAYERS_TABLE]: {
            Keys: playerIds.map((id) => ({ playerId: id })),
          },
        },
      }),
    );

    const items = result.Responses?.[PLAYERS_TABLE] as PlayerRecord[] | undefined;
    if (items) {
      for (const item of items) {
        map.set(item.playerId, item);
      }
    }
    return map;
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

  /**
   * Get notifications for a player, newest first (ULID sort key).
   * Optionally filter to only undismissed notifications.
   */
  async getNotifications(playerId: string, unreadOnly = false): Promise<NotificationRecord[]> {
    const values: Record<string, unknown> = { ':pid': playerId };
    if (unreadOnly) {
      values[':false'] = false;
    }

    const result = await docClient.send(
      new QueryCommand({
        TableName: NOTIFICATIONS_TABLE,
        KeyConditionExpression: 'playerId = :pid',
        ExpressionAttributeValues: values,
        ScanIndexForward: false,
        ...(unreadOnly && { FilterExpression: 'dismissed = :false' }),
      }),
    );
    return (result.Items as NotificationRecord[]) || [];
  }

  /**
   * Mark a notification as dismissed.
   * Throws if the notification does not exist.
   */
  async dismissNotification(playerId: string, notificationId: string): Promise<void> {
    await docClient.send(
      new UpdateCommand({
        TableName: NOTIFICATIONS_TABLE,
        Key: { playerId, notificationId },
        UpdateExpression: 'SET dismissed = :true',
        ExpressionAttributeValues: { ':true': true },
        ConditionExpression: 'attribute_exists(playerId)',
      }),
    );
  }

  /**
   * Write a tier history snapshot.
   */
  async putTierHistory(record: TierHistoryRecord): Promise<void> {
    await docClient.send(
      new PutCommand({
        TableName: TIER_HISTORY_TABLE,
        Item: record,
      }),
    );
  }

  /**
   * Get tier history for a player (last N months).
   */
  async getTierHistory(playerId: string, months = 6): Promise<TierHistoryRecord[]> {
    const fromDate = new Date();
    fromDate.setMonth(fromDate.getMonth() - months);
    const fromKey = fromDate.toISOString();

    const result = await docClient.send(
      new QueryCommand({
        TableName: TIER_HISTORY_TABLE,
        KeyConditionExpression: 'playerId = :pid AND createdAt >= :from',
        ExpressionAttributeValues: { ':pid': playerId, ':from': fromKey },
        ScanIndexForward: true,
      }),
    );
    return (result.Items as TierHistoryRecord[]) || [];
  }
}
