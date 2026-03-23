'use strict';

const path = require('path');
const fs = require('fs');
const express = require('express');
const serverless = require('serverless-http');
const helmet = require('helmet');
const cors = require('cors');
const rateLimit = require('express-rate-limit');

const healthRoute = require('./src/routes/health');
const pointsRoute = require('./src/routes/points');
const playerRoute = require('./src/routes/player');
const notificationsRoute = require('./src/routes/notifications');
const adminRoute = require('./src/routes/admin');
const { authMiddleware } = require('./src/middleware/auth');

const openapiSpec = JSON.parse(JSON.stringify(
  require('yaml').parse(fs.readFileSync(path.join(__dirname, 'openapi.yaml'), 'utf8'))
));

const app = express();

app.use((req, res, next) => {
  if (req.path.startsWith('/docs')) return next();
  return helmet()(req, res, next);
});
app.use(cors({
  origin: ['http://localhost:4000'],
  methods: ['GET', 'POST', 'PATCH', 'PUT'],
  allowedHeaders: ['Content-Type', 'X-Player-Id', 'X-Admin-Key'],
}));
app.use(express.json());

const apiLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 100,
  standardHeaders: 'draft-8',
  legacyHeaders: false,
});

const adminLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 20,
  standardHeaders: 'draft-8',
  legacyHeaders: false,
});

// Public routes
app.use('/api/v1/health', healthRoute);
app.get('/docs/openapi.json', (req, res) => res.json(openapiSpec));
app.get('/docs', (req, res) => {
  res.type('html').send(`<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <title>Rewards API — Swagger UI</title>
  <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"/>
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
  <script>SwaggerUIBundle({ url: '/docs/openapi.json', dom_id: '#swagger-ui' });</script>
</body>
</html>`);
});

// Leaderboard (public — X-Player-Id optional for rank lookup)
app.use('/api/v1', apiLimiter, pointsRoute);

// Protected routes (require auth header)
app.use('/api/v1/points', apiLimiter, authMiddleware, pointsRoute);
app.use('/api/v1/player/notifications', apiLimiter, authMiddleware, notificationsRoute);
app.use('/api/v1/player', apiLimiter, authMiddleware, playerRoute);

// Admin routes (stricter rate limit, auth handled in router)
app.use('/admin', adminLimiter, adminRoute);

// 404 handler
app.use((req, res) => {
  res.status(404).json({ error: 'Not found' });
});

// Check if monthly reset is overdue (warning only — does not auto-trigger)
const checkResetStatus = async () => {
  try {
    const dynamo = require('./src/services/dynamo.service');
    const players = await dynamo.getAllPlayers();
    if (players.length === 0) return;

    const now = new Date();
    const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

    const overdue = players.filter((p) => p.lastResetMonth && p.lastResetMonth !== currentMonthKey);
    if (overdue.length > 0) {
      console.warn(`[RESET WARNING] ${overdue.length} player(s) may need monthly reset (last reset does not match ${currentMonthKey})`);
    }
  } catch (err) {
    console.warn('[RESET WARNING] Could not check reset status:', err.message);
  }
};

// Fire-and-forget on cold start
checkResetStatus();

module.exports.api = serverless(app);
module.exports._checkResetStatus = checkResetStatus;
