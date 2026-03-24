'use strict';

const express = require('express');
const serverless = require('serverless-http');

const healthRoute = require('./src/routes/health');
const checkInRoute = require('./src/routes/check-in');
const calendarRoute = require('./src/routes/calendar');
const internalRoute = require('./src/routes/internal');
const streaksRoute = require('./src/routes/streaks');
const adminRoute = require('./src/routes/admin');
const rewardsRoute = require('./src/routes/rewards');
const freezesRoute = require('./src/routes/freezes');
const missionsRoute = require('./src/routes/missions');
const responsibleGamingRoute = require('./src/routes/responsible-gaming');
const { authMiddleware } = require('./src/middleware/auth');

const app = express();

app.use(express.json());

// CORS for local frontend
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Player-Id, X-Display-Name');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  if (req.method === 'OPTIONS') return res.sendStatus(200);
  next();
});

// Public routes
app.use('/api/v1/health', healthRoute);

// Internal routes (no auth)
app.use('/internal/streaks', internalRoute);

// Admin routes (no player JWT auth — MVP)
app.use('/api/v1/admin', adminRoute);

// Protected routes
app.use('/api/v1/streaks/check-in', authMiddleware, checkInRoute);
app.use('/api/v1/player/streaks/calendar', authMiddleware, calendarRoute);
app.use('/api/v1/player/streaks/rewards', authMiddleware, rewardsRoute);
app.use('/api/v1/player/streaks/freezes', authMiddleware, freezesRoute);
app.use('/api/v1/missions', authMiddleware, missionsRoute);
app.use('/api/v1/player/responsible-gaming', authMiddleware, responsibleGamingRoute);
app.use('/api/v1/streaks', authMiddleware, streaksRoute);

// 404 handler
app.use((req, res) => {
  res.status(404).json({ error: 'Not found' });
});

module.exports.api = serverless(app);

/**
 * Scheduled freeze consumption handler.
 * Runs at 01:00 UTC daily via EventBridge/cron.
 * Scans all players and auto-consumes a freeze for any player
 * who missed yesterday (last activity was 2+ days ago and has freezes).
 */
module.exports.scheduledFreezeConsumption = async () => {
  const { getAllPlayers, updatePlayer, addActivity } = require('./src/services/dynamo.service');
  const { consumeFreeze } = require('./src/services/freeze.service');
  const { toUTCDateString, subtractDays } = require('./src/services/streak.service');

  const now = new Date();
  const today = toUTCDateString(now);
  const yesterday = subtractDays(today, 1);

  console.log(`[scheduled-freeze] Running for ${today}, checking missed day: ${yesterday}`);

  const players = await getAllPlayers();
  let consumed = 0;
  let skipped = 0;

  for (const player of players) {
    const lastLogin = player.lastLoginDate || '';
    const lastPlay = player.lastPlayDate || '';
    const freezes = player.freezesAvailable || 0;

    // Skip if player was active yesterday or today, or has no freezes
    if (!lastLogin || freezes <= 0) {
      skipped++;
      continue;
    }

    // Player's last login was exactly 2 days ago (missed yesterday)
    if (lastLogin === subtractDays(today, 2)) {
      const result = await consumeFreeze(
        player.playerId,
        yesterday,
        'free_monthly',
        freezes,
        player.freezesUsedThisMonth || 0
      );

      await updatePlayer(player.playerId, {
        freezesAvailable: result.freezesAvailable,
        freezesUsedThisMonth: result.freezesUsedThisMonth,
        updatedAt: now.toISOString(),
      });

      // Record freeze activity for the missed day
      await addActivity(player.playerId, yesterday, {
        loggedIn: false,
        played: false,
        freezeUsed: true,
        streakBroken: false,
        loginStreakAtDay: player.loginStreak || 0,
        playStreakAtDay: player.playStreak || 0,
      });

      consumed++;
      console.log(`[scheduled-freeze] Consumed freeze for ${player.playerId}`);
    } else {
      skipped++;
    }
  }

  console.log(`[scheduled-freeze] Done. Consumed: ${consumed}, Skipped: ${skipped}`);
  return { consumed, skipped };
};
