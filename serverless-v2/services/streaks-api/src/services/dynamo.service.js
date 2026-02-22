'use strict';

const { PutCommand, GetCommand, QueryCommand, UpdateCommand, ScanCommand } = require('@aws-sdk/lib-dynamodb');
const { docClient } = require('../../shared/config/dynamo');

const PLAYERS_TABLE = process.env.STREAKS_PLAYERS_TABLE || 'streaks-players';
const ACTIVITY_TABLE = process.env.STREAKS_ACTIVITY_TABLE || 'streaks-activity';

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
 * Record a daily check-in.
 */
async function addActivity(playerId, date, data = {}) {
  await docClient.send(
    new PutCommand({
      TableName: ACTIVITY_TABLE,
      Item: {
        playerId,
        date,
        checkedIn: true,
        timestamp: new Date().toISOString(),
        ...data,
      },
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

module.exports = {
  getPlayer,
  putPlayer,
  updatePlayer,
  addActivity,
  getActivity,
  getAllPlayers,
};
