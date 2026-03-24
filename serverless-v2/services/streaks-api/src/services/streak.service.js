'use strict';

/**
 * Shared streak calculation logic.
 * Used by both login check-in (TICKET-004) and play hand-completed (TICKET-005).
 */

/**
 * Get the UTC date string (YYYY-MM-DD) from a Date object.
 */
function toUTCDateString(date) {
  return date.toISOString().slice(0, 10);
}

/**
 * Get the date N days before the given UTC date string.
 * @param {string} dateStr - YYYY-MM-DD
 * @param {number} days - number of days to subtract
 * @returns {string} YYYY-MM-DD
 */
function subtractDays(dateStr, days) {
  const d = new Date(dateStr + 'T00:00:00Z');
  d.setUTCDate(d.getUTCDate() - days);
  return toUTCDateString(d);
}

/**
 * Calculate the streak update result given the current state.
 *
 * @param {object} params
 * @param {string|null} params.lastDate - last activity date (YYYY-MM-DD) or null if first time
 * @param {string} params.today - today's UTC date (YYYY-MM-DD)
 * @param {number} params.currentStreak - current streak count
 * @param {number} params.freezesAvailable - available freezes
 * @param {boolean} params.freezeAlreadyUsedToday - whether a freeze was already consumed today
 * @returns {{ action: 'none'|'increment'|'freeze'|'reset', newStreak: number, consumeFreeze: boolean, streakBroken: boolean }}
 */
function calculateStreakUpdate({ lastDate, today, currentStreak, freezesAvailable, freezeAlreadyUsedToday }) {
  // First time ever — start at 1
  if (!lastDate) {
    return { action: 'increment', newStreak: 1, consumeFreeze: false, streakBroken: false };
  }

  // Already recorded today — idempotent
  if (lastDate === today) {
    return { action: 'none', newStreak: currentStreak, consumeFreeze: false, streakBroken: false };
  }

  const yesterday = subtractDays(today, 1);

  // Consecutive day
  if (lastDate === yesterday) {
    return { action: 'increment', newStreak: currentStreak + 1, consumeFreeze: false, streakBroken: false };
  }

  // Exactly 2 days ago — freeze eligible
  const twoDaysAgo = subtractDays(today, 2);
  if (lastDate === twoDaysAgo && freezesAvailable > 0 && !freezeAlreadyUsedToday) {
    return { action: 'freeze', newStreak: currentStreak, consumeFreeze: true, streakBroken: false };
  }

  // Gap too large or no freeze — reset
  return { action: 'reset', newStreak: 1, consumeFreeze: false, streakBroken: true };
}

/**
 * Calculate a player's combined streak score.
 * @param {object} player - player record from DynamoDB
 * @returns {number}
 */
function getPlayerScore(player) {
  return (player.loginStreak || 0) + (player.playStreak || 0);
}

module.exports = {
  toUTCDateString,
  subtractDays,
  calculateStreakUpdate,
  getPlayerScore,
};
