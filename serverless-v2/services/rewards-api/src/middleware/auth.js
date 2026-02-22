'use strict';

/**
 * Stub auth middleware.
 *
 * In production, this validates a JWT token. For the tech assignment,
 * it simply extracts playerId from the X-Player-Id header.
 *
 * Candidates may enhance this with real JWT validation if they choose.
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
