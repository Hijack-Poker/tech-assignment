'use strict';

// Mock database and event modules
jest.mock('../lib/table-fetcher', () => ({
  fetchTable: jest.fn(),
  saveGame: jest.fn(),
  savePlayers: jest.fn(),
}));
jest.mock('../lib/event-publisher', () => ({
  publishTableUpdate: jest.fn(),
}));

const { health } = require('../handler');

describe('Handler', () => {
  describe('health', () => {
    it('should return 200 with service info', async () => {
      const result = await health();

      expect(result.statusCode).toBe(200);
      const body = JSON.parse(result.body);
      expect(body.service).toBe('holdem-processor');
      expect(body.status).toBe('ok');
      expect(body.timestamp).toBeDefined();
    });
  });
});
