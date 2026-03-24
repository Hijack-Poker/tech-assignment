'use strict';

// Mock check-in route to prevent it from loading rewards.service → shared/config/dynamo
jest.mock('../src/routes/check-in', () => {
  const { Router } = require('express');
  const router = Router();
  router.post('/', (req, res) => res.status(200).json({}));
  const mod = Object.assign(router, {
    setDateProvider: jest.fn(),
    resetDateProvider: jest.fn(),
    _test: {},
  });
  return mod;
});

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(),
  updatePlayer: jest.fn(),
  addFreezeHistory: jest.fn(),
  getAllPlayers: jest.fn().mockResolvedValue([]),
}));

const { getPlayer, updatePlayer } = require('../src/services/dynamo.service');

// Import the admin route after mocking
const adminRoute = require('../src/routes/admin');

// Extract the POST /freezes/grant handler from the router
function getHandler() {
  const layer = adminRoute.stack.find(
    (l) => l.route && l.route.path === '/freezes/grant' && l.route.methods.post
  );
  return layer.route.stack[0].handle;
}

function mockRes() {
  const res = {
    statusCode: null,
    body: null,
    status(code) {
      this.statusCode = code;
      return this;
    },
    json(data) {
      this.body = data;
      return this;
    },
  };
  return res;
}

beforeEach(() => {
  jest.clearAllMocks();
  getPlayer.mockResolvedValue(null);
  updatePlayer.mockResolvedValue(undefined);
});

describe('POST /api/v1/admin/freezes/grant', () => {
  const handler = getHandler();

  it('should grant freezes to an existing player', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'p1',
      freezesAvailable: 1,
    });

    const req = { body: { playerId: 'p1', count: 3 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(200);
    expect(res.body.playerId).toBe('p1');
    expect(res.body.freezesAvailable).toBe(4);
    expect(res.body.granted).toBe(3);
  });

  it('should return 400 when playerId is missing', async () => {
    const req = { body: { count: 3 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(400);
    expect(res.body.error).toBe('Bad Request');
  });

  it('should return 400 when count is missing', async () => {
    const req = { body: { playerId: 'p1' } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(400);
    expect(res.body.error).toBe('Bad Request');
  });

  it('should return 400 when count is not a positive integer', async () => {
    const req = { body: { playerId: 'p1', count: 0 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(400);
    expect(res.body.message).toContain('positive integer');
  });

  it('should return 400 when count is negative', async () => {
    const req = { body: { playerId: 'p1', count: -1 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(400);
  });

  it('should return 400 when count is a float', async () => {
    const req = { body: { playerId: 'p1', count: 2.5 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(400);
  });

  it('should return 404 when player does not exist', async () => {
    getPlayer.mockResolvedValue(null);

    const req = { body: { playerId: 'unknown', count: 1 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(404);
    expect(res.body.error).toBe('Not Found');
    expect(res.body.message).toBe('Player not found');
  });

  it('should return 500 on unexpected error', async () => {
    getPlayer.mockRejectedValue(new Error('DynamoDB down'));

    const req = { body: { playerId: 'p1', count: 1 } };
    const res = mockRes();
    await handler(req, res);

    expect(res.statusCode).toBe(500);
    expect(res.body.error).toBe('Internal server error');
  });
});
