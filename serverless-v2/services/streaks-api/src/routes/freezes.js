'use strict';

const { Router } = require('express');
const { getPlayer, getFreezeHistory } = require('../services/dynamo.service');

const router = Router();

/**
 * GET /api/v1/player/streaks/freezes
 *
 * Returns the player's freeze balance and freeze usage history.
 */
router.get('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const player = await getPlayer(playerId);

    const freezesAvailable = player ? player.freezesAvailable || 0 : 0;
    const freezesUsedThisMonth = player ? player.freezesUsedThisMonth || 0 : 0;

    const freezeRecords = await getFreezeHistory(playerId);

    const history = freezeRecords.map((r) => ({
      date: r.date,
      source: r.source,
    }));

    return res.status(200).json({
      freezesAvailable,
      freezesUsedThisMonth,
      history,
    });
  } catch (err) {
    console.error('Freezes fetch error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
