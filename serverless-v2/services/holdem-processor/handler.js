'use strict';

const { processTable } = require('./lib/process-table');
const { fetchTable, resetTable, freshResetTable } = require('./lib/table-fetcher');
const { logger } = require('./shared/config/logger');

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'Content-Type',
  'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
};

/**
 * SQS Lambda handler — processes batched table messages.
 * Each message contains a tableId to process.
 */
async function handler(event) {
  const records = event.Records || [];
  logger.info(`Processing ${records.length} SQS messages`);

  const batchItemFailures = [];

  for (const record of records) {
    try {
      const body = JSON.parse(record.body);
      const { tableId, gameType = 'texas' } = body;

      logger.info(`Processing table ${tableId}`, { tableId, gameType });
      await processTable(tableId);
      logger.info(`Table ${tableId} processed successfully`);
    } catch (err) {
      logger.error(`Failed to process message: ${err.message}`, {
        messageId: record.messageId,
        error: err.message,
      });
      batchItemFailures.push({ itemIdentifier: record.messageId });
    }
  }

  return { batchItemFailures };
}

/**
 * HTTP endpoint for manual trigger (serverless-offline dev).
 */
async function processHandHttp(event) {
  try {
    const body = JSON.parse(event.body || '{}');
    const { tableId, action, amount, seat } = body;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    const actionRequest = action ? { action, amount, seat } : null;
    const result = await processTable(tableId, actionRequest);

    return {
      statusCode: 200,
      headers: CORS_HEADERS,
      body: JSON.stringify({ success: true, result }),
    };
  } catch (err) {
    logger.error(`HTTP process error: ${err.message}`);
    return {
      statusCode: 500,
      headers: CORS_HEADERS,
      body: JSON.stringify({ error: err.message }),
    };
  }
}

/**
 * GET /table/{tableId} — read current table state for the UI.
 */
async function getTableHttp(event) {
  try {
    const tableId = event.pathParameters?.tableId || event.queryStringParameters?.tableId;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    const table = await fetchTable(tableId);

    if (!table) {
      return {
        statusCode: 404,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'Table not found' }),
      };
    }

    // Map step number to name
    const { GAME_HAND } = require('./shared/games/common/constants');
    const stepEntry = Object.entries(GAME_HAND).find(([, v]) => v === table.game.handStep);
    const stepName = stepEntry ? stepEntry[0] : `UNKNOWN(${table.game.handStep})`;

    return {
      statusCode: 200,
      headers: CORS_HEADERS,
      body: JSON.stringify({
        game: {
          ...table.game,
          stepName,
          deck: undefined, // don't leak the deck to the UI
        },
        players: table.players.map((p) => ({
          playerId: p.playerId,
          username: p.username,
          seat: p.seat,
          stack: p.stack,
          bet: p.bet,
          totalBet: p.totalBet,
          status: p.status,
          action: p.action,
          cards: p.cards,
          handRank: p.handRank,
          bestHand: p.bestHand || [],
          isWinner: p.isWinner || false,
          winnings: p.winnings,
        })),
      }),
    };
  } catch (err) {
    logger.error(`GET table error: ${err.message}`);
    return {
      statusCode: 500,
      headers: CORS_HEADERS,
      body: JSON.stringify({ error: err.message }),
    };
  }
}

/**
 * POST /table/{tableId}/reset — reset table to a fresh game.
 */
async function resetTableHttp(event) {
  try {
    const tableId = event.pathParameters?.tableId || JSON.parse(event.body || '{}').tableId;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    const table = await resetTable(tableId);

    if (!table) {
      return {
        statusCode: 404,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'Table not found' }),
      };
    }

    return {
      statusCode: 200,
      headers: CORS_HEADERS,
      body: JSON.stringify({ success: true, gameNo: table.game.gameNo }),
    };
  } catch (err) {
    logger.error(`Reset table error: ${err.message}`);
    return {
      statusCode: 500,
      headers: CORS_HEADERS,
      body: JSON.stringify({ error: err.message }),
    };
  }
}

/**
 * POST /table/{tableId}/fresh-reset — wipe all game history and start fresh with initial stacks.
 */
async function freshResetTableHttp(event) {
  try {
    const tableId = event.pathParameters?.tableId || JSON.parse(event.body || '{}').tableId;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    const table = await freshResetTable(tableId);

    if (!table) {
      return {
        statusCode: 404,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'Table not found' }),
      };
    }

    return {
      statusCode: 200,
      headers: CORS_HEADERS,
      body: JSON.stringify({ success: true, gameNo: table.game.gameNo }),
    };
  } catch (err) {
    logger.error(`Fresh reset table error: ${err.message}`);
    return {
      statusCode: 500,
      headers: CORS_HEADERS,
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
    headers: CORS_HEADERS,
    body: JSON.stringify({
      service: 'holdem-processor',
      status: 'ok',
      timestamp: new Date().toISOString(),
    }),
  };
}

module.exports = { handler, processHandHttp, getTableHttp, resetTableHttp, freshResetTableHttp, health };
