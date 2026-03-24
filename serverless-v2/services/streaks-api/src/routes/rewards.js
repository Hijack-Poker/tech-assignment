'use strict';

const { Router } = require('express');
const { getRewards } = require('../services/dynamo.service');

const router = Router();

/**
 * GET /api/v1/player/streaks/rewards
 *
 * Returns the player's streak reward history, sorted by createdAt descending.
 */
router.get('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const rewards = await getRewards(playerId);

    // Sort by createdAt descending (newest first)
    rewards.sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''));

    const result = rewards.map((r) => ({
      date: r.createdAt,
      milestone: r.milestone,
      type: r.type,
      points: r.points,
    }));

    return res.status(200).json({ rewards: result });
  } catch (err) {
    console.error('Rewards fetch error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
