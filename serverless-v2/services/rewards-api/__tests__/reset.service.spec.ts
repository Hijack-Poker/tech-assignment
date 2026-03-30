import { ResetService } from '../src/dev/reset.service';
import { DynamoService } from '../src/dynamo/dynamo.service';
import { RedisService } from '../src/redis/redis.service';
import { PlayerRecord } from '../../../../shared/types/rewards';

jest.mock('../../../shared/config/dynamo', () => ({ docClient: { send: jest.fn() } }));
jest.mock('../../../shared/config/redis', () => ({ redisClient: {} }));

function makePlayer(overrides: Partial<PlayerRecord> = {}): PlayerRecord {
  return {
    playerId: 'p1',
    username: 'TestPlayer',
    tier: 1,
    points: 100,
    totalEarned: 400,
    handsPlayed: 10,
    tournamentsPlayed: 0,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-03-01T00:00:00Z',
    ...overrides,
  };
}

describe('ResetService', () => {
  let service: ResetService;
  let dynamo: jest.Mocked<DynamoService>;
  let redis: jest.Mocked<RedisService>;

  beforeEach(() => {
    dynamo = {
      getPlayer: jest.fn(),
      getPlayers: jest.fn(),
      putPlayer: jest.fn(),
      updatePlayer: jest.fn(),
      addTransaction: jest.fn(),
      getTransactions: jest.fn(),
      countTransactions: jest.fn(),
      getAllPlayers: jest.fn(),
      addNotification: jest.fn(),
      getNotifications: jest.fn(),
      dismissNotification: jest.fn(),
      putTierHistory: jest.fn(),
      getTierHistory: jest.fn(),
    } as unknown as jest.Mocked<DynamoService>;

    redis = {
      updateLeaderboard: jest.fn(),
      getTopPlayers: jest.fn(),
      getPlayersAroundRank: jest.fn(),
      getPlayerRank: jest.fn(),
    } as unknown as jest.Mocked<RedisService>;

    service = new ResetService(dynamo, redis);
  });

  it('resets all players points to 0', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 1, points: 500 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();

    await service.runMonthlyReset();

    expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ points: 0 }));
  });

  it('applies tier floor protection: Platinum drops to Gold', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 4, points: 15000 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();
    dynamo.addNotification.mockResolvedValue();

    const result = await service.runMonthlyReset();

    expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 3 }));
    expect(result.downgrades).toBe(1);
  });

  it('applies tier floor protection: Silver drops to Bronze', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 2, points: 800 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();
    dynamo.addNotification.mockResolvedValue();

    await service.runMonthlyReset();

    expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 1 }));
  });

  it('Bronze stays at Bronze (floor is 1)', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 1, points: 100 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();

    const result = await service.runMonthlyReset();

    expect(dynamo.updatePlayer).toHaveBeenCalledWith('p1', expect.objectContaining({ tier: 1 }));
    expect(result.downgrades).toBe(0);
  });

  it('creates downgrade notifications for affected players', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 3, points: 3000 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();
    dynamo.addNotification.mockResolvedValue();

    await service.runMonthlyReset();

    expect(dynamo.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({
        playerId: 'p1',
        type: 'tier_downgrade',
        title: expect.stringContaining('Silver'),
      }),
    );
  });

  it('does not create notification when tier stays the same', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1', tier: 1, points: 50 })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();

    await service.runMonthlyReset();

    expect(dynamo.addNotification).not.toHaveBeenCalled();
  });

  it('writes tier history snapshots for all players', async () => {
    dynamo.getAllPlayers.mockResolvedValue([
      makePlayer({ playerId: 'p1', tier: 2, points: 600 }),
      makePlayer({ playerId: 'p2', tier: 3, points: 3000 }),
    ]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();
    dynamo.addNotification.mockResolvedValue();

    await service.runMonthlyReset();

    expect(dynamo.putTierHistory).toHaveBeenCalledTimes(2);
    expect(dynamo.putTierHistory).toHaveBeenCalledWith(
      expect.objectContaining({ playerId: 'p1', reason: 'monthly_reset' }),
    );
    expect(dynamo.putTierHistory).toHaveBeenCalledWith(
      expect.objectContaining({ playerId: 'p2', reason: 'monthly_reset' }),
    );
  });

  it('updates Redis leaderboard for new month', async () => {
    dynamo.getAllPlayers.mockResolvedValue([makePlayer({ playerId: 'p1' })]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();

    await service.runMonthlyReset();

    expect(redis.updateLeaderboard).toHaveBeenCalledWith(
      expect.stringMatching(/^\d{4}-\d{2}$/),
      'p1',
      0,
    );
  });

  it('returns correct summary', async () => {
    dynamo.getAllPlayers.mockResolvedValue([
      makePlayer({ playerId: 'p1', tier: 1 }),
      makePlayer({ playerId: 'p2', tier: 3 }),
      makePlayer({ playerId: 'p3', tier: 4 }),
    ]);
    dynamo.putTierHistory.mockResolvedValue();
    dynamo.updatePlayer.mockResolvedValue();
    dynamo.addNotification.mockResolvedValue();

    const result = await service.runMonthlyReset();

    expect(result.processed).toBe(3);
    expect(result.downgrades).toBe(2); // p2 Gold→Silver, p3 Platinum→Gold
    expect(result.resetMonth).toMatch(/^\d{4}-\d{2}$/);
  });

  it('handles empty player list gracefully', async () => {
    dynamo.getAllPlayers.mockResolvedValue([]);

    const result = await service.runMonthlyReset();

    expect(result.processed).toBe(0);
    expect(result.downgrades).toBe(0);
  });
});
