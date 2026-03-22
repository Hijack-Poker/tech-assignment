'use strict';

/**
 * Standalone Express server for Fly.io deployment.
 * Wraps the Lambda handlers into standard HTTP routes.
 */
const express = require('express');
const { processHandHttp, getTableHttp, resetTableHttp, freshResetTableHttp, tipDealerHttp, health } = require('./handler');

const app = express();

// CORS middleware (must be before routes)
app.use((req, res, next) => {
  res.set({
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
  });
  if (req.method === 'OPTIONS') {
    return res.sendStatus(204);
  }
  next();
});

app.use(express.json());

// Convert Lambda-style handler to Express middleware
function lambdaToExpress(handler) {
  return async (req, res) => {
    const event = {
      body: JSON.stringify(req.body),
      pathParameters: req.params,
      queryStringParameters: req.query,
      headers: req.headers,
      httpMethod: req.method,
    };

    try {
      const result = await handler(event);
      const headers = result.headers || {};
      Object.entries(headers).forEach(([key, value]) => res.set(key, value));
      res.status(result.statusCode).send(result.body);
    } catch (err) {
      console.error('Handler error:', err);
      res.status(500).json({ error: 'Internal server error' });
    }
  };
}

// Routes
app.get('/health', lambdaToExpress(health));
app.post('/process', lambdaToExpress(processHandHttp));
app.get('/table/:tableId', lambdaToExpress(getTableHttp));
app.post('/table/:tableId/reset', lambdaToExpress(resetTableHttp));
app.post('/table/:tableId/fresh-reset', lambdaToExpress(freshResetTableHttp));
app.post('/table/:tableId/tip', lambdaToExpress(tipDealerHttp));

const PORT = process.env.PORT || 3030;
app.listen(PORT, '0.0.0.0', () => {
  console.log(`Holdem processor listening on port ${PORT}`);
});
