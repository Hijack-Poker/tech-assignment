'use strict';

/**
 * Generate all k-element combinations from an array.
 * @param {Array} arr Source array
 * @param {number} k Combination size
 * @returns {Array<Array>} All C(n,k) combinations
 */
function choose(arr, k) {
  const results = [];

  function recurse(start, combo) {
    if (combo.length === k) {
      results.push([...combo]);
      return;
    }
    for (let i = start; i <= arr.length - (k - combo.length); i++) {
      combo.push(arr[i]);
      recurse(i + 1, combo);
      combo.pop();
    }
  }

  recurse(0, []);
  return results;
}

/**
 * Generate all valid Omaha 5-card hands: exactly 2 hole cards + 3 community cards.
 * Returns C(4,2) x C(5,3) = 6 x 10 = 60 combinations.
 *
 * @param {string[]} holeCards 4 hole cards (e.g. ["AH","KD","QS","JC"])
 * @param {string[]} communityCards 5 community cards
 * @returns {Array<{hole: string[], community: string[], hand: string[]}>}
 */
function getOmahaCombos(holeCards, communityCards) {
  const holePairs = choose(holeCards, 2);
  const boardTriples = choose(communityCards, 3);
  const combos = [];

  for (const hole of holePairs) {
    for (const board of boardTriples) {
      combos.push({
        hole,
        community: board,
        hand: [...hole, ...board],
      });
    }
  }

  return combos;
}

module.exports = { choose, getOmahaCombos };
