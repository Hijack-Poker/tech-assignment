'use strict';

const { GAME_HAND, PLAYER_STATUS, ACTION } = require('../shared/games/common/constants');
const { createDeck, shuffle, deal, findWinners } = require('../shared/games/common/cards');
const { collectBets, isBettingRoundComplete } = require('../shared/games/common/betting');
const { calculatePots, distributePots } = require('../shared/games/common/pots');
const { getPlayersInHand, getActingPlayerCount, getNextSeat } = require('../shared/games/common/players');
const { toMoney } = require('../shared/utils');
const { fetchTable, savePlayers, saveGame } = require('./table-fetcher');
const { publishTableUpdate } = require('./event-publisher');
const { logger } = require('../shared/config/logger');

/**
 * Process a table through the hand state machine.
 *
 * Each call advances the hand by one step. The processor reads the current
 * hand_step from the game record, executes the logic for that step, then
 * saves the new state. The pipeline calls this repeatedly until the hand
 * is complete.
 *
 * For betting rounds, pass a playerAction { seat, action, amount } to
 * apply the player's decision. If no action is provided during a betting
 * round, the engine returns an awaiting_action status.
 */
async function processTable(tableId, playerAction) {
  const table = await fetchTable(tableId);
  if (!table) {
    logger.warn(`Table ${tableId} not found`);
    return { status: 'not_found' };
  }

  const { game, players } = table;
  let result;

  switch (game.handStep) {
    case GAME_HAND.GAME_PREP:
      result = gamePrep(game, players);
      break;
    case GAME_HAND.SETUP_DEALER:
      result = setupDealer(game, players);
      break;
    case GAME_HAND.SETUP_SMALL_BLIND:
      result = setupSmallBlind(game, players);
      break;
    case GAME_HAND.SETUP_BIG_BLIND:
      result = setupBigBlind(game, players);
      break;
    case GAME_HAND.DEAL_CARDS:
      result = dealCards(game, players);
      break;
    case GAME_HAND.PRE_FLOP_BETTING_ROUND:
      result = bettingRound(game, players, 'preflop', playerAction);
      break;
    case GAME_HAND.DEAL_FLOP:
      result = dealFlop(game, players);
      break;
    case GAME_HAND.FLOP_BETTING_ROUND:
      result = bettingRound(game, players, 'flop', playerAction);
      break;
    case GAME_HAND.DEAL_TURN:
      result = dealTurn(game, players);
      break;
    case GAME_HAND.TURN_BETTING_ROUND:
      result = bettingRound(game, players, 'turn', playerAction);
      break;
    case GAME_HAND.DEAL_RIVER:
      result = dealRiver(game, players);
      break;
    case GAME_HAND.RIVER_BETTING_ROUND:
      result = bettingRound(game, players, 'river', playerAction);
      break;
    case GAME_HAND.AFTER_RIVER_BETTING_ROUND:
      result = afterRiverBettingRound(game, players);
      break;
    case GAME_HAND.FIND_WINNERS:
      result = findHandWinners(game, players);
      break;
    case GAME_HAND.PAY_WINNERS:
      result = payWinners(game, players);
      break;
    case GAME_HAND.RECORD_STATS_AND_NEW_HAND:
      result = recordStatsAndNewHand(game, players);
      break;
    default:
      logger.warn(`Unknown hand step: ${game.handStep}`);
      return { status: 'unknown_step', step: game.handStep };
  }

  // Save updated state
  await saveGame(result.game);
  await savePlayers(result.players);

  // Publish table update for broadcast
  await publishTableUpdate(tableId, result.game, result.players);

  return {
    status: 'processed',
    tableId,
    step: result.game.handStep,
    stepName: getStepName(result.game.handStep),
  };
}

// ─── Step Implementations ─────────────────────────────────────────────

/**
 * Step 0: Prepare a new hand. Reset player states, increment game number.
 */
function gamePrep(game, players) {
  // Reset per-hand player state
  for (const player of players) {
    if (player.status !== PLAYER_STATUS.SITTING_OUT &&
        player.status !== PLAYER_STATUS.BUSTED) {
      player.status = PLAYER_STATUS.ACTIVE;
    }
    player.bet = 0;
    player.totalBet = 0;
    player.action = '';
    player.cards = [];
    player.handRank = '';
    player.winnings = 0;
  }

  game.pot = 0;
  game.currentBet = 0;
  game.communityCards = [];
  game.sidePots = [];
  game.deck = shuffle(createDeck());
  game.handStep = GAME_HAND.SETUP_DEALER;

  return { game, players };
}

/**
 * Step 1: Assign dealer button. Rotates to next active player.
 */
function setupDealer(game, players) {
  const active = getPlayersInHand(players);
  if (active.length < 2) {
    logger.warn('Not enough players to start hand');
    game.handStep = GAME_HAND.RECORD_STATS_AND_NEW_HAND;
    return { game, players };
  }

  // Rotate dealer to next active seat
  const nextDealer = getNextSeat(players, game.dealerSeat, game.maxSeats);
  game.dealerSeat = nextDealer;
  game.handStep = GAME_HAND.SETUP_SMALL_BLIND;

  return { game, players };
}

/**
 * Step 2: Post small blind.
 */
function setupSmallBlind(game, players) {
  const active = getPlayersInHand(players);

  // Heads-up: dealer posts SB. Otherwise, seat after dealer.
  const sbSeat = active.length === 2
    ? game.dealerSeat
    : getNextSeat(players, game.dealerSeat, game.maxSeats);

  game.smallBlindSeat = sbSeat;

  const sbPlayer = players.find((p) => p.seat === sbSeat);
  if (sbPlayer) {
    const sbAmount = Math.min(game.smallBlind, sbPlayer.stack);
    sbPlayer.stack = toMoney(sbPlayer.stack - sbAmount);
    sbPlayer.bet = sbAmount;
    sbPlayer.totalBet = sbAmount;
    if (sbPlayer.stack === 0) {
      sbPlayer.status = PLAYER_STATUS.ALL_IN;
    }
  }

  game.handStep = GAME_HAND.SETUP_BIG_BLIND;
  return { game, players };
}

/**
 * Step 3: Post big blind.
 */
function setupBigBlind(game, players) {
  const bbSeat = getNextSeat(players, game.smallBlindSeat, game.maxSeats);
  game.bigBlindSeat = bbSeat;

  const bbPlayer = players.find((p) => p.seat === bbSeat);
  if (bbPlayer) {
    const bbAmount = Math.min(game.bigBlind, bbPlayer.stack);
    bbPlayer.stack = toMoney(bbPlayer.stack - bbAmount);
    bbPlayer.bet = bbAmount;
    bbPlayer.totalBet = bbAmount;
    if (bbPlayer.stack === 0) {
      bbPlayer.status = PLAYER_STATUS.ALL_IN;
    }
  }

  game.currentBet = game.bigBlind;
  game.handStep = GAME_HAND.DEAL_CARDS;
  return { game, players };
}

/**
 * Step 4: Deal 2 hole cards to each active player.
 */
function dealCards(game, players) {
  const active = getPlayersInHand(players);
  for (const player of active) {
    player.cards = deal(game.deck, 2);
  }

  // First to act preflop: seat after big blind
  game.move = getNextSeat(players, game.bigBlindSeat, game.maxSeats);
  game.handStep = GAME_HAND.PRE_FLOP_BETTING_ROUND;
  return { game, players };
}

/**
 * Steps 5, 7, 9, 11: Execute a betting round.
 *
 * Processes one player action per call. The acting player is indicated
 * by game.move. If a playerAction is provided, it's applied to the acting
 * player. If no action is provided, returns awaiting_action status.
 *
 * When the round is complete (all active players have matched the current
 * bet), bets are collected and the hand advances to the next street.
 */
function bettingRound(game, players, round, playerAction) {
  const { processAction } = require('../shared/games/common/betting');

  // Check if only one player remains (everyone else folded)
  const inHand = getPlayersInHand(players);
  if (inHand.length <= 1) {
    game.handStep = GAME_HAND.FIND_WINNERS;
    return { game, players };
  }

  // If no active players can act (all-in or folded), skip to next step
  if (getActingPlayerCount(players) <= 1) {
    const collected = collectBets(game, players);
    game = collected.game;
    advanceToNextStreet(game, round);
    return { game, players };
  }

  // If no player action provided, signal that we're waiting
  if (!playerAction) {
    return { game, players, awaiting: true };
  }

  // Validate the action is for the correct seat
  const actingSeat = game.move;
  const actingPlayer = players.find(
    (p) => p.seat === actingSeat && p.status === PLAYER_STATUS.ACTIVE
  );

  if (!actingPlayer) {
    // No valid acting player — skip ahead
    const collected = collectBets(game, players);
    game = collected.game;
    advanceToNextStreet(game, round);
    return { game, players };
  }

  if (playerAction.seat !== actingSeat) {
    // Wrong seat — return error but don't change state
    return { game, players, error: `Awaiting action from seat ${actingSeat}, got seat ${playerAction.seat}` };
  }

  // Apply the action
  const action = playerAction.action;
  const amount = playerAction.amount || 0;

  // Track raise size for min-raise calculations
  if (action === ACTION.RAISE || action === ACTION.BET) {
    const raiseIncrease = amount - game.currentBet;
    game.lastRaiseSize = raiseIncrease > 0 ? raiseIncrease : game.bigBlind;
  }

  processAction(game, actingPlayer, action, amount);

  // Check if only one player remains after this action
  const remainingInHand = getPlayersInHand(players);
  if (remainingInHand.length <= 1) {
    const collected = collectBets(game, players);
    game = collected.game;
    game.handStep = GAME_HAND.FIND_WINNERS;
    return { game, players };
  }

  // Check if the betting round is complete
  if (isBettingRoundComplete(players, game.currentBet)) {
    const collected = collectBets(game, players);
    game = collected.game;
    advanceToNextStreet(game, round);
    return { game, players };
  }

  // Advance to next active player
  game.move = getNextSeat(players, actingSeat, game.maxSeats);

  return { game, players };
}

/**
 * Step 6: Deal the flop (3 community cards).
 */
function dealFlop(game, players) {
  const flop = deal(game.deck, 3);
  game.communityCards = [...game.communityCards, ...flop];
  game.move = getNextSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.FLOP_BETTING_ROUND;
  return { game, players };
}

/**
 * Step 8: Deal the turn (1 community card).
 */
function dealTurn(game, players) {
  const turn = deal(game.deck, 1);
  game.communityCards = [...game.communityCards, ...turn];
  game.move = getNextSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.TURN_BETTING_ROUND;
  return { game, players };
}

/**
 * Step 10: Deal the river (1 community card).
 */
function dealRiver(game, players) {
  const river = deal(game.deck, 1);
  game.communityCards = [...game.communityCards, ...river];
  game.move = getNextSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.RIVER_BETTING_ROUND;
  return { game, players };
}

/**
 * Step 12: After the final betting round, prepare for showdown.
 */
function afterRiverBettingRound(game, players) {
  game.handStep = GAME_HAND.FIND_WINNERS;
  return { game, players };
}

/**
 * Step 13: Determine winner(s). Evaluate hands against community cards.
 */
function findHandWinners(game, players) {
  const inHand = getPlayersInHand(players);

  // If only one player left, they win by default
  if (inHand.length === 1) {
    inHand[0].handRank = 'Last player standing';
    game.winners = [{ seat: inHand[0].seat, playerId: inHand[0].playerId }];
    game.handStep = GAME_HAND.PAY_WINNERS;
    return { game, players };
  }

  // Evaluate each player's best hand
  const hands = inHand.map((p) => ({
    playerId: p.playerId,
    seat: p.seat,
    cards: [...p.cards, ...game.communityCards],
  }));

  const winners = findWinners(hands);

  // Tag players with their hand rank
  for (const winner of winners) {
    const player = players.find((p) => p.seat === winner.seat);
    if (player) {
      player.handRank = winner.descr;
    }
  }

  game.winners = winners.map((w) => ({ seat: w.seat, playerId: w.playerId }));
  game.handStep = GAME_HAND.PAY_WINNERS;
  return { game, players };
}

/**
 * Step 14: Distribute pot to winner(s). Handles side pots.
 */
function payWinners(game, players) {
  const winnerSeats = (game.winners || []).map((w) => w.seat);

  // Calculate pots (main + side)
  const pots = calculatePots(players);

  if (pots.length === 0) {
    // Simple case: give entire pot to winners
    const share = toMoney(game.pot / winnerSeats.length);
    for (const seat of winnerSeats) {
      const player = players.find((p) => p.seat === seat);
      if (player) {
        player.stack = toMoney(player.stack + share);
        player.winnings = share;
      }
    }
  } else {
    // Distribute each pot to eligible winners
    const payouts = distributePots(pots, winnerSeats);
    for (const [seat, amount] of Object.entries(payouts)) {
      const player = players.find((p) => p.seat === parseInt(seat, 10));
      if (player) {
        player.stack = toMoney(player.stack + amount);
        player.winnings = amount;
      }
    }
  }

  game.pot = 0;
  game.handStep = GAME_HAND.RECORD_STATS_AND_NEW_HAND;
  return { game, players };
}

/**
 * Step 15: Record stats and prepare for the next hand.
 */
function recordStatsAndNewHand(game, players) {
  game.status = 'completed';

  logger.info(`Hand #${game.gameNo} complete on table ${game.tableId}`, {
    winners: game.winners,
  });

  return { game, players };
}

// ─── Helpers ──────────────────────────────────────────────────────────

function advanceToNextStreet(game, currentRound) {
  const nextStep = {
    preflop: GAME_HAND.DEAL_FLOP,
    flop: GAME_HAND.DEAL_TURN,
    turn: GAME_HAND.DEAL_RIVER,
    river: GAME_HAND.AFTER_RIVER_BETTING_ROUND,
  };
  game.handStep = nextStep[currentRound] || GAME_HAND.FIND_WINNERS;
}

function getStepName(step) {
  const names = Object.entries(GAME_HAND).find(([, v]) => v === step);
  return names ? names[0] : `UNKNOWN(${step})`;
}

module.exports = {
  processTable,
  // Export internals for testing
  gamePrep,
  setupDealer,
  setupSmallBlind,
  setupBigBlind,
  dealCards,
  bettingRound,
  dealFlop,
  dealTurn,
  dealRiver,
  findHandWinners,
  payWinners,
  recordStatsAndNewHand,
};
