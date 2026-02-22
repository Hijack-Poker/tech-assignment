'use strict';

const { Router } = require('express');
const router = Router();

/**
 * POST /api/v1/points/award
 *
 * Award points to a player. Candidates implement this.
 *
 * Expected body:
 *   { playerId: string, points: number, reason: string }
 *
 * Expected response:
 *   { playerId, newBalance, tier, transaction }
 */
router.post('/award', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement point awarding logic here. See challenge docs for requirements.',
    hint: {
      input: { playerId: 'string', points: 'number', reason: 'string' },
      output: { playerId: 'string', newBalance: 'number', tier: 'string', transaction: 'object' },
    },
  });
});

/**
 * GET /api/v1/points/leaderboard
 *
 * Get the points leaderboard. Candidates implement this.
 *
 * Expected response:
 *   { leaderboard: [{ playerId, username, points, tier, rank }] }
 */
router.get('/leaderboard', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement leaderboard query here. See challenge docs for requirements.',
  });
});

module.exports = router;
