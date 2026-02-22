'use strict';

const { Router } = require('express');
const router = Router();

/**
 * POST /api/v1/streaks/check-in
 *
 * Record a daily check-in for a player. Candidates implement this.
 *
 * Expected response:
 *   { playerId, currentStreak, longestStreak, todayCheckedIn, milestone }
 */
router.post('/', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement daily check-in logic here. See challenge docs for requirements.',
    hint: {
      playerId: req.playerId,
      output: {
        playerId: 'string',
        currentStreak: 'number',
        longestStreak: 'number',
        todayCheckedIn: 'boolean',
        milestone: 'object | null',
      },
    },
  });
});

module.exports = router;
