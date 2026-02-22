'use strict';

const { PutCommand, GetCommand, QueryCommand, UpdateCommand, ScanCommand } = require('@aws-sdk/lib-dynamodb');
const { docClient } = require('../../shared/config/dynamo');

const PLAYERS_TABLE = process.env.REWARDS_PLAYERS_TABLE || 'rewards-players';
const TRANSACTIONS_TABLE = process.env.REWARDS_TRANSACTIONS_TABLE || 'rewards-transactions';

/**
 * Get a player's rewards profile.
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
 * Create or update a player's rewards profile.
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
 * Record a point transaction.
 */
async function addTransaction(playerId, transaction) {
  await docClient.send(
    new PutCommand({
      TableName: TRANSACTIONS_TABLE,
      Item: {
        playerId,
        timestamp: Date.now(),
        ...transaction,
      },
    })
  );
}

/**
 * Get a player's transaction history.
 */
async function getTransactions(playerId, limit = 20) {
  const result = await docClient.send(
    new QueryCommand({
      TableName: TRANSACTIONS_TABLE,
      KeyConditionExpression: 'playerId = :pid',
      ExpressionAttributeValues: { ':pid': playerId },
      ScanIndexForward: false,
      Limit: limit,
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
  addTransaction,
  getTransactions,
  getAllPlayers,
};
