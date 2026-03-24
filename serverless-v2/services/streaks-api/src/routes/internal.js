'use strict';

const { Router } = require('express');
const { getPlayer, putPlayer, updatePlayer, addActivity, getActivity } = require('../services/dynamo.service');
const { calculateStreakUpdate, toUTCDateString } = require('../services/streak.service');
const { checkAndAwardMilestone } = require('../services/rewards.service');

const router = Router();

/**
 * POST /internal/streaks/hand-completed
 *
 * Called by the game processor when a player completes a hand.
 * Updates the play streak independently from the login streak.
 *
 * Body: { playerId, tableId, handId, completedAt }
 * No JWT auth required (internal endpoint).
 */
router.post('/hand-completed', async (req, res) => {
  try {
    const { playerId, tableId, handId, completedAt } = req.body;

    // Validate required fields
    if (!playerId || !tableId || !handId || !completedAt) {
      return res.status(400).json({
        error: 'Bad Request',
        message: 'playerId, tableId, handId, and completedAt are required',
      });
    }

    // Validate completedAt is a valid ISO timestamp
    const completedDate = new Date(completedAt);
    if (isNaN(completedDate.getTime())) {
      return res.status(400).json({
        error: 'Bad Request',
        message: 'completedAt must be a valid ISO 8601 timestamp',
      });
    }

    const today = toUTCDateString(completedDate);

    // Get or initialise player
    let player = await getPlayer(playerId);

    // Self-exclusion enforcement
    if (player && player.selfExcludedUntil && new Date(player.selfExcludedUntil) > new Date()) {
      return res.status(403).json({
        error: 'Forbidden',
        message: `Account is self-excluded until ${player.selfExcludedUntil}`,
      });
    }

    const isNewPlayer = !player;

    if (isNewPlayer) {
      player = {
        playerId,
        loginStreak: 0,
        playStreak: 0,
        bestLoginStreak: 0,
        bestPlayStreak: 0,
        lastLoginDate: null,
        lastPlayDate: null,
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: new Date().toISOString(),
      };
    }

    // Check if a freeze was already consumed today (e.g. via login check-in)
    let freezeAlreadyUsedToday = false;
    const todayActivity = await getActivity(playerId, today, today);
    if (Array.isArray(todayActivity) && todayActivity.some((a) => a.freezeUsed)) {
      freezeAlreadyUsedToday = true;
    }

    const result = calculateStreakUpdate({
      lastDate: player.lastPlayDate,
      today,
      currentStreak: player.playStreak,
      freezesAvailable: player.freezesAvailable,
      freezeAlreadyUsedToday,
    });

    // Idempotent — already played today
    if (result.action === 'none') {
      return res.status(200).json({
        playerId,
        playStreak: player.playStreak,
        bestPlayStreak: player.bestPlayStreak,
        alreadyPlayedToday: true,
      });
    }

    // Build player updates
    const updates = {
      playStreak: result.newStreak,
      lastPlayDate: today,
      updatedAt: new Date().toISOString(),
    };

    // Update bestPlayStreak if exceeded
    if (result.newStreak > player.bestPlayStreak) {
      updates.bestPlayStreak = result.newStreak;
    }

    // Consume freeze if needed
    if (result.consumeFreeze) {
      updates.freezesAvailable = player.freezesAvailable - 1;
      updates.freezesUsedThisMonth = (player.freezesUsedThisMonth || 0) + 1;
    }

    // Persist player
    if (isNewPlayer) {
      await putPlayer({ ...player, ...updates });
    } else {
      await updatePlayer(playerId, updates);
    }

    // Write daily activity record
    await addActivity(playerId, today, {
      played: true,
      loggedIn: false,
      freezeUsed: result.consumeFreeze,
      streakBroken: result.streakBroken,
      loginStreakAtDay: player.loginStreak || 0,
      playStreakAtDay: result.newStreak,
    });

    // Check for play milestone reward
    const milestone = await checkAndAwardMilestone(playerId, result.newStreak, 'play');

    return res.status(200).json({
      playerId,
      playStreak: result.newStreak,
      bestPlayStreak: Math.max(result.newStreak, player.bestPlayStreak),
      streakBroken: result.streakBroken,
      freezeConsumed: result.consumeFreeze,
      alreadyPlayedToday: false,
      milestone: milestone || null,
    });
  } catch (err) {
    console.error('hand-completed error:', err);
    return res.status(500).json({
      error: 'Internal Server Error',
      message: 'Failed to process hand completion',
    });
  }
});

module.exports = router;
