'use strict';

const { Router } = require('express');
const { getDailyMissions, claimMission } = require('../services/missions.service');

const router = Router();

/**
 * Get today's UTC date string.
 */
function getToday() {
  return new Date().toISOString().slice(0, 10);
}

/**
 * GET /api/v1/missions
 * Returns today's missions for the authenticated player.
 */
router.get('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const today = getToday();
    const record = await getDailyMissions(playerId, today);

    return res.status(200).json({
      date: today,
      missions: record.missions,
      pointsEarnedToday: record.pointsEarnedToday || 0,
    });
  } catch (err) {
    console.error('Missions fetch error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * POST /api/v1/missions/:missionId/claim
 * Claim a completed mission reward.
 */
router.post('/:missionId/claim', async (req, res) => {
  try {
    const playerId = req.playerId;
    const { missionId } = req.params;
    const today = getToday();

    const result = await claimMission(playerId, today, missionId);
    if (result.error) {
      return res.status(400).json({ error: result.error });
    }

    return res.status(200).json(result);
  } catch (err) {
    console.error('Mission claim error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
