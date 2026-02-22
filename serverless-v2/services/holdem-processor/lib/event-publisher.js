'use strict';

const { EventBridgeClient, PutEventsCommand } = require('@aws-sdk/client-eventbridge');
const { logger } = require('../shared/config/logger');

const clientConfig = {
  region: process.env.AWS_REGION || 'us-east-1',
  requestHandler: {
    requestTimeout: 3000, // 3 second timeout for local dev
    connectionTimeout: 2000,
  },
};

// Use mock EventBridge endpoint in local dev
if (process.env.EVENTBRIDGE_ENDPOINT) {
  clientConfig.endpoint = process.env.EVENTBRIDGE_ENDPOINT;
  clientConfig.credentials = {
    accessKeyId: process.env.AWS_ACCESS_KEY_ID || 'local',
    secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY || 'local',
  };
}

const eventBridge = new EventBridgeClient(clientConfig);

/**
 * Publish a TABLE_UPDATE event to EventBridge.
 * The cash-game-broadcast service listens for these events
 * and pushes updates to connected WebSocket clients.
 */
async function publishTableUpdate(tableId, game, players) {
  const eventBusName = process.env.EVENT_BUS_NAME || 'poker-events';

  const detail = {
    gameType: 'texas',
    tableId,
    timestamp: Date.now(),
    hasFullData: true,
    tableData: {
      tableId,
      gameNo: game.gameNo,
      handStep: game.handStep,
      pot: game.pot,
      communityCards: game.communityCards,
      dealerSeat: game.dealerSeat,
      move: game.move,
      players: players.map((p) => ({
        playerId: p.playerId,
        seat: p.seat,
        stack: p.stack,
        bet: p.bet,
        status: p.status,
        action: p.action,
        handRank: p.handRank,
        winnings: p.winnings,
      })),
    },
  };

  try {
    const command = new PutEventsCommand({
      Entries: [
        {
          EventBusName: eventBusName,
          Source: 'hijack.holdem-processor',
          DetailType: 'TABLE_UPDATE',
          Detail: JSON.stringify(detail),
        },
      ],
    });

    const response = await eventBridge.send(command);
    logger.info(`Published TABLE_UPDATE for table ${tableId}`, {
      eventId: response.Entries?.[0]?.EventId,
      failedCount: response.FailedEntryCount,
    });

    return response;
  } catch (err) {
    logger.error(`Failed to publish TABLE_UPDATE: ${err.message}`, {
      tableId,
      error: err.message,
    });
    // Don't throw — game processing should continue even if broadcast fails
    return null;
  }
}

module.exports = { publishTableUpdate };
