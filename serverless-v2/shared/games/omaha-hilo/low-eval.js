'use strict';

const { getOmahaCombos } = require('./combinations');

/**
 * Map card rank string to a numeric value for low evaluation.
 * Ace = 1 (plays low), 2-8 = face value, 9+ = disqualified (returns Infinity).
 */
const LOW_RANK_MAP = {
  'A': 1, '2': 2, '3': 3, '4': 4, '5': 5,
  '6': 6, '7': 7, '8': 8,
  '9': Infinity, '10': Infinity, 'J': Infinity, 'Q': Infinity, 'K': Infinity,
};

/**
 * Extract the rank portion of a card string (e.g. "AH" -> "A", "10D" -> "10").
 */
function getCardRank(card) {
  return card.slice(0, -1);
}

/**
 * Evaluate a 5-card hand for low qualification (8-or-better).
 * Returns sorted rank array (descending) if qualifying, or null.
 *
 * @param {string[]} hand 5 cards
 * @returns {number[]|null} Sorted ranks descending (e.g. [7,5,4,3,1]) or null
 */
function evaluateLowHand(hand) {
  const ranks = hand.map((card) => LOW_RANK_MAP[getCardRank(card)]);

  if (ranks.some((r) => r === Infinity)) return null;

  const unique = new Set(ranks);
  if (unique.size !== 5) return null;

  return ranks.sort((a, b) => b - a);
}

/**
 * Compare two low hands. Lower is better.
 * Returns negative if a is better, positive if b is better, 0 if tied.
 */
function compareLow(a, b) {
  for (let i = 0; i < 5; i++) {
    if (a[i] !== b[i]) return a[i] - b[i];
  }
  return 0;
}

/**
 * Format a low hand for display (e.g. [8,5,4,3,1] -> "8-5-4-3-A").
 */
function formatLowHand(ranks) {
  return ranks.map((r) => (r === 1 ? 'A' : String(r))).join('-');
}

/**
 * Evaluate a player's best Omaha low hand.
 * Enforces "exactly 2 hole + 3 community" and 8-or-better qualification.
 *
 * @param {string[]} holeCards 4 hole cards
 * @param {string[]} communityCards 5 community cards
 * @returns {{ ranks: number[], descr: string }|null} Best low hand or null
 */
function evaluateOmahaLow(holeCards, communityCards) {
  const combos = getOmahaCombos(holeCards, communityCards);
  let bestLow = null;

  for (const combo of combos) {
    const low = evaluateLowHand(combo.hand);
    if (!low) continue;

    if (!bestLow || compareLow(low, bestLow) < 0) {
      bestLow = low;
    }
  }

  if (!bestLow) return null;

  return {
    ranks: bestLow,
    descr: formatLowHand(bestLow),
  };
}

/**
 * Find the low-hand winner(s) among multiple players.
 * Returns empty array if no qualifying low exists.
 *
 * @param {Array<{playerId, seat, cards: string[]}>} players Players still in hand
 * @param {string[]} communityCards 5 community cards
 * @returns {Array<{playerId, seat, descr, ranks: number[]}>} Low winner(s)
 */
function findLowWinners(players, communityCards) {
  const evaluated = [];

  for (const p of players) {
    const result = evaluateOmahaLow(p.cards, communityCards);
    if (result) {
      evaluated.push({
        playerId: p.playerId,
        seat: p.seat,
        descr: result.descr,
        ranks: result.ranks,
      });
    }
  }

  if (evaluated.length === 0) return [];

  evaluated.sort((a, b) => compareLow(a.ranks, b.ranks));
  const bestRanks = evaluated[0].ranks;

  return evaluated.filter((e) => compareLow(e.ranks, bestRanks) === 0);
}

module.exports = {
  evaluateOmahaLow,
  findLowWinners,
  evaluateLowHand,
  compareLow,
  formatLowHand,
};
