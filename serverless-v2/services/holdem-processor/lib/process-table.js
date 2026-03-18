'use strict';

const { GAME_HAND, PLAYER_STATUS, ACTION } = require('../shared/games/common/constants');
const { createDeck, shuffle, deal, findWinners, evaluateAllHands } = require('../shared/games/common/cards');
const {
  collectBets,
  isBettingRoundComplete,
  validateAction,
  getValidActions,
} = require('../shared/games/common/betting');
const { calculatePots } = require('../shared/games/common/pots');
const {
  getPlayersInHand,
  getActingPlayerCount,
  getNextActingSeat,
  getNextPlayerSeat,
} = require('../shared/games/common/players');
const { toMoney } = require('../shared/utils');
const { fetchTable, savePlayers, saveGame } = require('./table-fetcher');
const { publishTableUpdate } = require('./event-publisher');
const { logger } = require('../shared/config/logger');

// ─── Main entry point ─────────────────────────────────────────────────

async function processTable(tableId, actionRequest = null) {
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
      result = bettingRound(game, players, 'preflop', actionRequest);
      break;
    case GAME_HAND.DEAL_FLOP:
      result = dealFlop(game, players);
      break;
    case GAME_HAND.FLOP_BETTING_ROUND:
      result = bettingRound(game, players, 'flop', actionRequest);
      break;
    case GAME_HAND.DEAL_TURN:
      result = dealTurn(game, players);
      break;
    case GAME_HAND.TURN_BETTING_ROUND:
      result = bettingRound(game, players, 'turn', actionRequest);
      break;
    case GAME_HAND.DEAL_RIVER:
      result = dealRiver(game, players);
      break;
    case GAME_HAND.RIVER_BETTING_ROUND:
      result = bettingRound(game, players, 'river', actionRequest);
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

  // Rejected actions: do NOT persist state or broadcast
  if (result.error) {
    return {
      status: 'rejected',
      tableId,
      error: result.error,
    };
  }

  await saveGame(result.game);
  await savePlayers(result.players);
  await publishTableUpdate(tableId, result.game, result.players);

  return {
    status: 'processed',
    tableId,
    step: result.game.handStep,
    stepName: getStepName(result.game.handStep),
  };
}

// ─── Step implementations ─────────────────────────────────────────────

/** Step 0: Prepare a new hand. */
function gamePrep(game, players) {
  for (const player of players) {
    if (
      player.status !== PLAYER_STATUS.SITTING_OUT &&
      player.status !== PLAYER_STATUS.BUSTED
    ) {
      player.status = PLAYER_STATUS.ACTIVE;
    }
    player.bet = 0;
    player.totalBet = 0;
    player.action = '';
    player.cards = [];
    player.handRank = '';
    player.bestHand = [];
    player.isWinner = false;
    player.winnings = 0;
  }

  game.pot = 0;
  game.currentBet = 0;
  game.communityCards = [];
  game.sidePots = [];
  game.deck = shuffle(createDeck());
  game.lastRaiseSize = game.bigBlind;
  game.noReopenSeats = [];
  game.handStep = GAME_HAND.SETUP_DEALER;

  return { game, players };
}

/** Step 1: Rotate dealer button to next active player. */
function setupDealer(game, players) {
  const active = getPlayersInHand(players);
  if (active.length < 2) {
    logger.warn('Not enough players to start hand');
    game.handStep = GAME_HAND.RECORD_STATS_AND_NEW_HAND;
    return { game, players };
  }

  game.dealerSeat = getNextPlayerSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.SETUP_SMALL_BLIND;
  return { game, players };
}

/** Step 2: Post small blind. Heads-up: dealer posts SB. */
function setupSmallBlind(game, players) {
  const active = getPlayersInHand(players);
  const sbSeat =
    active.length === 2
      ? game.dealerSeat
      : getNextPlayerSeat(players, game.dealerSeat, game.maxSeats);

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

/** Step 3: Post big blind. */
function setupBigBlind(game, players) {
  const bbSeat = getNextPlayerSeat(
    players,
    game.smallBlindSeat,
    game.maxSeats
  );
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
  game.lastRaiseSize = game.bigBlind;
  game.handStep = GAME_HAND.DEAL_CARDS;
  return { game, players };
}

/** Step 4: Deal 2 hole cards to each in-hand player. */
function dealCards(game, players) {
  const active = getPlayersInHand(players);
  for (const player of active) {
    player.cards = deal(game.deck, 2);
  }

  // Preflop: first to act is left of BB.
  // Heads-up: first to act is dealer (= SB), who is left of BB.
  game.move = getNextActingSeat(players, game.bigBlindSeat, game.maxSeats);
  game.handStep = GAME_HAND.PRE_FLOP_BETTING_ROUND;
  return { game, players };
}

// ─── Betting round engine ─────────────────────────────────────────────

/**
 * Process one player action per call. Returns { game, players } on success
 * or { game, players, error } on rejection.
 */
function bettingRound(game, players, round, actionRequest = null) {
  // All fold → immediate winner
  const inHand = getPlayersInHand(players);
  if (inHand.length <= 1) {
    const collected = collectBets(game, players);
    game = collected.game;
    game.handStep = GAME_HAND.FIND_WINNERS;
    return { game, players };
  }

  // Nobody can act (all are all-in or folded) → advance street
  if (getActingPlayerCount(players) === 0) {
    const collected = collectBets(game, players);
    game = collected.game;
    advanceToNextStreet(game, round);
    return { game, players };
  }

  // Round already complete → advance
  if (isBettingRoundComplete(players, game.currentBet)) {
    const collected = collectBets(game, players);
    game = collected.game;
    advanceToNextStreet(game, round);
    return { game, players };
  }

  // Find acting player
  let actingSeat = game.move;
  let actingPlayer = players.find(
    (p) => p.seat === actingSeat && p.status === PLAYER_STATUS.ACTIVE
  );

  // If move points to a non-active seat, find the next active
  if (!actingPlayer) {
    actingSeat = getNextActingSeat(players, actingSeat - 1, game.maxSeats);
    if (actingSeat === -1) {
      const collected = collectBets(game, players);
      game = collected.game;
      advanceToNextStreet(game, round);
      return { game, players };
    }
    game.move = actingSeat;
    actingPlayer = players.find(
      (p) => p.seat === actingSeat && p.status === PLAYER_STATUS.ACTIVE
    );
  }

  // Out-of-turn check
  if (actionRequest && actionRequest.seat && actionRequest.seat !== actingSeat) {
    return {
      game,
      players,
      error: {
        code: 'OUT_OF_TURN',
        message: `It is seat ${actingSeat}'s turn, not seat ${actionRequest.seat}`,
      },
    };
  }

  // No action submitted → return waiting (never auto-fold)
  if (!actionRequest || !actionRequest.action) {
    return {
      game,
      players,
      error: {
        code: 'AWAITING_ACTION',
        message: `Waiting for seat ${actingSeat} to act`,
      },
    };
  }

  // Validate
  const validation = validateAction(actingPlayer, game, actionRequest);
  if (!validation.valid) {
    return {
      game,
      players,
      error: { code: validation.reason, message: validation.message },
    };
  }

  // Apply
  applyAction(game, players, actingPlayer, validation.action, validation.amount);

  // Check for all-fold
  const remaining = getPlayersInHand(players);
  if (remaining.length <= 1) {
    const collected = collectBets(game, players);
    game = collected.game;
    game.handStep = GAME_HAND.FIND_WINNERS;
    return { game, players };
  }

  // Advance to next player or end round
  const nextSeat = getNextActingSeat(players, actingSeat, game.maxSeats);
  if (nextSeat === -1 || isBettingRoundComplete(players, game.currentBet)) {
    const collected = collectBets(game, players);
    game = collected.game;
    game.move = 0;
    advanceToNextStreet(game, round);
  } else {
    game.move = nextSeat;
  }

  return { game, players };
}

// ─── Action application ───────────────────────────────────────────────

function applyAction(game, players, player, action, amount) {
  switch (action) {
    case ACTION.FOLD:
      player.status = PLAYER_STATUS.FOLDED;
      player.action = ACTION.FOLD;
      break;

    case ACTION.CHECK:
      player.action = ACTION.CHECK;
      break;

    case ACTION.CALL: {
      const toCall = Math.max(0, toMoney(game.currentBet - player.bet));
      const callAmount = Math.min(toCall, player.stack);
      player.stack = toMoney(player.stack - callAmount);
      player.bet = toMoney(player.bet + callAmount);
      player.totalBet = toMoney(player.totalBet + callAmount);
      player.action = ACTION.CALL;
      if (player.stack === 0) {
        player.status = PLAYER_STATUS.ALL_IN;
        player.action = ACTION.ALLIN;
      }
      break;
    }

    case ACTION.BET: {
      // amount = total bet size
      const betAmount = Math.min(amount, player.stack);
      player.stack = toMoney(player.stack - betAmount);
      player.bet = toMoney(player.bet + betAmount);
      player.totalBet = toMoney(player.totalBet + betAmount);
      player.action = ACTION.BET;

      game.currentBet = player.bet;
      game.lastRaiseSize = player.bet;

      if (player.stack === 0) {
        player.status = PLAYER_STATUS.ALL_IN;
        player.action = ACTION.ALLIN;
      }

      resetActionsForRaise(game, players, player.seat, true);
      break;
    }

    case ACTION.RAISE: {
      // amount = raise-TO target (player.bet will become this value)
      const raiseTo = Math.min(amount, toMoney(player.bet + player.stack));
      const chipsToPut = toMoney(raiseTo - player.bet);
      const raiseIncrement = toMoney(raiseTo - game.currentBet);

      player.stack = toMoney(player.stack - chipsToPut);
      player.bet = raiseTo;
      player.totalBet = toMoney(player.totalBet + chipsToPut);
      player.action = ACTION.RAISE;

      const isFullRaise =
        raiseIncrement >= (game.lastRaiseSize || game.bigBlind);
      game.currentBet = raiseTo;
      if (isFullRaise) {
        game.lastRaiseSize = raiseIncrement;
      }

      if (player.stack === 0) {
        player.status = PLAYER_STATUS.ALL_IN;
        player.action = ACTION.ALLIN;
      }

      resetActionsForRaise(game, players, player.seat, isFullRaise);
      break;
    }

    case ACTION.ALLIN: {
      const allInAmount = player.stack;
      const newBet = toMoney(player.bet + allInAmount);

      player.totalBet = toMoney(player.totalBet + allInAmount);
      player.stack = 0;
      player.bet = newBet;
      player.status = PLAYER_STATUS.ALL_IN;
      player.action = ACTION.ALLIN;

      if (newBet > game.currentBet) {
        const raiseIncrement = toMoney(newBet - game.currentBet);
        const isFullRaise =
          raiseIncrement >= (game.lastRaiseSize || game.bigBlind);

        game.currentBet = newBet;
        if (isFullRaise) {
          game.lastRaiseSize = raiseIncrement;
        }

        resetActionsForRaise(game, players, player.seat, isFullRaise);
      }
      break;
    }
  }
}

/**
 * After a bet/raise, reset other players' actions so they must respond.
 * Full raise: everyone must respond (action reopened).
 * Short all-in: only players whose bet < currentBet respond,
 * and previously-acted players are added to noReopenSeats.
 */
function resetActionsForRaise(game, players, raiserSeat, isFullRaise) {
  if (isFullRaise) {
    game.noReopenSeats = [];
    for (const p of players) {
      if (p.seat === raiserSeat) continue;
      if (p.status !== PLAYER_STATUS.ACTIVE) continue;
      p.action = '';
    }
  } else {
    // Short all-in
    for (const p of players) {
      if (p.seat === raiserSeat) continue;
      if (p.status !== PLAYER_STATUS.ACTIVE) continue;
      if (p.bet < game.currentBet) {
        if (p.action !== '' && !game.noReopenSeats.includes(p.seat)) {
          game.noReopenSeats.push(p.seat);
        }
        p.action = '';
      }
    }
  }
}

// ─── Deal steps ───────────────────────────────────────────────────────

function dealFlop(game, players) {
  game.communityCards = [...game.communityCards, ...deal(game.deck, 3)];
  game.move = getNextActingSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.FLOP_BETTING_ROUND;
  return { game, players };
}

function dealTurn(game, players) {
  game.communityCards = [...game.communityCards, ...deal(game.deck, 1)];
  game.move = getNextActingSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.TURN_BETTING_ROUND;
  return { game, players };
}

function dealRiver(game, players) {
  game.communityCards = [...game.communityCards, ...deal(game.deck, 1)];
  game.move = getNextActingSeat(players, game.dealerSeat, game.maxSeats);
  game.handStep = GAME_HAND.RIVER_BETTING_ROUND;
  return { game, players };
}

function afterRiverBettingRound(game, players) {
  game.handStep = GAME_HAND.FIND_WINNERS;
  return { game, players };
}

// ─── Showdown & payout ────────────────────────────────────────────────

/** Step 13: Tag winners for display and store best 5-card hands for all players. */
function findHandWinners(game, players) {
  const inHand = getPlayersInHand(players);

  if (inHand.length === 1) {
    inHand[0].handRank = 'Last player standing';
    inHand[0].bestHand = inHand[0].cards; // Just show their hole cards
    game.winners = [{ seat: inHand[0].seat, playerId: inHand[0].playerId }];
    game.handStep = GAME_HAND.PAY_WINNERS;
    return { game, players };
  }

  const hands = inHand.map((p) => ({
    playerId: p.playerId,
    seat: p.seat,
    cards: [...p.cards, ...game.communityCards],
  }));

  // Evaluate ALL hands and get best 5 cards for each player
  const evaluated = evaluateAllHands(hands);

  for (const result of evaluated) {
    const player = players.find((p) => p.seat === result.seat);
    if (player) {
      player.handRank = result.descr;
      player.bestHand = result.bestHand;
      player.isWinner = result.isWinner;
    }
  }

  const winners = evaluated.filter((e) => e.isWinner);
  game.winners = winners.map((w) => ({ seat: w.seat, playerId: w.playerId }));
  game.handStep = GAME_HAND.PAY_WINNERS;
  return { game, players };
}

/** Step 14: Distribute pots (main + side) to per-pot winners. */
function payWinners(game, players) {
  const inHand = getPlayersInHand(players);

  // Single player left — give them everything
  if (inHand.length <= 1 && inHand.length > 0) {
    const winner = inHand[0];
    winner.stack = toMoney(winner.stack + game.pot);
    winner.winnings = game.pot;
    if (!winner.handRank) winner.handRank = 'Last player standing';
    game.winners = [{ seat: winner.seat, playerId: winner.playerId }];
    game.pot = 0;
    game.handStep = GAME_HAND.RECORD_STATS_AND_NEW_HAND;
    return { game, players };
  }

  // Showdown: evaluate per-pot
  const pots = calculatePots(players);
  const allWinnerSeats = new Set();

  for (const pot of pots) {
    const eligible = pot.eligible
      .map((seat) => players.find((p) => p.seat === seat))
      .filter((p) => p && p.cards && p.cards.length > 0);

    if (eligible.length === 0) continue;

    let potWinnerSeats;
    if (eligible.length === 1) {
      potWinnerSeats = [eligible[0].seat];
    } else {
      const hands = eligible.map((p) => ({
        playerId: p.playerId,
        seat: p.seat,
        cards: [...p.cards, ...game.communityCards],
      }));
      const winners = findWinners(hands);
      potWinnerSeats = winners.map((w) => w.seat);
      for (const w of winners) {
        const pl = players.find((p) => p.seat === w.seat);
        if (pl && !pl.handRank) pl.handRank = w.descr;
      }
    }

    // Distribute with deterministic remainder
    const count = potWinnerSeats.length;
    const baseShare = Math.floor((pot.amount * 100) / count) / 100;
    let remainder = toMoney(pot.amount - toMoney(baseShare * count));

    for (const seat of potWinnerSeats) {
      let share = baseShare;
      if (remainder >= 0.01) {
        share = toMoney(share + 0.01);
        remainder = toMoney(remainder - 0.01);
      }
      const pl = players.find((p) => p.seat === seat);
      if (pl) {
        pl.stack = toMoney(pl.stack + share);
        pl.winnings = toMoney((pl.winnings || 0) + share);
        allWinnerSeats.add(seat);
      }
    }
  }

  game.winners = [...allWinnerSeats].map((seat) => {
    const p = players.find((pl) => pl.seat === seat);
    return { seat, playerId: p ? p.playerId : null };
  });

  game.pot = 0;
  game.handStep = GAME_HAND.RECORD_STATS_AND_NEW_HAND;
  return { game, players };
}

/** Step 15: Mark hand complete. */
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
  gamePrep,
  setupDealer,
  setupSmallBlind,
  setupBigBlind,
  dealCards,
  bettingRound,
  applyAction,
  dealFlop,
  dealTurn,
  dealRiver,
  findHandWinners,
  payWinners,
  recordStatsAndNewHand,
};
