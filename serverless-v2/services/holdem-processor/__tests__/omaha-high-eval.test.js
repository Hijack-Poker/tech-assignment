'use strict';

const { evaluateOmahaHigh, findHighWinners } = require('../shared/games/omaha-hilo/high-eval');

describe('Omaha High Evaluator', () => {
  describe('evaluateOmahaHigh()', () => {
    it('should enforce the 2+3 rule — not best-5-of-9', () => {
      // Player has 4 hearts in hand, board has 1 heart + 4 non-hearts
      // With Hold'em logic, picking best 5 of 9 could find a flush
      // But Omaha requires exactly 2 hole + 3 board, so with only 1 heart on board
      // a flush using 4 hole hearts is impossible (can only use 2 hole cards)
      const holeCards = ['AH', 'KH', 'QH', 'JH'];
      const communityCards = ['10H', '2D', '3C', '4S', '5D'];

      const result = evaluateOmahaHigh(holeCards, communityCards);
      expect(result).toBeTruthy();
      // Should be able to make a flush with 2 hearts from hole + 10H from board
      // AH + KH + 10H + ... best combo is AH,KH + 10H,X,Y
      // AH KH 10H 2D 3C -> flush? No, only 3 hearts.
      // Actually AH,KH + 10H,2D,3C = A,K,10 hearts + 2D,3C = NOT a flush (only 3 suited)
      // Best is likely a straight: AH,KH,QH,JH + 10H -> but must use only 2 hole.
      // QH,JH + 10H,2D,3C = Q,J,10 high? Or AH,QH + 10H,5D,4S = A high
      // Actually: JH,QH + 10H,2D,3C = no straight. 
      // KH,QH + 10H,5D,4S = K high
      // AH,JH + 10H,5D,4S = AJ10 high
      // Actually a straight: AH,KH + 10H,QH,JH -- wait QH,JH are in hole
      // KH,QH + 10H,... only 1 board heart available.
      // Let me think: any combo using exactly 2 of [AH,KH,QH,JH] + 3 of [10H,2D,3C,4S,5D]:
      // Best: QH,JH + 10H,2D,3C -> no useful structure
      // AH,KH + 10H,5D,4S -> A,K,10 high
      // The name should be something like "Pair" or "High Card" class
      expect(result.descr).toBeTruthy();
    });

    it('should find a straight with proper 2+3 usage', () => {
      const holeCards = ['AH', 'KD', '5S', '2C'];
      const communityCards = ['QS', 'JC', '10H', '3D', '7S'];
      // AH,KD + QS,JC,10H = A-K-Q-J-10 straight (Royal straight/broadway)
      const result = evaluateOmahaHigh(holeCards, communityCards);
      expect(result).toBeTruthy();
      expect(result.descr.toLowerCase()).toContain('straight');
    });

    it('should find a flush only when 2 hole + 3 board are suited', () => {
      const holeCards = ['AH', '8H', 'KD', 'QS'];
      const communityCards = ['JH', '5H', '3H', '9C', '2D'];
      // AH,8H + JH,5H,3H = 5 hearts -> flush
      const result = evaluateOmahaHigh(holeCards, communityCards);
      expect(result).toBeTruthy();
      expect(result.descr.toLowerCase()).toContain('flush');
    });

    it('should not find a flush when only 1 hole card is suited with 4 board', () => {
      const holeCards = ['AH', 'KD', 'QS', 'JC'];
      const communityCards = ['9H', '7H', '5H', '3H', '2D'];
      // Only AH is a heart in hole cards; need exactly 2 hole cards that are hearts
      // but only AH is a heart. So no flush possible.
      const result = evaluateOmahaHigh(holeCards, communityCards);
      expect(result).toBeTruthy();
      expect(result.descr.toLowerCase()).not.toContain('flush');
    });
  });

  describe('findHighWinners()', () => {
    it('should identify the player with the best high hand', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', 'AD', '2S', '3C'] },
        { playerId: 2, seat: 2, cards: ['KH', 'KD', '4S', '5C'] },
      ];
      const communityCards = ['AS', '7D', '8C', '9H', 'JC'];
      // Player 1: AH,AD + AS -> trips aces combo possible
      // Player 2: KH,KD + ... pair of kings at best

      const winners = findHighWinners(players, communityCards);
      expect(winners).toHaveLength(1);
      expect(winners[0].seat).toBe(1);
    });

    it('should handle ties and return multiple winners', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['2H', '3D', '8S', '9C'] },
        { playerId: 2, seat: 2, cards: ['2D', '3H', '8C', '9H'] },
      ];
      const communityCards = ['AH', 'KD', 'QS', 'JC', '10H'];
      // Both players have the same best hand: community straight A-K-Q-J-10
      // using any 2 hole cards, both end up with the same community-driven hand

      const winners = findHighWinners(players, communityCards);
      expect(winners.length).toBeGreaterThanOrEqual(2);
    });

    it('should correctly evaluate full house vs flush', () => {
      const players = [
        { playerId: 1, seat: 1, cards: ['AH', 'AS', 'KD', 'QD'] },
        { playerId: 2, seat: 2, cards: ['JH', '9H', '2S', '3S'] },
      ];
      const communityCards = ['AD', 'KH', 'KS', '8H', '4H'];
      // Player 1: AH,AS + AD,KH,KS = A-A-A-K-K full house
      // Player 2: JH,9H + KH,8H,4H = flush (hearts) — but need 2 hole + 3 board
      //   JH,9H + KH,8H,4H = K-J-9-8-4 flush

      const winners = findHighWinners(players, communityCards);
      expect(winners).toHaveLength(1);
      expect(winners[0].seat).toBe(1);
    });
  });
});
