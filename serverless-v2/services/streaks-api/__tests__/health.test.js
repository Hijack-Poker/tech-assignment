'use strict';

const express = require('express');
const healthRoute = require('../src/routes/health');

describe('Streaks API — Health', () => {
  it('should return 200 with service info', async () => {
    const req = { method: 'GET', url: '/api/v1/health' };
    const res = {
      body: null,
      json(data) {
        this.body = data;
        return this;
      },
    };

    const handler = healthRoute.stack[0].route.stack[0].handle;
    handler(req, res);

    expect(res.body.service).toBe('streaks-api');
    expect(res.body.status).toBe('ok');
    expect(res.body.timestamp).toBeDefined();
  });
});
