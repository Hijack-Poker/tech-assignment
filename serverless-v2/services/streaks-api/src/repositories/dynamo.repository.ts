'use strict';

import {
  PutCommand,
  GetCommand,
  QueryCommand,
  UpdateCommand,
  ScanCommand,
  BatchWriteCommand,
} from '@aws-sdk/lib-dynamodb';
import type { PlayerStreak, DailyActivity, StreakReward, FreezeHistory } from '../models/streak.model';

// eslint-disable-next-line @typescript-eslint/no-var-requires
const { docClient } = require('../../shared/config/dynamo');

// Table names from environment or defaults
const PLAYERS_TABLE = process.env.STREAKS_PLAYERS_TABLE || 'streaks-players';
const ACTIVITY_TABLE = process.env.STREAKS_ACTIVITY_TABLE || 'streaks-activity';
const REWARDS_TABLE = process.env.STREAKS_REWARDS_TABLE || 'streaks-rewards';
const FREEZE_HISTORY_TABLE = process.env.STREAKS_FREEZE_HISTORY_TABLE || 'streaks-freeze-history';

// ---------------------------------------------------------------------------
// streaks-players
// ---------------------------------------------------------------------------

/**
 * Get a player's streak profile by playerId.
 */
export async function getPlayerStreak(playerId: string): Promise<PlayerStreak | null> {
  const result = await docClient.send(
    new GetCommand({
      TableName: PLAYERS_TABLE,
      Key: { playerId },
    })
  );
  return (result.Item as PlayerStreak) || null;
}

/**
 * Create or overwrite a player's streak profile.
 */
export async function putPlayerStreak(player: PlayerStreak): Promise<void> {
  await docClient.send(
    new PutCommand({
      TableName: PLAYERS_TABLE,
      Item: player,
    })
  );
}

/**
 * Partially update a player's streak record.
 */
export async function updatePlayerStreak(
  playerId: string,
  updates: Partial<Omit<PlayerStreak, 'playerId'>>
): Promise<void> {
  const entries = Object.entries(updates);
  if (entries.length === 0) return;

  const expressions: string[] = [];
  const names: Record<string, string> = {};
  const values: Record<string, unknown> = {};

  entries.forEach(([key, value], i) => {
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
    })
  );
}

/**
 * Get all players (for leaderboard). Use sparingly — scans entire table.
 */
export async function getAllPlayerStreaks(): Promise<PlayerStreak[]> {
  const result = await docClient.send(
    new ScanCommand({ TableName: PLAYERS_TABLE })
  );
  return (result.Items as PlayerStreak[]) || [];
}

// ---------------------------------------------------------------------------
// streaks-activity
// ---------------------------------------------------------------------------

/**
 * Write a daily activity record.
 */
export async function putActivity(activity: DailyActivity): Promise<void> {
  await docClient.send(
    new PutCommand({
      TableName: ACTIVITY_TABLE,
      Item: activity,
    })
  );
}

/**
 * Get a single day's activity for a player.
 */
export async function getActivity(playerId: string, date: string): Promise<DailyActivity | null> {
  const result = await docClient.send(
    new GetCommand({
      TableName: ACTIVITY_TABLE,
      Key: { playerId, date },
    })
  );
  return (result.Item as DailyActivity) || null;
}

/**
 * Query daily activity for a player within a date range (inclusive), ordered by date.
 */
export async function queryActivityByDateRange(
  playerId: string,
  startDate: string,
  endDate: string
): Promise<DailyActivity[]> {
  const result = await docClient.send(
    new QueryCommand({
      TableName: ACTIVITY_TABLE,
      KeyConditionExpression: 'playerId = :pid AND #d BETWEEN :start AND :end',
      ExpressionAttributeNames: { '#d': 'date' },
      ExpressionAttributeValues: {
        ':pid': playerId,
        ':start': startDate,
        ':end': endDate,
      },
      ScanIndexForward: true,
    })
  );
  return (result.Items as DailyActivity[]) || [];
}

// ---------------------------------------------------------------------------
// streaks-rewards
// ---------------------------------------------------------------------------

/**
 * Write a streak reward record.
 */
export async function putReward(reward: StreakReward): Promise<void> {
  await docClient.send(
    new PutCommand({
      TableName: REWARDS_TABLE,
      Item: reward,
    })
  );
}

/**
 * Query all rewards for a player, sorted by createdAt descending.
 *
 * Since the SK is rewardId (ULID), and ULIDs are time-ordered, querying
 * with ScanIndexForward: false returns newest rewards first.
 */
export async function queryRewards(playerId: string): Promise<StreakReward[]> {
  const result = await docClient.send(
    new QueryCommand({
      TableName: REWARDS_TABLE,
      KeyConditionExpression: 'playerId = :pid',
      ExpressionAttributeValues: { ':pid': playerId },
      ScanIndexForward: false,
    })
  );
  return (result.Items as StreakReward[]) || [];
}

// ---------------------------------------------------------------------------
// streaks-freeze-history
// ---------------------------------------------------------------------------

/**
 * Write a freeze history record.
 */
export async function putFreezeHistory(freeze: FreezeHistory): Promise<void> {
  await docClient.send(
    new PutCommand({
      TableName: FREEZE_HISTORY_TABLE,
      Item: freeze,
    })
  );
}

/**
 * Query all freeze history records for a player.
 */
export async function queryFreezeHistory(playerId: string): Promise<FreezeHistory[]> {
  const result = await docClient.send(
    new QueryCommand({
      TableName: FREEZE_HISTORY_TABLE,
      KeyConditionExpression: 'playerId = :pid',
      ExpressionAttributeValues: { ':pid': playerId },
      ScanIndexForward: true,
    })
  );
  return (result.Items as FreezeHistory[]) || [];
}

// ---------------------------------------------------------------------------
// Batch helpers
// ---------------------------------------------------------------------------

/**
 * Batch write up to 25 activity records at once.
 */
export async function batchPutActivities(activities: DailyActivity[]): Promise<void> {
  const chunks = chunkArray(activities, 25);
  for (const chunk of chunks) {
    await docClient.send(
      new BatchWriteCommand({
        RequestItems: {
          [ACTIVITY_TABLE]: chunk.map((item) => ({
            PutRequest: { Item: item },
          })),
        },
      })
    );
  }
}

/**
 * Batch write up to 25 reward records at once.
 */
export async function batchPutRewards(rewards: StreakReward[]): Promise<void> {
  const chunks = chunkArray(rewards, 25);
  for (const chunk of chunks) {
    await docClient.send(
      new BatchWriteCommand({
        RequestItems: {
          [REWARDS_TABLE]: chunk.map((item) => ({
            PutRequest: { Item: item },
          })),
        },
      })
    );
  }
}

function chunkArray<T>(arr: T[], size: number): T[][] {
  const result: T[][] = [];
  for (let i = 0; i < arr.length; i += size) {
    result.push(arr.slice(i, i + size));
  }
  return result;
}
