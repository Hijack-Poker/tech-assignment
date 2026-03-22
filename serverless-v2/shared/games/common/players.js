'use strict';

const { PLAYER_STATUS } = require('./constants');

/**
 * Player is still in the hand (active or all-in, not folded/sitting-out/busted).
 */
function isInHand(player) {
  return (
    player.status === PLAYER_STATUS.ACTIVE ||
    player.status === PLAYER_STATUS.ALL_IN
  );
}

/**
 * Player can take a betting action (active and not all-in).
 */
function canAct(player) {
  return player.status === PLAYER_STATUS.ACTIVE;
}

function isFolded(player) {
  return player.status === PLAYER_STATUS.FOLDED;
}

function isAllIn(player) {
  return player.status === PLAYER_STATUS.ALL_IN;
}

function isSittingOut(player) {
  return player.status === PLAYER_STATUS.SITTING_OUT;
}

function isBusted(player) {
  return player.status === PLAYER_STATUS.BUSTED;
}

/**
 * All players still in the hand (active + all-in).
 */
function getPlayersInHand(players) {
  return players.filter(isInHand);
}

/**
 * Number of players who can still take actions (active only, not all-in).
 */
function getActingPlayerCount(players) {
  return players.filter(canAct).length;
}

function getPlayerBySeat(players, seat) {
  return players.find((p) => p.seat === seat) || null;
}

/**
 * Next occupied seat of a player IN HAND (active or all-in).
 * Used for dealer/blind rotation where all-in players still count.
 */
function getNextPlayerSeat(players, currentSeat, maxSeats) {
  const inHand = getPlayersInHand(players);
  if (inHand.length === 0) return -1;

  for (let i = 1; i <= maxSeats; i++) {
    const seat = ((currentSeat - 1 + i) % maxSeats) + 1;
    if (inHand.find((p) => p.seat === seat)) return seat;
  }
  return -1;
}

/**
 * Next seat of an ACTIVE player (can act — not all-in, not folded).
 * Used for betting turn progression.
 */
function getNextActingSeat(players, currentSeat, maxSeats) {
  for (let i = 1; i <= maxSeats; i++) {
    const seat = ((currentSeat - 1 + i) % maxSeats) + 1;
    const player = players.find((p) => p.seat === seat);
    if (player && canAct(player)) return seat;
  }
  return -1;
}

// Legacy alias
const getNextSeat = getNextActingSeat;
const isActive = isInHand;
const getActivePlayers = getPlayersInHand;

module.exports = {
  isInHand,
  canAct,
  isActive,
  isFolded,
  isAllIn,
  isSittingOut,
  isBusted,
  getActivePlayers,
  getPlayersInHand,
  getActingPlayerCount,
  getPlayerBySeat,
  getNextPlayerSeat,
  getNextActingSeat,
  getNextSeat,
};
