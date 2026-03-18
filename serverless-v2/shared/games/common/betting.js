'use strict';

const { PLAYER_STATUS, ACTION } = require('./constants');
const { toMoney } = require('../../utils');

/**
 * A betting round is complete when every ACTIVE player has acted
 * (action !== '') and their bet matches the current bet.
 * All-in and folded players are excluded from this check.
 */
function isBettingRoundComplete(players, currentBet) {
  const active = players.filter((p) => p.status === PLAYER_STATUS.ACTIVE);
  if (active.length === 0) return true;
  return active.every((p) => p.action !== '' && p.bet >= currentBet);
}

/**
 * Collect bets into the pot and reset per-street state.
 */
function collectBets(game, players) {
  let collected = 0;
  for (const player of players) {
    collected = toMoney(collected + player.bet);
    player.bet = 0;
    player.action = '';
  }
  game.pot = toMoney(game.pot + collected);
  game.currentBet = 0;
  game.lastRaiseSize = game.bigBlind;
  game.noReopenSeats = [];
  return { game, players, collected };
}

/**
 * Minimum legal raise-to amount = currentBet + lastRaiseSize.
 */
function getMinRaiseTo(game) {
  return toMoney(game.currentBet + (game.lastRaiseSize || game.bigBlind));
}

/**
 * Get the set of legal actions for a player given the current game state.
 */
function getValidActions(player, game) {
  if (player.status !== PLAYER_STATUS.ACTIVE) return [];

  const actions = [ACTION.FOLD];
  const toCall = Math.max(0, toMoney(game.currentBet - player.bet));
  const canRaise = !(game.noReopenSeats || []).includes(player.seat);
  const lastRaise = game.lastRaiseSize || game.bigBlind;

  if (toCall === 0) {
    actions.push(ACTION.CHECK);
  } else {
    actions.push(ACTION.CALL);
  }

  // Full raise/bet option
  if (canRaise && player.stack > 0) {
    if (game.currentBet === 0) {
      // Opening bet (postflop, no bet yet)
      if (player.stack >= game.bigBlind) {
        actions.push(ACTION.BET);
      }
    } else {
      // Raise over existing bet
      const minRaiseTotal = toMoney(game.currentBet + lastRaise);
      const chipsNeeded = toMoney(minRaiseTotal - player.bet);
      if (player.stack >= chipsNeeded) {
        actions.push(ACTION.RAISE);
      }
    }
  }

  // All-in: always available if stack > 0, unless reopen is blocked and
  // going all-in would exceed the current bet (which is effectively a raise).
  if (player.stack > 0) {
    const allInBet = toMoney(player.bet + player.stack);
    if (canRaise || allInBet <= game.currentBet) {
      actions.push(ACTION.ALLIN);
    }
  }

  return actions;
}

/**
 * Validate a requested action. Returns { valid, action, amount } or
 * { valid: false, reason, message }.
 *
 * Amount conventions:
 *   BET:   amount = total bet size (chips from stack).
 *   RAISE: amount = raise-TO target (player.bet will become this value).
 *          If 0 or omitted, defaults to minimum legal raise.
 */
function validateAction(player, game, requestedAction) {
  if (!requestedAction || !requestedAction.action) {
    return { valid: false, reason: 'NO_ACTION', message: 'No action provided' };
  }

  let action = String(requestedAction.action).toLowerCase();
  const toCall = Math.max(0, toMoney(game.currentBet - player.bet));

  // Normalize common mismatches
  if (action === ACTION.CALL && toCall === 0) action = ACTION.CHECK;
  if (action === ACTION.BET && game.currentBet > 0) action = ACTION.RAISE;
  if (action === ACTION.RAISE && game.currentBet === 0) action = ACTION.BET;

  const validActions = getValidActions(player, game);
  if (!validActions.includes(action)) {
    return {
      valid: false,
      reason: 'ILLEGAL_ACTION',
      message: `Action '${action}' is not legal. Valid: ${validActions.join(', ')}`,
    };
  }

  switch (action) {
    case ACTION.FOLD:
    case ACTION.CHECK:
    case ACTION.CALL:
    case ACTION.ALLIN:
      return { valid: true, action, amount: 0 };

    case ACTION.BET: {
      const amount = Number(requestedAction.amount || 0);
      const minBet = game.bigBlind;
      const desiredBet = amount > 0 ? amount : minBet;
      // Allow under-min only when it's an all-in
      if (desiredBet < minBet && desiredBet < player.stack) {
        return { valid: false, reason: 'BET_TOO_SMALL', message: `Min bet is ${minBet}` };
      }
      return { valid: true, action, amount: Math.min(desiredBet, player.stack) };
    }

    case ACTION.RAISE: {
      const amount = Number(requestedAction.amount || 0);
      const minRaiseTo = getMinRaiseTo(game);
      const maxRaiseTo = toMoney(player.bet + player.stack);
      const raiseToTarget = amount > 0 ? amount : minRaiseTo;

      // Under-min is only legal as an all-in
      if (raiseToTarget < minRaiseTo && maxRaiseTo >= minRaiseTo) {
        return {
          valid: false,
          reason: 'RAISE_TOO_SMALL',
          message: `Min raise to ${minRaiseTo}`,
        };
      }
      return { valid: true, action, amount: Math.min(raiseToTarget, maxRaiseTo) };
    }

    default:
      return { valid: false, reason: 'UNKNOWN_ACTION', message: `Unknown: ${action}` };
  }
}

module.exports = {
  isBettingRoundComplete,
  collectBets,
  getMinRaiseTo,
  getValidActions,
  validateAction,
};
