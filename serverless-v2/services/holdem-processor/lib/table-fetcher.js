'use strict';

const { sequelize } = require('../shared/config/db');
const { QueryTypes } = require('sequelize');
const { logger } = require('../shared/config/logger');

/**
 * Fetch the current game and player state for a table from MySQL.
 * Returns { game, players } or null if not found.
 */
async function fetchTable(tableId) {
  try {
    // Get the active game for this table
    const [game] = await sequelize.query(
      `SELECT g.*, gt.small_blind, gt.big_blind, gt.max_seats, gt.name as table_name
       FROM games g
       JOIN game_tables gt ON g.table_id = gt.id
       WHERE g.table_id = :tableId AND g.status = 'in_progress'
       ORDER BY g.game_no DESC
       LIMIT 1`,
      { replacements: { tableId }, type: QueryTypes.SELECT }
    );

    if (!game) {
      // No active game — create a new one
      return await createNewGame(tableId);
    }

    // Get players in this game
    const players = await sequelize.query(
      `SELECT gp.*, p.guid, p.username
       FROM game_players gp
       JOIN players p ON gp.player_id = p.id
       WHERE gp.game_id = :gameId
       ORDER BY gp.seat`,
      { replacements: { gameId: game.id }, type: QueryTypes.SELECT }
    );

    return {
      game: normalizeGame(game),
      players: players.map(normalizePlayer),
    };
  } catch (err) {
    logger.error(`Failed to fetch table ${tableId}: ${err.message || err}`, { stack: err.stack });
    return null;
  }
}

/**
 * Create a new game for a table.
 * Carries over stacks from the last completed game; busts players with $0.
 * Falls back to fresh buy-ins if no previous game exists.
 */
async function createNewGame(tableId) {
  try {
    const [table] = await sequelize.query(
      `SELECT * FROM game_tables WHERE id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.SELECT }
    );

    if (!table) return null;

    const maxSeats = parseInt(table.max_seats, 10) || 6;
    const minBuy = parseFloat(table.min_buy_in);
    const maxBuy = parseFloat(table.max_buy_in);

    // Try to carry over from the last completed game
    const prevPlayers = await sequelize.query(
      `SELECT gp.player_id, gp.seat, gp.stack, p.guid, p.username
       FROM game_players gp
       JOIN players p ON gp.player_id = p.id
       JOIN games g ON gp.game_id = g.id
       WHERE g.table_id = :tableId AND g.status = 'completed'
       ORDER BY g.game_no DESC, gp.seat`,
      { replacements: { tableId }, type: QueryTypes.SELECT }
    );

    // Group by game (take only the most recent completed game's players)
    let carryOverPlayers = null;
    if (prevPlayers.length > 0) {
      // All rows from the query are from the latest completed game (ORDER BY game_no DESC)
      // but we need to de-dup by seat in case of multiple completed games
      const seen = new Set();
      carryOverPlayers = [];
      for (const p of prevPlayers) {
        if (!seen.has(p.seat)) {
          seen.add(p.seat);
          carryOverPlayers.push(p);
        }
      }
      // Filter out busted players (stack <= 0)
      carryOverPlayers = carryOverPlayers.filter(p => parseFloat(p.stack) > 0);
    }

    // Fall back to fresh buy-ins if no previous game or not enough players
    if (!carryOverPlayers || carryOverPlayers.length < 2) {
      const allPlayers = await sequelize.query(
        `SELECT * FROM players ORDER BY id LIMIT ${maxSeats}`,
        { type: QueryTypes.SELECT }
      );

      if (allPlayers.length < 2) return null;

      carryOverPlayers = allPlayers.map((p, i) => {
        const stackOptions = [
          maxBuy,
          Math.round((minBuy + maxBuy) * 0.35),
          Math.round((minBuy + maxBuy) * 0.75),
          minBuy,
          Math.round((minBuy + maxBuy) * 0.6),
          Math.round((minBuy + maxBuy) * 0.5),
        ];
        return {
          player_id: p.id,
          seat: i + 1,
          stack: stackOptions[i] || Math.round((minBuy + maxBuy) / 2),
        };
      });
    }

    // Get next game number
    const [lastGame] = await sequelize.query(
      `SELECT COALESCE(MAX(game_no), 0) + 1 as next_no FROM games WHERE table_id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.SELECT }
    );
    const nextGameNo = lastGame?.next_no || 1;

    // Get dealer seat from last completed game to carry over rotation
    const [lastCompleted] = await sequelize.query(
      `SELECT dealer_seat FROM games WHERE table_id = :tableId AND status = 'completed' ORDER BY game_no DESC LIMIT 1`,
      { replacements: { tableId }, type: QueryTypes.SELECT }
    );
    const dealerSeat = lastCompleted ? lastCompleted.dealer_seat : 1;

    // Insert game record
    const [gameId] = await sequelize.query(
      `INSERT INTO games (table_id, game_no, hand_step, dealer_seat, pot, status)
       VALUES (:tableId, :gameNo, 0, :dealerSeat, 0, 'in_progress')`,
      { replacements: { tableId, gameNo: nextGameNo, dealerSeat }, type: QueryTypes.INSERT }
    );

    // Insert players with carried-over stacks
    for (const p of carryOverPlayers) {
      await sequelize.query(
        `INSERT INTO game_players (game_id, table_id, player_id, seat, stack, status)
         VALUES (:gameId, :tableId, :playerId, :seat, :stack, '1')`,
        {
          replacements: {
            gameId,
            tableId,
            playerId: p.player_id,
            seat: p.seat,
            stack: parseFloat(p.stack),
          },
          type: QueryTypes.INSERT,
        }
      );
    }

    // Re-fetch the created game
    return fetchTable(tableId);
  } catch (err) {
    logger.error(`Failed to create new game for table ${tableId}: ${err.message}`);
    return null;
  }
}

/**
 * Save updated game state back to MySQL.
 */
async function saveGame(game) {
  try {
    await sequelize.query(
      `UPDATE games SET
        hand_step = :handStep,
        dealer_seat = :dealerSeat,
        small_blind_seat = :smallBlindSeat,
        big_blind_seat = :bigBlindSeat,
        community_cards = :communityCards,
        deck = :deck,
        current_bet = :currentBet,
        last_raise_size = :lastRaiseSize,
        winners = :winners,
        pot = :pot,
        side_pots = :sidePots,
        move = :move,
        status = :status
       WHERE id = :id`,
      {
        replacements: {
          id: game.id,
          handStep: game.handStep,
          dealerSeat: game.dealerSeat,
          smallBlindSeat: game.smallBlindSeat || 0,
          bigBlindSeat: game.bigBlindSeat || 0,
          communityCards: JSON.stringify(game.communityCards || []),
          deck: JSON.stringify(game.deck || []),
          currentBet: game.currentBet || 0,
          lastRaiseSize: game.lastRaiseSize || 0,
          winners: JSON.stringify(game.winners || []),
          pot: game.pot,
          sidePots: JSON.stringify(game.sidePots || []),
          move: game.move || 0,
          status: game.status,
        },
        type: QueryTypes.UPDATE,
      }
    );
  } catch (err) {
    logger.error(`Failed to save game ${game.id}: ${err.message}`);
    throw err;
  }
}

/**
 * Save updated player states back to MySQL.
 */
async function savePlayers(players) {
  try {
    for (const player of players) {
      await sequelize.query(
        `UPDATE game_players SET
          stack = :stack,
          bet = :bet,
          total_bet = :totalBet,
          status = :status,
          action = :action,
          cards = :cards,
          best_hand = :bestHand,
          hand_rank = :handRank,
          is_winner = :isWinner,
          winnings = :winnings
         WHERE id = :id`,
        {
          replacements: {
            id: player.id,
            stack: player.stack,
            bet: player.bet,
            totalBet: player.totalBet,
            status: player.status,
            action: player.action,
            cards: JSON.stringify(player.cards || []),
            bestHand: JSON.stringify(player.bestHand || []),
            handRank: player.handRank || '',
            isWinner: player.isWinner ? 1 : 0,
            winnings: player.winnings || 0,
          },
          type: QueryTypes.UPDATE,
        }
      );
    }
  } catch (err) {
    logger.error(`Failed to save players: ${err.message}`);
    throw err;
  }
}

// ─── Normalizers ──────────────────────────────────────────────────────

function normalizeGame(row) {
  return {
    id: row.id,
    tableId: row.table_id,
    tableName: row.table_name,
    gameNo: row.game_no,
    handStep: row.hand_step,
    dealerSeat: row.dealer_seat,
    smallBlindSeat: row.small_blind_seat || 0,
    bigBlindSeat: row.big_blind_seat || 0,
    communityCards: typeof row.community_cards === 'string'
      ? JSON.parse(row.community_cards || '[]')
      : (row.community_cards || []),
    pot: parseFloat(row.pot) || 0,
    sidePots: typeof row.side_pots === 'string'
      ? JSON.parse(row.side_pots || '[]')
      : (row.side_pots || []),
    move: row.move || 0,
    status: row.status,
    smallBlind: parseFloat(row.small_blind),
    bigBlind: parseFloat(row.big_blind),
    maxSeats: row.max_seats,
    deck: typeof row.deck === 'string'
      ? JSON.parse(row.deck || '[]')
      : (row.deck || []),
    currentBet: parseFloat(row.current_bet) || 0,
    lastRaiseSize: parseFloat(row.last_raise_size) || 0,
    winners: typeof row.winners === 'string'
      ? JSON.parse(row.winners || '[]')
      : (row.winners || []),
  };
}

function normalizePlayer(row) {
  return {
    id: row.id,
    gameId: row.game_id,
    tableId: row.table_id,
    playerId: row.player_id,
    guid: row.guid,
    username: row.username,
    seat: row.seat,
    stack: parseFloat(row.stack) || 0,
    bet: parseFloat(row.bet) || 0,
    totalBet: parseFloat(row.total_bet) || 0,
    status: row.status || '1',
    action: row.action || '',
    cards: typeof row.cards === 'string'
      ? JSON.parse(row.cards || '[]')
      : (row.cards || []),
    bestHand: typeof row.best_hand === 'string'
      ? JSON.parse(row.best_hand || '[]')
      : (row.best_hand || []),
    handRank: row.hand_rank || '',
    isWinner: row.is_winner === 1 || row.is_winner === '1',
    winnings: parseFloat(row.winnings) || 0,
  };
}

/**
 * Reset table — mark any in-progress game as completed and create a fresh one.
 */
async function resetTable(tableId) {
  try {
    // Mark all in-progress games for this table as completed
    await sequelize.query(
      `UPDATE games SET status = 'completed' WHERE table_id = :tableId AND status = 'in_progress'`,
      { replacements: { tableId }, type: QueryTypes.UPDATE }
    );

    // Create a fresh game
    return await createNewGame(tableId);
  } catch (err) {
    logger.error(`Failed to reset table ${tableId}: ${err.message}`);
    throw err;
  }
}

/**
 * Fresh-reset table — delete ALL game history and create a brand-new game
 * with fresh buy-in stacks so every player starts from scratch.
 */
async function freshResetTable(tableId) {
  try {
    // Delete all game_players and games for this table
    await sequelize.query(
      `DELETE gp FROM game_players gp JOIN games g ON gp.game_id = g.id WHERE g.table_id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.DELETE }
    );
    await sequelize.query(
      `DELETE FROM games WHERE table_id = :tableId`,
      { replacements: { tableId }, type: QueryTypes.DELETE }
    );

    // Now createNewGame will find no previous completed games
    // and will allocate fresh buy-in stacks for all players
    return await createNewGame(tableId);
  } catch (err) {
    logger.error(`Failed to fresh-reset table ${tableId}: ${err.message}`);
    throw err;
  }
}

module.exports = { fetchTable, saveGame, savePlayers, resetTable, freshResetTable };
