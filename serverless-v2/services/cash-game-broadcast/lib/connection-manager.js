'use strict';

const { PutCommand, GetCommand, DeleteCommand, QueryCommand } = require('@aws-sdk/lib-dynamodb');
const { docClient } = require('../shared/config/dynamo');
const { logger } = require('../shared/config/logger');

const CONNECTIONS_TABLE = process.env.CONNECTIONS_TABLE || 'connections';

/**
 * Store a new WebSocket connection.
 */
async function addConnection(connectionId, playerId, tableSubscription) {
  try {
    await docClient.send(
      new PutCommand({
        TableName: CONNECTIONS_TABLE,
        Item: {
          connectionId,
          playerId,
          tableSubscription,
          connectedAt: new Date().toISOString(),
        },
      })
    );
    logger.info(`Connection added: ${connectionId}`, { playerId, tableSubscription });
  } catch (err) {
    logger.error(`Failed to add connection: ${err.message}`);
  }
}

/**
 * Remove a WebSocket connection (on disconnect or stale).
 */
async function removeConnection(connectionId) {
  try {
    await docClient.send(
      new DeleteCommand({
        TableName: CONNECTIONS_TABLE,
        Key: { connectionId },
      })
    );
    logger.info(`Connection removed: ${connectionId}`);
  } catch (err) {
    logger.error(`Failed to remove connection: ${err.message}`);
  }
}

/**
 * Get a connection by ID.
 */
async function getConnection(connectionId) {
  try {
    const result = await docClient.send(
      new GetCommand({
        TableName: CONNECTIONS_TABLE,
        Key: { connectionId },
      })
    );
    return result.Item || null;
  } catch (err) {
    logger.error(`Failed to get connection: ${err.message}`);
    return null;
  }
}

/**
 * Get all connections subscribed to a specific table.
 * Uses GSI on tableSubscription.
 */
async function getTableSubscribers(gameType, tableId) {
  const subscriptionKey = `${gameType}#${tableId}`;
  try {
    const result = await docClient.send(
      new QueryCommand({
        TableName: CONNECTIONS_TABLE,
        IndexName: 'tableSubscription-index',
        KeyConditionExpression: 'tableSubscription = :sub',
        ExpressionAttributeValues: {
          ':sub': subscriptionKey,
        },
      })
    );
    return result.Items || [];
  } catch (err) {
    logger.error(`Failed to get subscribers for ${subscriptionKey}: ${err.message}`);
    return [];
  }
}

module.exports = {
  addConnection,
  removeConnection,
  getConnection,
  getTableSubscribers,
};
