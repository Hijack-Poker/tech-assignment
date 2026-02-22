'use strict';

const { Router } = require('express');
const router = Router();

/**
 * GET /api/v1/player/rewards
 *
 * Get a player's rewards summary. Candidates implement this.
 *
 * Uses playerId from auth middleware (req.playerId).
 *
 * Expected response:
 *   { playerId, tier, points, nextTierAt, recentTransactions: [...] }
 */
router.get('/rewards', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement player rewards lookup here. See challenge docs for requirements.',
    hint: {
      playerId: req.playerId,
      output: {
        playerId: 'string',
        tier: 'string',
        points: 'number',
        nextTierAt: 'number',
        recentTransactions: 'array',
      },
    },
  });
});

/**
 * GET /api/v1/player/history
 *
 * Get a player's point transaction history. Candidates implement this.
 *
 * Expected query: ?limit=20&offset=0
 * Expected response:
 *   { transactions: [{ timestamp, points, reason, balance }], total }
 */
router.get('/history', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement transaction history here. See challenge docs for requirements.',
  });
});

module.exports = router;
