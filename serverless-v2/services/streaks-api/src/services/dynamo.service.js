'use strict';

const { PutCommand, GetCommand, QueryCommand, UpdateCommand, ScanCommand } = require('@aws-sdk/lib-dynamodb');
const { docClient } = require('../../shared/config/dynamo');

const PLAYERS_TABLE = process.env.STREAKS_PLAYERS_TABLE || 'streaks-players';
const ACTIVITY_TABLE = process.env.STREAKS_ACTIVITY_TABLE || 'streaks-activity';
const REWARDS_TABLE = process.env.STREAKS_REWARDS_TABLE || 'streaks-rewards';
const FREEZE_HISTORY_TABLE = process.env.STREAKS_FREEZE_HISTORY_TABLE || 'streaks-freeze-history';
const MISSIONS_TABLE = process.env.STREAKS_MISSIONS_TABLE || 'streaks-missions';

/**
 * Get a player's streak profile.
 */
async function getPlayer(playerId) {
  const result = await docClient.send(
    new GetCommand({
      TableName: PLAYERS_TABLE,
      Key: { playerId },
    })
  );
  return result.Item || null;
}

/**
 * Create or update a player's streak profile.
 */
async function putPlayer(player) {
  await docClient.send(
    new PutCommand({
      TableName: PLAYERS_TABLE,
      Item: player,
    })
  );
}

/**
 * Update specific attributes on a player record.
 */
async function updatePlayer(playerId, updates) {
  const expressions = [];
  const names = {};
  const values = {};

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
    })
  );
}

/**
 * Record daily activity. Merges with existing record for the same day
 * so that check-in and hand-completed don't overwrite each other.
 */
async function addActivity(playerId, date, data = {}) {
  // Try to get existing activity for this day
  const existing = await docClient.send(
    new GetCommand({
      TableName: ACTIVITY_TABLE,
      Key: { playerId, date },
    })
  );

  const merged = {
    playerId,
    date,
    checkedIn: true,
    timestamp: new Date().toISOString(),
    ...(existing.Item || {}),
    ...data,
    // Preserve truthy flags — never flip true back to false
    loggedIn: (existing.Item?.loggedIn || false) || (data.loggedIn || false),
    played: (existing.Item?.played || false) || (data.played || false),
    freezeUsed: (existing.Item?.freezeUsed || false) || (data.freezeUsed || false),
    streakBroken: (existing.Item?.streakBroken || false) || (data.streakBroken || false),
  };

  // Keep the higher streak values
  if (existing.Item) {
    merged.loginStreakAtDay = Math.max(existing.Item.loginStreakAtDay || 0, data.loginStreakAtDay || 0);
    merged.playStreakAtDay = Math.max(existing.Item.playStreakAtDay || 0, data.playStreakAtDay || 0);
  }

  await docClient.send(
    new PutCommand({
      TableName: ACTIVITY_TABLE,
      Item: merged,
    })
  );
}

/**
 * Get a player's activity for a date range.
 */
async function getActivity(playerId, startDate, endDate) {
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
    })
  );
  return result.Items || [];
}

/**
 * Get all players (for leaderboard).
 */
async function getAllPlayers() {
  const result = await docClient.send(
    new ScanCommand({ TableName: PLAYERS_TABLE })
  );
  return result.Items || [];
}

/**
 * Record a freeze history entry.
 */
async function addFreezeHistory(playerId, date, source) {
  await docClient.send(
    new PutCommand({
      TableName: FREEZE_HISTORY_TABLE,
      Item: {
        playerId,
        date,
        source,
        createdAt: new Date().toISOString(),
      },
    })
  );
}

/**
 * Get all rewards for a player.
 */
async function getRewards(playerId) {
  const result = await docClient.send(
    new QueryCommand({
      TableName: REWARDS_TABLE,
      KeyConditionExpression: 'playerId = :pid',
      ExpressionAttributeValues: { ':pid': playerId },
    })
  );
  return result.Items || [];
}

/**
 * Get freeze history for a player, sorted by date descending.
 */
async function getFreezeHistory(playerId) {
  const result = await docClient.send(
    new QueryCommand({
      TableName: FREEZE_HISTORY_TABLE,
      KeyConditionExpression: 'playerId = :pid',
      ExpressionAttributeValues: { ':pid': playerId },
      ScanIndexForward: false,
    })
  );
  return result.Items || [];
}

/**
 * Get missions for a player on a specific date.
 */
async function getMissions(playerId, date) {
  const result = await docClient.send(
    new GetCommand({
      TableName: MISSIONS_TABLE,
      Key: { playerId, date },
    })
  );
  return result.Item || null;
}

/**
 * Save missions for a player on a specific date.
 */
async function putMissions(item) {
  await docClient.send(
    new PutCommand({
      TableName: MISSIONS_TABLE,
      Item: item,
    })
  );
}

module.exports = {
  getPlayer,
  putPlayer,
  updatePlayer,
  addActivity,
  getActivity,
  getAllPlayers,
  addFreezeHistory,
  getRewards,
  getFreezeHistory,
  getMissions,
  putMissions,
  PLAYERS_TABLE,
  ACTIVITY_TABLE,
  REWARDS_TABLE,
  FREEZE_HISTORY_TABLE,
  MISSIONS_TABLE,
};
