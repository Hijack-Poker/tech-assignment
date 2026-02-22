'use strict';

/**
 * Stub auth middleware.
 * Extracts playerId from X-Player-Id header.
 */
function authMiddleware(req, res, next) {
  const playerId = req.headers['x-player-id'];

  if (!playerId) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'X-Player-Id header is required',
    });
  }

  req.playerId = playerId;
  next();
}

module.exports = { authMiddleware };
