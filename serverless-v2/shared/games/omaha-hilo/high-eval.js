'use strict';

const { Hand } = require('pokersolver');
const { getOmahaCombos } = require('./combinations');

/**
 * Convert our card format ("AH", "10D") to pokersolver format ("Ah", "Td").
 */
function toPokersolver(card) {
  const suit = card.slice(-1).toLowerCase();
  let rank = card.slice(0, -1);
  if (rank === '10') rank = 'T';
  return rank + suit;
}

/**
 * Evaluate a player's best Omaha high hand.
 * Enforces the "exactly 2 hole + 3 community" rule by enumerating all 60 combos.
 *
 * @param {string[]} holeCards 4 hole cards
 * @param {string[]} communityCards 5 community cards
 * @returns {{ descr: string, rank: number, name: string, solved: object }|null}
 */
function evaluateOmahaHigh(holeCards, communityCards) {
  const combos = getOmahaCombos(holeCards, communityCards);
  let bestHand = null;
  let bestSolved = null;

  for (const combo of combos) {
    const converted = combo.hand.map(toPokersolver);
    const solved = Hand.solve(converted);

    if (!bestSolved || solved.rank > bestSolved.rank) {
      bestSolved = solved;
      bestHand = combo;
    } else if (solved.rank === bestSolved.rank) {
      const cmp = solved.compare(bestSolved);
      if (cmp < 0) {
        bestSolved = solved;
        bestHand = combo;
      }
    }
  }

  if (!bestSolved) return null;

  return {
    descr: bestSolved.descr,
    rank: bestSolved.rank,
    name: bestSolved.name,
    solved: bestSolved,
  };
}

/**
 * Find the high-hand winner(s) among multiple players using Omaha evaluation.
 *
 * @param {Array<{playerId, seat, cards: string[]}>} players Players still in hand
 * @param {string[]} communityCards 5 community cards
 * @returns {Array<{playerId, seat, descr, rank, name}>} Winner(s)
 */
function findHighWinners(players, communityCards) {
  const evaluated = players.map((p) => {
    const result = evaluateOmahaHigh(p.cards, communityCards);
    return {
      playerId: p.playerId,
      seat: p.seat,
      ...result,
    };
  });

  const solvedHands = evaluated.map((e) => e.solved);
  const winners = Hand.winners(solvedHands);
  const winnerSet = new Set(winners);

  return evaluated
    .filter((e) => winnerSet.has(e.solved))
    .map((e) => ({
      playerId: e.playerId,
      seat: e.seat,
      descr: e.descr,
      rank: e.rank,
      name: e.name,
    }));
}

module.exports = { evaluateOmahaHigh, findHighWinners, toPokersolver };
