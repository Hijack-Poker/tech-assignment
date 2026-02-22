'use strict';

const { Router } = require('express');
const router = Router();

/**
 * GET /api/v1/streaks
 *
 * Get a player's streak data. Candidates implement this.
 *
 * Uses playerId from auth middleware (req.playerId).
 *
 * Expected response:
 *   { playerId, currentStreak, longestStreak, totalCheckIns, milestones, calendar }
 */
router.get('/', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement streak data lookup here. See challenge docs for requirements.',
    hint: {
      playerId: req.playerId,
      output: {
        playerId: 'string',
        currentStreak: 'number',
        longestStreak: 'number',
        totalCheckIns: 'number',
        milestones: 'array',
        calendar: 'array of { date, checkedIn }',
      },
    },
  });
});

/**
 * GET /api/v1/streaks/leaderboard
 *
 * Get the streaks leaderboard. Candidates implement this.
 */
router.get('/leaderboard', (req, res) => {
  res.status(501).json({
    error: 'Not implemented',
    message: 'Implement streaks leaderboard here. See challenge docs for requirements.',
  });
});

module.exports = router;
