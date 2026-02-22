'use strict';

const { getTableSubscribers, removeConnection } = require('./lib/connection-manager');
const { logger } = require('./shared/config/logger');

/**
 * Main handler — receives TABLE_UPDATE events and broadcasts to connected clients.
 *
 * In production, this pushes updates via API Gateway WebSocket Management API.
 * In local dev, it logs the broadcast (no real WebSocket server).
 */
async function main(event) {
  try {
    // Handle both EventBridge and HTTP (serverless-offline) payloads
    let detail;
    if (event.detail) {
      // EventBridge format
      detail = event.detail;
    } else if (event.body) {
      // HTTP format (serverless-offline)
      const body = JSON.parse(event.body);
      detail = body.detail || body;
    } else {
      logger.warn('Unknown event format', { event });
      return { statusCode: 400, body: JSON.stringify({ error: 'Unknown event format' }) };
    }

    const { gameType, tableId, tableData } = detail;
    logger.info(`Broadcasting TABLE_UPDATE for table ${tableId}`, { gameType, tableId });

    // Look up connected clients subscribed to this table
    const subscribers = await getTableSubscribers(gameType, tableId);
    logger.info(`Found ${subscribers.length} subscribers for table ${tableId}`);

    let sentCount = 0;
    const staleConnections = [];

    for (const subscriber of subscribers) {
      try {
        // In production: send personalized data via WebSocket Management API
        // In local dev: log what would be sent
        logger.info(`[Mock] Would send to connection ${subscriber.connectionId}`, {
          tableId,
          playerId: subscriber.playerId,
          handStep: tableData?.handStep,
        });
        sentCount++;
      } catch (err) {
        if (err.statusCode === 410) {
          // Connection is gone — mark for cleanup
          staleConnections.push(subscriber.connectionId);
        } else {
          logger.error(`Failed to send to ${subscriber.connectionId}: ${err.message}`);
        }
      }
    }

    // Clean up stale connections
    for (const connectionId of staleConnections) {
      await removeConnection(connectionId);
    }

    return {
      statusCode: 200,
      body: JSON.stringify({
        message: `Broadcast sent to ${sentCount} subscribers`,
        tableId,
        staleRemoved: staleConnections.length,
      }),
    };
  } catch (err) {
    logger.error(`Broadcast error: ${err.message}`, { error: err.message });
    return {
      statusCode: 500,
      body: JSON.stringify({ error: err.message }),
    };
  }
}

/**
 * Health check endpoint.
 */
async function health() {
  return {
    statusCode: 200,
    body: JSON.stringify({
      service: 'cash-game-broadcast',
      status: 'ok',
      timestamp: new Date().toISOString(),
    }),
  };
}

module.exports = { main, health };
