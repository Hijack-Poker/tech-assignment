'use strict';

const { health } = require('../handler');

describe('Cash Game Broadcast Handler', () => {
  describe('health', () => {
    it('should return 200 with service info', async () => {
      const result = await health();

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.service).toBe('cash-game-broadcast');
      expect(body.status).toBe('ok');
    });
  });
});
