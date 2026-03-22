'use strict';

const { sequelize } = require('../shared/config/db');

jest.mock('../shared/config/db', () => ({
  sequelize: { query: jest.fn() },
}));
jest.mock('../shared/config/logger', () => ({
  logger: { info: jest.fn(), error: jest.fn(), warn: jest.fn() },
}));

const { fetchTable } = require('../lib/table-fetcher');

describe('table-fetcher', () => {
  afterEach(() => jest.resetAllMocks());

  describe('fetchTable', () => {
    it('should return null when table does not exist', async () => {
      // First query: no active game
      sequelize.query.mockResolvedValueOnce([]);
      // Second query (createNewGame): no game_tables row
      sequelize.query.mockResolvedValueOnce([]);

      const result = await fetchTable(999);

      expect(result).toBeNull();
    });

    it('should return game and players when data exists', async () => {
      const mockGame = {
        id: 1, table_id: 1, table_name: 'Starter Table', game_no: 1,
        hand_step: 0, dealer_seat: 1, small_blind_seat: 0, big_blind_seat: 0,
        community_cards: '[]', pot: '0.00', side_pots: '[]', move: 0,
        status: 'in_progress', small_blind: '1.00', big_blind: '2.00',
        max_seats: 6, deck: '[]', current_bet: '0.00', winners: '[]',
      };
      const mockPlayer = {
        id: 1, game_id: 1, table_id: 1, player_id: 1,
        guid: 'p1-uuid-0001', username: 'Alice', seat: 1,
        stack: '120.00', bet: '0.00', total_bet: '0.00',
        status: '1', action: '', cards: '[]', hand_rank: '', winnings: '0.00',
      };

      // First query: active game found
      sequelize.query.mockResolvedValueOnce([mockGame]);
      // Second query: players
      sequelize.query.mockResolvedValueOnce([mockPlayer]);

      const result = await fetchTable(1);

      expect(result).not.toBeNull();
      expect(result.game.tableId).toBe(1);
      expect(result.game.tableName).toBe('Starter Table');
      expect(result.players).toHaveLength(1);
      expect(result.players[0].username).toBe('Alice');
    });

    it('should throw when sequelize.query rejects', async () => {
      sequelize.query.mockRejectedValue(new Error('ECONNREFUSED'));

      await expect(fetchTable(1)).rejects.toThrow('ECONNREFUSED');
    });
  });
});
