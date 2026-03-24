'use strict';

const { Router } = require('express');
const { getPlayer, putPlayer, getAllPlayers, updatePlayer, getRewards } = require('../services/dynamo.service');
const { MILESTONES } = require('../config/constants');
const { getPlayerScore } = require('../services/streak.service');

const router = Router();

/**
 * Determine VIP tier based on combined streak score.
 */
function getTier(score) {
  if (score >= 90) return 'platinum';
  if (score >= 30) return 'gold';
  if (score >= 7) return 'silver';
  return 'bronze';
}

/**
 * Find the next milestone for a given streak count and type.
 * Returns { days, reward, daysRemaining } or null if past all milestones.
 */
function getNextMilestone(streakCount, type) {
  const milestone = MILESTONES.find((m) => m.days > streakCount);
  if (!milestone) return null;
  return {
    days: milestone.days,
    reward: type === 'login' ? milestone.loginReward : milestone.playReward,
    daysRemaining: milestone.days - streakCount,
  };
}

/**
 * GET /api/v1/streaks
 *
 * Returns the player's current streak state.
 */
router.get('/', async (req, res) => {
  try {
    const playerId = req.playerId;
    let player = await getPlayer(playerId);

    // Auto-create player record on first authenticated request
    if (!player) {
      player = {
        playerId,
        displayName: req.displayName || '',
        loginStreak: 0,
        playStreak: 0,
        bestLoginStreak: 0,
        bestPlayStreak: 0,
        lastLoginDate: '',
        lastPlayDate: '',
        freezesAvailable: 0,
        freezesUsedThisMonth: 0,
        lastFreezeGrantDate: '',
        updatedAt: new Date().toISOString(),
      };
      await putPlayer(player);
    }

    // Update displayName if available but not yet stored
    if (player && req.displayName && !player.displayName) {
      await updatePlayer(playerId, { displayName: req.displayName });
      player.displayName = req.displayName;
    }

    const loginStreak = player.loginStreak || 0;
    const playStreak = player.playStreak || 0;
    const bestLoginStreak = player.bestLoginStreak || 0;
    const bestPlayStreak = player.bestPlayStreak || 0;
    const freezesAvailable = player.freezesAvailable || 0;
    const lastLoginDate = player.lastLoginDate || '';
    const lastPlayDate = player.lastPlayDate || '';

    // Combo bonus — both streaks active
    const comboActive = loginStreak > 0 && playStreak > 0;
    const comboMultiplier = comboActive ? 1 + Math.min(Math.floor((loginStreak + playStreak) / 10), 5) * 0.1 : 1;

    return res.status(200).json({
      loginStreak,
      playStreak,
      bestLoginStreak,
      bestPlayStreak,
      freezesAvailable,
      nextLoginMilestone: getNextMilestone(loginStreak, 'login'),
      nextPlayMilestone: getNextMilestone(playStreak, 'play'),
      lastLoginDate,
      lastPlayDate,
      comboActive,
      comboMultiplier,
    });
  } catch (err) {
    console.error('Streaks fetch error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * GET /api/v1/streaks/leaderboard
 *
 * Returns the top players ranked by streak count.
 * Query params: type=login|play|combined (default: combined)
 */
router.get('/leaderboard', async (req, res) => {
  try {
    const { type = 'combined' } = req.query;
    const players = await getAllPlayers();

    const ranked = players
      .map((p) => {
        let score;
        if (type === 'login') score = p.loginStreak || 0;
        else if (type === 'play') score = p.playStreak || 0;
        else score = getPlayerScore(p);

        return {
          playerId: p.playerId,
          displayName: p.displayName || p.playerId.slice(0, 8),
          loginStreak: p.loginStreak || 0,
          playStreak: p.playStreak || 0,
          bestLoginStreak: p.bestLoginStreak || 0,
          bestPlayStreak: p.bestPlayStreak || 0,
          score,
          tier: getTier(getPlayerScore(p)),
        };
      })
      .sort((a, b) => b.score - a.score)
      .slice(0, 50)
      .map((p, i) => ({ ...p, rank: i + 1 }));

    // Find the requesting player's rank
    const myPlayerId = req.playerId;
    const myEntry = ranked.find((p) => p.playerId === myPlayerId);
    let playerRank = myEntry || null;

    if (!playerRank) {
      // Player not in top 50 — calculate their rank
      const allRanked = players
        .map((p) => {
          let score;
          if (type === 'login') score = p.loginStreak || 0;
          else if (type === 'play') score = p.playStreak || 0;
          else score = getPlayerScore(p);
          return { playerId: p.playerId, score };
        })
        .sort((a, b) => b.score - a.score);

      const idx = allRanked.findIndex((p) => p.playerId === myPlayerId);
      if (idx >= 0) {
        const p = players.find((pl) => pl.playerId === myPlayerId);
        const score = allRanked[idx].score;
        playerRank = {
          rank: idx + 1,
          playerId: myPlayerId,
          displayName: p?.displayName || myPlayerId.slice(0, 8),
          loginStreak: p?.loginStreak || 0,
          playStreak: p?.playStreak || 0,
          score,
          tier: getTier(score),
        };
      }
    }

    return res.status(200).json({ leaderboard: ranked, playerRank });
  } catch (err) {
    console.error('Leaderboard error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

/**
 * GET /api/v1/streaks/share
 *
 * Returns the player's streak data formatted for sharing.
 */
router.get('/share', async (req, res) => {
  try {
    const playerId = req.playerId;
    const player = await getPlayer(playerId);

    if (!player) {
      return res.status(200).json({
        playerName: 'New Player',
        loginStreak: 0,
        playStreak: 0,
        bestLoginStreak: 0,
        bestPlayStreak: 0,
        tier: 'bronze',
        totalRewards: 0,
        shareText: "I just joined Hijack Poker! Come play with me! \uD83C\uDCA3",
      });
    }

    const loginStreak = player.loginStreak || 0;
    const playStreak = player.playStreak || 0;
    const bestLoginStreak = player.bestLoginStreak || 0;
    const bestPlayStreak = player.bestPlayStreak || 0;
    const playerName = player.displayName || playerId.slice(0, 8);
    const tier = getTier(loginStreak + playStreak);

    const rewards = await getRewards(playerId);
    const totalRewards = rewards.length;

    const parts = [];
    if (loginStreak > 0) parts.push(`${loginStreak}-day login streak`);
    if (playStreak > 0) parts.push(`${playStreak}-day play streak`);
    const streakSummary = parts.length > 0 ? parts.join(' and ') : null;

    const shareText = streakSummary
      ? `🔥 ${streakSummary} on Hijack Poker! ${tier !== 'bronze' ? `${tier.charAt(0).toUpperCase() + tier.slice(1)} tier. ` : ''}Can you beat me?\n\nhttps://hijackpoker.com`
      : "I just joined Hijack Poker! Come play with me! 🃏\n\nhttps://hijackpoker.com";

    return res.status(200).json({
      playerName,
      loginStreak,
      playStreak,
      bestLoginStreak,
      bestPlayStreak,
      tier,
      totalRewards,
      shareText,
    });
  } catch (err) {
    console.error('Share data error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
});

module.exports = router;
module.exports._test = { getNextMilestone };
