'use strict';

const {
  evaluateOmahaLow,
  findLowWinners,
  evaluateLowHand,
  compareLow,
  formatLowHand,
} = require('../shared/games/omaha-hilo/low-eval');

describe('Omaha Low Evaluator', () => {
  describe('evaluateLowHand()', () => {
    it('should return sorted ranks for a qualifying hand', () => {
      const result = evaluateLowHand(['AH', '2D', '3S', '5C', '7H']);
      expect(result).toEqual([7, 5, 3, 2, 1]);
    });

    it('should return null when a card rank exceeds 8', () => {
      const result = evaluateLowHand(['AH', '2D', '3S', '5C', '9H']);
      expect(result).toBeNull();
    });

    it('should return null when there is a pair', () => {
      const result = evaluateLowHand(['AH', '2D', '2S', '5C', '7H']);
      expect(result).toBeNull();
    });

    it('should qualify the best possible low (wheel): A-2-3-4-5', () => {
      const result = evaluateLowHand(['AH', '2D', '3S', '4C', '5H']);
      expect(result).toEqual([5, 4, 3, 2, 1]);
    });

    it('should qualify the worst possible low: 4-5-6-7-8', () => {
      const result = evaluateLowHand(['4H', '5D', '6S', '7C', '8H']);
      expect(result).toEqual([8, 7, 6, 5, 4]);
    });
  });

  describe('compareLow()', () => {
    it('should rank 7-5-4-3-A as better than 8-4-3-2-A', () => {
      expect(compareLow([7, 5, 4, 3, 1], [8, 4, 3, 2, 1])).toBeLessThan(0);
    });

    it('should return 0 for identical low hands', () => {
      expect(compareLow([7, 5, 4, 3, 1], [7, 5, 4, 3, 1])).toBe(0);
    });

    it('should compare second card when first cards tie', () => {
      expect(compareLow([8, 5, 4, 3, 1], [8, 6, 4, 3, 1])).toBeLessThan(0);
    });

    it('should compare all the way down to the fifth card', () => {
      expect(compareLow([8, 7, 6, 5, 2], [8, 7, 6, 5, 3])).toBeLessThan(0);
    });
  });

  describe('formatLowHand()', () => {
    it('should format with A for ace (rank 1)', () => {
      expect(formatLowHand([7, 5, 4, 3, 1])).toBe('7-5-4-3-A');
    });

    it('should format numeric ranks normally', () => {
      expect(formatLowHand([8, 6, 5, 4, 2])).toBe('8-6-5-4-2');
    });
  });

  describe('evaluateOmahaLow()', () => {
    it('should find the best qualifying low from 60 combos', () => {
      const holeCards = ['AH', '4C', 'KH', 'JD'];
      const communityCards = ['2H', '3D', '7S', 'KC', 'QD'];
      // Best low: AH,4C + 2H,3D,7S = A-2-3-4-7
      const result = evaluateOmahaLow(holeCards, communityCards);
      expect(result).toBeTruthy();
      expect(result.ranks).toEqual([7, 4, 3, 2, 1]);
      expect(result.descr).toBe('7-4-3-2-A');
    });

    it('should return null when board has no 3 qualifying low cards', () => {
      const holeCards = ['AH', '2D', '3S', '4C'];
      const communityCards = ['9H', '10D', 'JC', 'QS', 'KD'];
      // Board has 0 cards ≤ 8
      const result = evaluateOmahaLow(holeCards, communityCards);
      expect(result).toBeNull();
    });

    it('should return null when only 2 qualifying board cards exist', () => {
      const holeCards = ['AH', '2D', '3S', '4C'];
      const communityCards = ['5H', '6D', 'JC', 'QS', 'KD'];
      // Board has 5H, 6D = only 2 cards ≤ 8. Need 3 from board.
      // Combos: 2 hole + 3 board. 3 board cards always include JC or QS or KD
      // So at least one board card >8 in every combo → no qualifying low
      // Wait: 5H,6D are 2 qualifiers. We need 3 board cards, but only 2 qualify.
      // Any combo of 3 from [5H,6D,JC,QS,KD] includes at least 1 of J/Q/K
      const result = evaluateOmahaLow(holeCards, communityCards);
      expect(result).toBeNull();
    });

    it('should handle board with exactly 3 qualifying cards', () => {
      const holeCards = ['AH', '4D', 'KH', 'QC'];
      const communityCards = ['2H', '5S', '8D', 'JC', 'KD'];
      // Only valid board triple for low: 2H,5S,8D
      // Need 2 hole ≤ 8: AH(=1),4D. Combo: AH,4D + 2H,5S,8D = 1,4,2,5,8 = [8,5,4,2,1]
      const result = evaluateOmahaLow(holeCards, communityCards);
      expect(result).toBeTruthy();
      expect(result.ranks).toEqual([8, 5, 4, 2, 1]);
    });
  });

  describe('findLowWinners()', () => {
    it('should return the player with the best low', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', '2D', 'KS', 'QC'] },
        { playerId: 2, seat: 2, cards: ['4H', '5D', 'KH', 'QD'] },
      ];
      const communityCards = ['3H', '6S', '8D', 'JC', 'KD'];
      // Player 1: AH,2D + 3H,6S,8D = [8,6,3,2,1] = "8-6-3-2-A"
      // Player 2: 4H,5D + 3H,6S,8D = [8,6,5,4,3] = "8-6-5-4-3"
      // Player 1 wins: 8-6-3 < 8-6-5

      const winners = findLowWinners(players, communityCards);
      expect(winners).toHaveLength(1);
      expect(winners[0].seat).toBe(1);
    });

    it('should return empty array when no qualifying low exists', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', '2D', '3S', '4C'] },
        { playerId: 2, seat: 2, cards: ['5H', '6D', '7S', '8C'] },
      ];
      const communityCards = ['9H', '10D', 'JC', 'QS', 'KD'];

      const winners = findLowWinners(players, communityCards);
      expect(winners).toHaveLength(0);
    });

    it('should handle tie in low hands — both players win', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', '2D', 'KS', 'QC'] },
        { playerId: 2, seat: 2, cards: ['AD', '2H', 'KH', 'QD'] },
      ];
      const communityCards = ['3H', '5S', '7D', 'JC', 'KD'];
      // Both: A,2 + 3,5,7 = [7,5,3,2,1]

      const winners = findLowWinners(players, communityCards);
      expect(winners).toHaveLength(2);
    });

    it('should only award low to qualifying players', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', '2D', 'KS', 'QC'] },
        { playerId: 2, seat: 2, cards: ['KH', 'QD', 'JH', '10C'] },
      ];
      const communityCards = ['3H', '5S', '7D', 'JC', 'KD'];
      // Player 1: AH,2D + 3H,5S,7D = [7,5,3,2,1] — qualifies
      // Player 2: no 2 hole cards ≤ 8 (J,10,K,Q) with the board low triple

      const winners = findLowWinners(players, communityCards);
      expect(winners).toHaveLength(1);
      expect(winners[0].seat).toBe(1);
    });
  });
});
