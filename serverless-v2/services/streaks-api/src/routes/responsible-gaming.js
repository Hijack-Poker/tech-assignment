'use strict';

const { Router } = require('express');
const { getPlayer, updatePlayer } = require('../services/dynamo.service');

const router = Router();

/**
 * GET /api/v1/player/responsible-gaming
 * Returns the player's responsible gaming settings.
 */
router.get('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const player = await getPlayer(playerId);

    return res.status(200).json({
      sessionLimitMinutes: player?.sessionLimitMinutes || null,
      dailyHandLimit: player?.dailyHandLimit || null,
      selfExcludedUntil: player?.selfExcludedUntil || null,
      reminderEnabled: player?.reminderEnabled !== false,
      coolOffEnabled: player?.coolOffEnabled || false,
    });
  } catch (err) {
    console.error('Responsible gaming fetch error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * PUT /api/v1/player/responsible-gaming
 * Update responsible gaming settings.
 */
router.put('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const { sessionLimitMinutes, dailyHandLimit, reminderEnabled } = req.body;

    const updates = { updatedAt: new Date().toISOString() };

    if (sessionLimitMinutes !== undefined) {
      if (sessionLimitMinutes !== null && (sessionLimitMinutes < 15 || sessionLimitMinutes > 1440)) {
        return res.status(400).json({ error: 'Session limit must be between 15 and 1440 minutes, or null to disable' });
      }
      updates.sessionLimitMinutes = sessionLimitMinutes;
    }

    if (dailyHandLimit !== undefined) {
      if (dailyHandLimit !== null && (dailyHandLimit < 1 || dailyHandLimit > 10000)) {
        return res.status(400).json({ error: 'Daily hand limit must be between 1 and 10000, or null to disable' });
      }
      updates.dailyHandLimit = dailyHandLimit;
    }

    if (reminderEnabled !== undefined) {
      updates.reminderEnabled = !!reminderEnabled;
    }

    await updatePlayer(playerId, updates);

    return res.status(200).json({ success: true, ...updates });
  } catch (err) {
    console.error('Responsible gaming update error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * POST /api/v1/player/responsible-gaming/self-exclude
 * Self-exclude for a number of days. This is intentionally hard to undo.
 */
router.post('/self-exclude', async (req, res) => {
  try {
    const playerId = req.playerId;
    const { days } = req.body;

    if (!days || days < 1 || days > 365) {
      return res.status(400).json({ error: 'Self-exclusion period must be between 1 and 365 days' });
    }

    const until = new Date();
    until.setUTCDate(until.getUTCDate() + days);

    await updatePlayer(playerId, {
      selfExcludedUntil: until.toISOString(),
      updatedAt: new Date().toISOString(),
    });

    return res.status(200).json({
      success: true,
      selfExcludedUntil: until.toISOString(),
      message: `You have been self-excluded until ${until.toISOString().slice(0, 10)}. Contact support to reverse this.`,
    });
  } catch (err) {
    console.error('Self-exclusion error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
