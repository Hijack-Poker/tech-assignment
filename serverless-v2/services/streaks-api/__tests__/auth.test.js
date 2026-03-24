'use strict';

const { authMiddleware } = require('../src/middleware/auth');

/**
 * Helper — build a minimal JWT token (no real signature).
 */
function createToken(payload) {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const body = Buffer.from(JSON.stringify(payload)).toString('base64url');
  return `${header}.${body}.stub-signature`;
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

describe('JWT auth middleware', () => {
  it('should extract playerId from a valid JWT and attach to req', () => {
    const token = createToken({ playerId: 'player-42' });
    const req = { headers: { authorization: `Bearer ${token}` } };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).toHaveBeenCalled();
    expect(req.playerId).toBe('player-42');
  });

  it('should return 401 when Authorization header is missing', () => {
    const req = { headers: {} };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).not.toHaveBeenCalled();
    expect(res.statusCode).toBe(401);
    expect(res.body.error).toBe('Unauthorized');
    expect(res.body.message).toMatch(/Authorization header is required/);
  });

  it('should return 401 when Authorization header is not Bearer scheme', () => {
    const req = { headers: { authorization: 'Basic abc123' } };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).not.toHaveBeenCalled();
    expect(res.statusCode).toBe(401);
    expect(res.body.message).toMatch(/Bearer/);
  });

  it('should return 401 for a malformed token (not 3 segments)', () => {
    const req = { headers: { authorization: 'Bearer not-a-jwt' } };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).not.toHaveBeenCalled();
    expect(res.statusCode).toBe(401);
    expect(res.body.message).toMatch(/Invalid or malformed token/);
  });

  it('should return 401 when token payload has no playerId', () => {
    const token = createToken({ sub: 'someone', role: 'user' });
    const req = { headers: { authorization: `Bearer ${token}` } };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).not.toHaveBeenCalled();
    expect(res.statusCode).toBe(401);
    expect(res.body.message).toMatch(/Invalid or malformed token/);
  });

  it('should return 401 when payload is not valid JSON', () => {
    const header = Buffer.from('{"alg":"none"}').toString('base64url');
    const badPayload = Buffer.from('not-json').toString('base64url');
    const token = `${header}.${badPayload}.sig`;
    const req = { headers: { authorization: `Bearer ${token}` } };
    const res = mockRes();
    const next = jest.fn();

    authMiddleware(req, res, next);

    expect(next).not.toHaveBeenCalled();
    expect(res.statusCode).toBe(401);
  });
});
