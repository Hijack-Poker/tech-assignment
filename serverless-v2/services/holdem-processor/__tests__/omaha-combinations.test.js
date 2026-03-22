'use strict';

const { choose, getOmahaCombos } = require('../shared/games/omaha-hilo/combinations');

describe('Omaha Combinations', () => {
  describe('choose()', () => {
    it('should generate C(4,2) = 6 combinations', () => {
      const result = choose(['A', 'B', 'C', 'D'], 2);
      expect(result).toHaveLength(6);
    });

    it('should generate C(5,3) = 10 combinations', () => {
      const result = choose(['A', 'B', 'C', 'D', 'E'], 3);
      expect(result).toHaveLength(10);
    });

    it('should return single element for C(n,n)', () => {
      const result = choose([1, 2, 3], 3);
      expect(result).toHaveLength(1);
      expect(result[0]).toEqual([1, 2, 3]);
    });

    it('should return n single-element arrays for C(n,1)', () => {
      const result = choose([1, 2, 3, 4], 1);
      expect(result).toHaveLength(4);
      expect(result).toEqual([[1], [2], [3], [4]]);
    });
  });

  describe('getOmahaCombos()', () => {
    const holeCards = ['AH', 'KD', 'QS', 'JC'];
    const communityCards = ['10H', '9D', '8C', '7S', '6H'];

    it('should return exactly 60 combinations (C(4,2) x C(5,3))', () => {
      const combos = getOmahaCombos(holeCards, communityCards);
      expect(combos).toHaveLength(60);
    });

    it('each combo should have exactly 2 hole cards and 3 community cards', () => {
      const combos = getOmahaCombos(holeCards, communityCards);
      for (const combo of combos) {
        expect(combo.hole).toHaveLength(2);
        expect(combo.community).toHaveLength(3);
        expect(combo.hand).toHaveLength(5);

        for (const c of combo.hole) {
          expect(holeCards).toContain(c);
        }
        for (const c of combo.community) {
          expect(communityCards).toContain(c);
        }
      }
    });

    it('should produce no duplicate combinations', () => {
      const combos = getOmahaCombos(holeCards, communityCards);
      const handStrings = combos.map((c) => c.hand.sort().join(','));
      const unique = new Set(handStrings);
      expect(unique.size).toBe(60);
    });

    it('each hand should be the concatenation of hole + community', () => {
      const combos = getOmahaCombos(holeCards, communityCards);
      for (const combo of combos) {
        expect(combo.hand).toEqual([...combo.hole, ...combo.community]);
      }
    });
  });
});
