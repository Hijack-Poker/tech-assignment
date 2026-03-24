'use strict';

const { processTable } = require('./lib/process-table');
const { fetchTable } = require('./lib/table-fetcher');
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
 * Accepts optional player action: { tableId, seat, action, amount }
 */
async function processHandHttp(event) {
  try {
    const body = JSON.parse(event.body || '{}');
    const { tableId, seat, action, amount } = body;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    // Build player action if provided
    const playerAction = (seat != null && action)
      ? { seat: parseInt(seat, 10), action, amount: amount != null ? parseFloat(amount) : 0 }
      : undefined;

    const result = await processTable(tableId, playerAction);

    // If engine is awaiting a player action, return the current state
    if (result && result.awaiting) {
      return {
        statusCode: 200,
        headers: CORS_HEADERS,
        body: JSON.stringify({
          success: true,
          result: {
            status: 'awaiting_action',
            tableId,
            step: result.game ? result.game.handStep : 0,
            stepName: result.game ? getStepName(result.game.handStep) : '',
            move: result.game ? result.game.move : 0,
          },
        }),
      };
    }

    if (result && result.error) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ success: false, error: result.error }),
      };
    }

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

function getStepName(step) {
  const { GAME_HAND } = require('./shared/games/common/constants');
  const entry = Object.entries(GAME_HAND).find(([, v]) => v === step);
  return entry ? entry[0] : `UNKNOWN(${step})`;
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
 * POST /reset — Reset the table by marking the current game as completed
 * and creating a fresh game via the next processTable call.
 */
async function resetTableHttp(event) {
  try {
    const body = JSON.parse(event.body || '{}');
    const { tableId } = body;

    if (!tableId) {
      return {
        statusCode: 400,
        headers: CORS_HEADERS,
        body: JSON.stringify({ error: 'tableId is required' }),
      };
    }

    const { sequelize } = require('./shared/config/db');
    const { QueryTypes } = require('sequelize');

    // Delete all games and game_players for this table to start fresh
    await sequelize.query(
      `DELETE FROM game_players WHERE table_id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.DELETE }
    );
    await sequelize.query(
      `DELETE FROM games WHERE table_id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.DELETE }
    );

    // Process the table — this will create game #1 via createNewGame
    const result = await processTable(parseInt(tableId, 10));

    return {
      statusCode: 200,
      headers: CORS_HEADERS,
      body: JSON.stringify({ success: true, message: 'Table reset', result }),
    };
  } catch (err) {
    logger.error(`Reset error: ${err.message}`);
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

module.exports = { handler, processHandHttp, getTableHttp, resetTableHttp, health };
