'use strict';

/**
 * JWT auth middleware (stub).
 * Decodes a JWT from the Authorization: Bearer header and extracts playerId.
 * Verification is stubbed — no signature check.
 */
function authMiddleware(req, res, next) {
  const authHeader = req.headers['authorization'];

  if (!authHeader) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Authorization header is required',
    });
  }

  const parts = authHeader.split(' ');
  if (parts.length !== 2 || parts[0] !== 'Bearer') {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Authorization header must be Bearer <token>',
    });
  }

  const token = parts[1];

  try {
    const segments = token.split('.');
    if (segments.length !== 3) {
      throw new Error('Invalid token structure');
    }

    // Decode the payload (second segment) — stub, no signature verification
    const payload = JSON.parse(
      Buffer.from(segments[1], 'base64url').toString()
    );

    if (!payload.playerId) {
      throw new Error('playerId not found in token payload');
    }

    req.playerId = payload.playerId;
    req.displayName = payload.displayName || null;
    next();
  } catch (err) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Invalid or malformed token',
    });
  }
}

module.exports = { authMiddleware };
