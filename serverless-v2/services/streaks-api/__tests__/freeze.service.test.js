'use strict';

jest.mock('../src/services/dynamo.service', () => ({
  getPlayer: jest.fn(),
  updatePlayer: jest.fn(),
  addFreezeHistory: jest.fn(),
}));

const { getPlayer, updatePlayer, addFreezeHistory } = require('../src/services/dynamo.service');
const {
  checkMonthlyFreezeGrant,
  consumeFreeze,
  grantFreezes,
  MONTHLY_FREE_FREEZES,
} = require('../src/services/freeze.service');

beforeEach(() => {
  jest.clearAllMocks();
  getPlayer.mockResolvedValue(null);
  updatePlayer.mockResolvedValue(undefined);
  addFreezeHistory.mockResolvedValue(undefined);
});

describe('freeze.service — checkMonthlyFreezeGrant', () => {
  it('should grant 1 free freeze when lastFreezeGrantDate differs from current month', () => {
    const player = {
      playerId: 'p1',
      freezesAvailable: 0,
      freezesUsedThisMonth: 2,
      lastFreezeGrantDate: '2026-02',
    };

    const result = checkMonthlyFreezeGrant(player, '2026-03');

    expect(result.granted).toBe(true);
    expect(result.freezesAvailable).toBe(1);
    expect(result.freezesUsedThisMonth).toBe(0);
    expect(result.lastFreezeGrantDate).toBe('2026-03');
  });

  it('should not grant freeze if already granted this month', () => {
    const player = {
      playerId: 'p1',
      freezesAvailable: 1,
      freezesUsedThisMonth: 0,
      lastFreezeGrantDate: '2026-03',
    };

    const result = checkMonthlyFreezeGrant(player, '2026-03');

    expect(result.granted).toBe(false);
    expect(result.freezesAvailable).toBe(1);
    expect(result.freezesUsedThisMonth).toBe(0);
    expect(result.lastFreezeGrantDate).toBe('2026-03');
  });

  it('should add to existing freezes when granting monthly', () => {
    const player = {
      playerId: 'p1',
      freezesAvailable: 2,
      freezesUsedThisMonth: 1,
      lastFreezeGrantDate: '2026-02',
    };

    const result = checkMonthlyFreezeGrant(player, '2026-03');

    expect(result.granted).toBe(true);
    expect(result.freezesAvailable).toBe(3); // 2 existing + 1 granted
  });

  it('should handle empty lastFreezeGrantDate as new month', () => {
    const player = {
      playerId: 'p1',
      freezesAvailable: 0,
      freezesUsedThisMonth: 0,
      lastFreezeGrantDate: '',
    };

    const result = checkMonthlyFreezeGrant(player, '2026-03');

    expect(result.granted).toBe(true);
    expect(result.freezesAvailable).toBe(1);
  });

  it('should export MONTHLY_FREE_FREEZES as 1', () => {
    expect(MONTHLY_FREE_FREEZES).toBe(1);
  });
});

describe('freeze.service — consumeFreeze', () => {
  it('should decrement freezesAvailable and write freeze history', async () => {
    const result = await consumeFreeze('p1', '2026-03-20', 'free_monthly', 2, 0);

    expect(result.freezesAvailable).toBe(1);
    expect(result.freezesUsedThisMonth).toBe(1);
    expect(addFreezeHistory).toHaveBeenCalledWith('p1', '2026-03-20', 'free_monthly');
  });

  it('should track purchased source correctly', async () => {
    const result = await consumeFreeze('p1', '2026-03-20', 'purchased', 3, 1);

    expect(result.freezesAvailable).toBe(2);
    expect(result.freezesUsedThisMonth).toBe(2);
    expect(addFreezeHistory).toHaveBeenCalledWith('p1', '2026-03-20', 'purchased');
  });
});

describe('freeze.service — grantFreezes', () => {
  it('should increase freezesAvailable by the specified count', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'p1',
      freezesAvailable: 1,
    });

    const result = await grantFreezes('p1', 3);

    expect(result.playerId).toBe('p1');
    expect(result.freezesAvailable).toBe(4);
    expect(updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({
      freezesAvailable: 4,
    }));
  });

  it('should throw PLAYER_NOT_FOUND when player does not exist', async () => {
    getPlayer.mockResolvedValue(null);

    await expect(grantFreezes('unknown', 1)).rejects.toThrow('Player not found');

    try {
      await grantFreezes('unknown', 1);
    } catch (err) {
      expect(err.code).toBe('PLAYER_NOT_FOUND');
    }
  });

  it('should handle player with zero existing freezes', async () => {
    getPlayer.mockResolvedValue({
      playerId: 'p1',
      freezesAvailable: 0,
    });

    const result = await grantFreezes('p1', 5);

    expect(result.freezesAvailable).toBe(5);
  });
});
