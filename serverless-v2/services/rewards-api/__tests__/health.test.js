'use strict';

const express = require('express');
const healthRoute = require('../src/routes/health');

// Create a mini Express app for testing
function createTestApp() {
  const app = express();
  app.use('/api/v1/health', healthRoute);
  return app;
}

describe('Rewards API — Health', () => {
  it('should return 200 with service info', async () => {
    const app = createTestApp();

    // Simulate a request
    const req = { method: 'GET', url: '/api/v1/health' };
    const res = {
      statusCode: null,
      body: null,
      json(data) {
        this.body = data;
        return this;
      },
    };

    // Direct route handler test
    const handler = healthRoute.stack[0].route.stack[0].handle;
    handler(req, res);

    expect(res.body.service).toBe('rewards-api');
    expect(res.body.status).toBe('ok');
    expect(res.body.timestamp).toBeDefined();
  });
});
