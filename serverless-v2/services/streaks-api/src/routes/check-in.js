'use strict';

const { Router } = require('express');
const { getPlayer, putPlayer, updatePlayer, addActivity } = require('../services/dynamo.service');
const { checkAndAwardMilestone } = require('../services/rewards.service');

const router = Router();

// Maximum streak display cap
const MAX_STREAK = 365;

// Monthly free freezes granted
const MONTHLY_FREE_FREEZES = 1;

// Injectable date provider for testing
let _getCurrentDate = () => new Date();

function setDateProvider(fn) {
  _getCurrentDate = fn;
}

function resetDateProvider() {
  _getCurrentDate = () => new Date();
}

/**
 * Get today's UTC date as YYYY-MM-DD string.
 */
function getUTCDateString(date) {
  return date.toISOString().slice(0, 10);
}

/**
 * Get UTC month as YYYY-MM string.
 */
function getUTCMonthString(date) {
  return date.toISOString().slice(0, 7);
}

/**
 * Calculate the difference in UTC calendar days between two YYYY-MM-DD strings.
 * Returns (dateStr2 - dateStr1) in days.
 */
function daysBetween(dateStr1, dateStr2) {
  const d1 = new Date(dateStr1 + 'T00:00:00Z');
  const d2 = new Date(dateStr2 + 'T00:00:00Z');
  return Math.round((d2 - d1) / (1000 * 60 * 60 * 24));
}

/**
 * POST /api/v1/streaks/check-in
 *
 * Record a daily login check-in for a player.
 * Implements the 9-step check-in flow:
 *
 * 1. Get playerId from auth middleware
 * 2. Calculate today (UTC)
 * 3. Get player record (or create new)
 * 4. Idempotency: if already checked in today, return current state
 * 5. Lazy monthly freeze grant check
 * 6. Calculate days since last login
 * 7. Consecutive day → increment streak
 * 8. Missed day → consume freeze or reset streak
 * 9. Update best streak, cap at 365, write activity, return response
 */
router.post('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    const now = _getCurrentDate();
    const today = getUTCDateString(now);
    const currentMonth = getUTCMonthString(now);

    // Step 3: Get player record
    let player = await getPlayer(playerId);

    // Self-exclusion enforcement
    if (player && player.selfExcludedUntil && new Date(player.selfExcludedUntil) > new Date()) {
      return res.status(403).json({
        error: 'Forbidden',
        message: `Account is self-excluded until ${player.selfExcludedUntil}`,
      });
    }

    // First-time player — create new record
    if (!player) {
      player = {
        playerId,
        loginStreak: 1,
        playStreak: 0,
        bestLoginStreak: 1,
        bestPlayStreak: 0,
        lastLoginDate: today,
        lastPlayDate: '',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: now.toISOString(),
      };
      await putPlayer(player);
      await addActivity(playerId, today, {
        loggedIn: true,
        played: false,
        freezeUsed: false,
        streakBroken: false,
        loginStreakAtDay: 1,
        playStreakAtDay: 0,
      });

      const milestone = await checkAndAwardMilestone(playerId, 1, 'login');

      return res.status(200).json({
        playerId,
        loginStreak: 1,
        bestLoginStreak: 1,
        todayCheckedIn: true,
        milestone: milestone || null,
      });
    }

    // Step 4: Idempotency — already checked in today
    if (player.lastLoginDate === today) {
      // Ensure activity record has loggedIn=true (may have been overwritten by hand-completed)
      await addActivity(playerId, today, { loggedIn: true });
      return res.status(200).json({
        playerId,
        loginStreak: player.loginStreak,
        bestLoginStreak: player.bestLoginStreak,
        todayCheckedIn: true,
        milestone: null,
      });
    }

    // Step 5: Monthly free freeze grant (lazy check)
    let freezesAvailable = player.freezesAvailable || 0;
    let freezesUsedThisMonth = player.freezesUsedThisMonth || 0;
    let lastFreezeGrantDate = player.lastFreezeGrantDate || '';

    if (lastFreezeGrantDate !== currentMonth) {
      freezesAvailable += MONTHLY_FREE_FREEZES;
      freezesUsedThisMonth = 0;
      lastFreezeGrantDate = currentMonth;
    }

    // Step 6: Calculate days since last login
    const daysSinceLogin = daysBetween(player.lastLoginDate, today);

    let newStreak;
    let freezeUsed = false;
    let streakBroken = false;

    if (daysSinceLogin === 1) {
      // Step 7: Consecutive day — increment streak
      newStreak = (player.loginStreak || 0) + 1;
    } else if (daysSinceLogin === 2 && freezesAvailable > 0) {
      // Step 8a: Exactly 1 day missed, freeze available — consume freeze, preserve streak
      freezeUsed = true;
      freezesAvailable -= 1;
      freezesUsedThisMonth += 1;
      newStreak = (player.loginStreak || 0) + 1;
    } else {
      // Step 8b: 2+ days missed or 1 day missed with no freeze — streak resets
      newStreak = 1;
      streakBroken = true;
    }

    // Step 9: Cap streak at maximum
    newStreak = Math.min(newStreak, MAX_STREAK);

    // Update best streak
    const bestLoginStreak = Math.max(newStreak, player.bestLoginStreak || 0);

    // Build the update payload
    const updates = {
      loginStreak: newStreak,
      bestLoginStreak,
      lastLoginDate: today,
      freezesAvailable,
      freezesUsedThisMonth,
      lastFreezeGrantDate,
      updatedAt: now.toISOString(),
    };

    await updatePlayer(playerId, updates);

    // Write daily activity record
    await addActivity(playerId, today, {
      loggedIn: true,
      played: false,
      freezeUsed,
      streakBroken,
      loginStreakAtDay: newStreak,
      playStreakAtDay: player.playStreak || 0,
    });

    // Check for milestone reward on the new login streak
    const milestone = await checkAndAwardMilestone(playerId, newStreak, 'login');

    return res.status(200).json({
      playerId,
      loginStreak: newStreak,
      bestLoginStreak,
      todayCheckedIn: true,
      milestone: milestone || null,
    });
  } catch (err) {
    console.error('Check-in error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
module.exports.setDateProvider = setDateProvider;
module.exports.resetDateProvider = resetDateProvider;
module.exports._test = { getUTCDateString, getUTCMonthString, daysBetween, MAX_STREAK, MONTHLY_FREE_FREEZES };
