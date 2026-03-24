'use strict';

const { Router } = require('express');
const { getActivity } = require('../services/dynamo.service');

const router = Router();

/**
 * Validate month parameter format (YYYY-MM) and return year/month numbers.
 * Returns null if invalid.
 */
function parseMonth(monthParam) {
  if (!monthParam || typeof monthParam !== 'string') return null;

  const match = monthParam.match(/^(\d{4})-(0[1-9]|1[0-2])$/);
  if (!match) return null;

  const year = parseInt(match[1], 10);
  const month = parseInt(match[2], 10);

  return { year, month };
}

/**
 * Get the number of days in a given month.
 */
function daysInMonth(year, month) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

/**
 * Derive activity type from activity record flags.
 * Priority: freezeUsed > played > login_only > streak_broken > none
 */
function deriveActivityType(record) {
  if (record.freezeUsed) return 'freeze';
  if (record.played) return 'played';
  if (record.loggedIn) return 'login_only';
  if (record.streakBroken) return 'streak_broken';
  return 'none';
}

/**
 * GET /api/v1/player/streaks/calendar?month=YYYY-MM
 *
 * Returns daily activity data for a given month to power the calendar heat map.
 */
router.get('/', async (req, res) => {
  try {
    const parsed = parseMonth(req.query.month);

    if (!parsed) {
      return res.status(400).json({
        error: 'Bad Request',
        message: 'month query parameter is required and must be in YYYY-MM format',
      });
    }

    const { year, month } = parsed;
    const playerId = req.playerId;

    const totalDays = daysInMonth(year, month);
    const startDate = `${req.query.month}-01`;
    const endDate = `${req.query.month}-${String(totalDays).padStart(2, '0')}`;

    // Query activity records for the player in this date range
    const activities = await getActivity(playerId, startDate, endDate);

    // Index activity records by date for fast lookup
    const activityByDate = {};
    for (const record of activities) {
      activityByDate[record.date] = record;
    }

    // Build days array with one entry per day of the month
    const days = [];
    for (let d = 1; d <= totalDays; d++) {
      const dateStr = `${req.query.month}-${String(d).padStart(2, '0')}`;
      const record = activityByDate[dateStr];

      if (record) {
        days.push({
          date: dateStr,
          activity: deriveActivityType(record),
          loginStreak: record.loginStreakAtDay || 0,
          playStreak: record.playStreakAtDay || 0,
        });
      } else {
        days.push({
          date: dateStr,
          activity: 'none',
          loginStreak: 0,
          playStreak: 0,
        });
      }
    }

    return res.status(200).json({
      month: req.query.month,
      days,
    });
  } catch (err) {
    console.error('Calendar error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
module.exports._test = { parseMonth, daysInMonth, deriveActivityType };
