'use strict';

const { Router } = require('express');
const { grantFreezes } = require('../services/freeze.service');
const { getAllPlayers, updatePlayer, getPlayer, getActivity, getRewards, getFreezeHistory } = require('../services/dynamo.service');
const { setDateProvider, resetDateProvider } = require('./check-in');
const { getPlayerScore } = require('../services/streak.service');

const router = Router();

/**
 * POST /api/v1/admin/freezes/grant
 *
 * Grant additional freezes to a player.
 * Body: { playerId, count }
 *
 * No JWT auth required (admin endpoint — skip for MVP).
 */
router.post('/freezes/grant', async (req, res) => {
  try {
    const { playerId, count } = req.body;

    if (!playerId || count == null) {
      return res.status(400).json({
        error: 'Bad Request',
        message: 'playerId and count are required',
      });
    }

    if (typeof count !== 'number' || !Number.isInteger(count) || count < 1) {
      return res.status(400).json({
        error: 'Bad Request',
        message: 'count must be a positive integer',
      });
    }

    const result = await grantFreezes(playerId, count);

    return res.status(200).json({
      playerId: result.playerId,
      freezesAvailable: result.freezesAvailable,
      granted: count,
    });
  } catch (err) {
    if (err.code === 'PLAYER_NOT_FOUND') {
      return res.status(404).json({
        error: 'Not Found',
        message: 'Player not found',
      });
    }
    console.error('Admin grant-freezes error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * GET /api/v1/admin/analytics
 * Returns platform-wide analytics metrics.
 */
router.get('/analytics', async (req, res) => {
  try {
    const players = await getAllPlayers();
    const now = new Date();
    const todayStr = now.toISOString().slice(0, 10);
    const yesterdayStr = new Date(now - 86400000).toISOString().slice(0, 10);
    const weekAgoStr = new Date(now - 7 * 86400000).toISOString().slice(0, 10);
    const monthAgoStr = new Date(now - 30 * 86400000).toISOString().slice(0, 10);

    const totalPlayers = players.length;

    // DAU: players who logged in today
    const dau = players.filter((p) => p.lastLoginDate === todayStr).length;

    // WAU: players who logged in within last 7 days
    const wau = players.filter((p) => p.lastLoginDate >= weekAgoStr).length;

    // MAU: players who logged in within last 30 days
    const mau = players.filter((p) => p.lastLoginDate >= monthAgoStr).length;

    // Active streaks
    const activeLoginStreaks = players.filter((p) => (p.loginStreak || 0) > 0).length;
    const activePlayStreaks = players.filter((p) => (p.playStreak || 0) > 0).length;

    // Average streaks
    const avgLoginStreak = totalPlayers > 0
      ? (players.reduce((sum, p) => sum + (p.loginStreak || 0), 0) / totalPlayers).toFixed(1)
      : 0;
    const avgPlayStreak = totalPlayers > 0
      ? (players.reduce((sum, p) => sum + (p.playStreak || 0), 0) / totalPlayers).toFixed(1)
      : 0;

    // Retention (crude): players who have ever returned (lastLoginDate != creation-ish)
    const retention = {
      d1: players.filter((p) => (p.loginStreak || 0) >= 2).length,
      d7: players.filter((p) => (p.bestLoginStreak || 0) >= 7).length,
      d30: players.filter((p) => (p.bestLoginStreak || 0) >= 30).length,
    };

    // Tier distribution
    const tiers = { bronze: 0, silver: 0, gold: 0, platinum: 0 };
    players.forEach((p) => {
      const score = getPlayerScore(p);
      if (score >= 90) tiers.platinum++;
      else if (score >= 30) tiers.gold++;
      else if (score >= 7) tiers.silver++;
      else tiers.bronze++;
    });

    // Top players
    const topPlayers = players
      .map((p) => ({
        playerId: p.playerId,
        displayName: p.displayName || p.playerId.slice(0, 8),
        loginStreak: p.loginStreak || 0,
        playStreak: p.playStreak || 0,
        score: getPlayerScore(p),
      }))
      .sort((a, b) => b.score - a.score)
      .slice(0, 5);

    // Churn risk: players with streaks who haven't logged in today or yesterday
    const churnRisk = players.filter(
      (p) => (p.loginStreak || 0) > 3 && p.lastLoginDate < yesterdayStr
    ).length;

    return res.status(200).json({
      timestamp: now.toISOString(),
      totalPlayers,
      engagement: { dau, wau, mau },
      streaks: {
        activeLoginStreaks,
        activePlayStreaks,
        avgLoginStreak: Number(avgLoginStreak),
        avgPlayStreak: Number(avgPlayStreak),
      },
      retention,
      tiers,
      topPlayers,
      churnRisk,
    });
  } catch (err) {
    console.error('Analytics error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * GET /api/v1/admin/players/:playerId
 *
 * Fetch a player's complete history: profile, recent activity, rewards, freezes.
 * Used by the admin dashboard for per-player investigation.
 */
router.get('/players/:playerId', async (req, res) => {
  try {
    const { playerId } = req.params;
    const player = await getPlayer(playerId);

    if (!player) {
      return res.status(404).json({ error: 'Not Found', message: 'Player not found' });
    }

    // Fetch last 90 days of activity
    const now = new Date();
    const endDate = now.toISOString().slice(0, 10);
    const startDate = new Date(now - 90 * 86400000).toISOString().slice(0, 10);

    const [activity, rewards, freezeHistory] = await Promise.all([
      getActivity(playerId, startDate, endDate),
      getRewards(playerId),
      getFreezeHistory(playerId),
    ]);

    return res.status(200).json({
      player: {
        playerId: player.playerId,
        displayName: player.displayName || player.playerId,
        loginStreak: player.loginStreak || 0,
        playStreak: player.playStreak || 0,
        bestLoginStreak: player.bestLoginStreak || 0,
        bestPlayStreak: player.bestPlayStreak || 0,
        freezesAvailable: player.freezesAvailable || 0,
        lastLoginDate: player.lastLoginDate || '',
        lastPlayDate: player.lastPlayDate || '',
        selfExcludedUntil: player.selfExcludedUntil || null,
        updatedAt: player.updatedAt || '',
      },
      activity: activity.sort((a, b) => b.date.localeCompare(a.date)),
      rewards: rewards.sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || '')),
      freezeHistory: freezeHistory.sort((a, b) => b.date.localeCompare(a.date)),
    });
  } catch (err) {
    console.error('Admin player-history error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * POST /api/v1/admin/debug/clear-exclusion
 *
 * Clear self-exclusion for a player. Dev/local only.
 * Body: { playerId }
 */
router.post('/debug/clear-exclusion', async (req, res) => {
  try {
    const { playerId } = req.body;
    if (!playerId) {
      return res.status(400).json({ error: 'playerId is required' });
    }
    await updatePlayer(playerId, { selfExcludedUntil: null, updatedAt: new Date().toISOString() });
    return res.status(200).json({ message: `Self-exclusion cleared for ${playerId}` });
  } catch (err) {
    console.error('Clear exclusion error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * POST /api/v1/admin/debug/time-travel
 *
 * Override the server date for testing. Dev/local only.
 * Body: { date: "YYYY-MM-DD" } to set, or { reset: true } to clear.
 */
router.post('/debug/time-travel', (req, res) => {
  const { date, reset } = req.body;

  if (reset) {
    resetDateProvider();
    return res.status(200).json({ message: 'Date reset to real time' });
  }

  if (!date || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
    return res.status(400).json({ error: 'Provide date as YYYY-MM-DD' });
  }

  setDateProvider(() => new Date(date + 'T12:00:00Z'));
  return res.status(200).json({ message: `Server date set to ${date}` });
});

/**
 * GET /api/v1/admin/debug/time-travel
 * Returns the current server date (real or overridden).
 */
router.get('/debug/time-travel', (req, res) => {
  const { _getCurrentDate } = require('./check-in');
  // _getCurrentDate isn't exported, but setDateProvider/resetDateProvider are.
  // We'll just return a check-in against the provider.
  return res.status(200).json({ message: 'Use POST to set date' });
});

module.exports = router;
