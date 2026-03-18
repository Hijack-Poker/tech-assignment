'use strict';

const { PLAYER_STATUS } = require('./constants');
const { toMoney } = require('../../utils');

/**
 * Build main pot + side pots from cumulative player contributions (totalBet).
 *
 * Each pot has { amount, eligible: [seat, ...] }.
 * Eligible = contributed at least that tier AND not folded.
 * Folded players' chips still go into the pot.
 */
function calculatePots(players) {
  const contributors = players
    .filter((p) => p.totalBet > 0)
    .sort((a, b) => a.totalBet - b.totalBet);

  if (contributors.length === 0) return [];

  const betLevels = [...new Set(contributors.map((p) => p.totalBet))].sort(
    (a, b) => a - b
  );

  const pots = [];
  let previousLevel = 0;

  for (const level of betLevels) {
    const levelAmount = toMoney(level - previousLevel);
    if (levelAmount <= 0) continue;

    // Everyone who contributed at or above this level pays levelAmount into this pot
    const payingPlayers = contributors.filter((p) => p.totalBet >= level);
    const potAmount = toMoney(levelAmount * payingPlayers.length);

    // Only non-folded players at this level are eligible to win
    const eligible = payingPlayers
      .filter((p) => p.status !== PLAYER_STATUS.FOLDED)
      .map((p) => p.seat);

    if (potAmount > 0 && eligible.length > 0) {
      pots.push({ amount: potAmount, eligible });
    } else if (potAmount > 0 && eligible.length === 0) {
      // All eligible players folded at this tier; merge into next pot
      // or give to the broadest eligible set still alive
      if (pots.length > 0) {
        pots[pots.length - 1].amount = toMoney(
          pots[pots.length - 1].amount + potAmount
        );
      }
    }

    previousLevel = level;
  }

  return pots;
}

/**
 * Sum all bets into a single pot value.
 */
function calculateSimplePot(players) {
  return toMoney(players.reduce((sum, p) => sum + (p.totalBet || 0), 0));
}

/**
 * Distribute pot winnings to winners with deterministic remainder handling.
 * No chips are ever lost.
 *
 * @param {Array} pots - [{ amount, eligible }]
 * @param {Array} winnerSeats - seats that won
 * @returns {Object} payouts - { [seat]: amount }
 */
function distributePots(pots, winnerSeats) {
  const payouts = {};

  for (const pot of pots) {
    const eligibleWinners = winnerSeats.filter((s) => pot.eligible.includes(s));
    if (eligibleWinners.length === 0) continue;

    const count = eligibleWinners.length;
    const baseShare = Math.floor((pot.amount * 100) / count) / 100;
    let remainder = toMoney(pot.amount - toMoney(baseShare * count));

    for (const seat of eligibleWinners) {
      let share = baseShare;
      if (remainder >= 0.01) {
        share = toMoney(share + 0.01);
        remainder = toMoney(remainder - 0.01);
      }
      payouts[seat] = toMoney((payouts[seat] || 0) + share);
    }
  }

  return payouts;
}

module.exports = {
  calculatePots,
  calculateSimplePot,
  distributePots,
};
