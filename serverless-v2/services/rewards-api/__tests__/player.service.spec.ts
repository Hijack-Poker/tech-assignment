import { NotFoundException } from '@nestjs/common';
import { ConditionalCheckFailedException } from '@aws-sdk/client-dynamodb';
import { PlayerService } from '../src/player/player.service';
import { DynamoService } from '../src/dynamo/dynamo.service';
import { PlayerRecord, TransactionRecord } from '../../../../shared/types/rewards';

// Prevent actual AWS connections
jest.mock('../../../shared/config/dynamo', () => ({ docClient: { send: jest.fn() } }));

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

function makeTransaction(overrides: Partial<TransactionRecord> = {}): TransactionRecord {
  return {
    playerId: 'p1',
    timestamp: 1000000,
    type: 'gameplay',
    basePoints: 5,
    multiplier: 1.0,
    earnedPoints: 5,
    monthKey: '2026-03',
    createdAt: '2026-03-01T00:00:00Z',
    balanceAfter: 105,
    ...overrides,
  };
}

describe('PlayerService', () => {
  let service: PlayerService;
  let dynamo: jest.Mocked<DynamoService>;

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
    } as unknown as jest.Mocked<DynamoService>;

    service = new PlayerService(dynamo);
  });

  describe('getRewards', () => {
    it('throws NotFoundException when player does not exist', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.getRewards('p1')).rejects.toThrow(NotFoundException);
    });

    it('returns correct tier name for Bronze player', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 1 }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getRewards('p1');
      expect(result.tier).toBe('Bronze');
      expect(result.nextTierName).toBe('Silver');
      expect(result.nextTierAt).toBe(500);
    });

    it('returns null nextTier for Platinum player', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ tier: 4 }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getRewards('p1');
      expect(result.tier).toBe('Platinum');
      expect(result.nextTierName).toBeNull();
      expect(result.nextTierAt).toBeNull();
    });

    it('includes points and totalEarned', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer({ points: 250, totalEarned: 1200 }));
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getRewards('p1');
      expect(result.points).toBe(250);
      expect(result.totalEarned).toBe(1200);
    });

    it('fetches and maps recent transactions', async () => {
      const tx = makeTransaction({ basePoints: 10, earnedPoints: 15, multiplier: 1.5 });
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [tx], lastKey: undefined });

      const result = await service.getRewards('p1');
      expect(result.recentTransactions).toHaveLength(1);
      expect(result.recentTransactions[0]).toEqual(
        expect.objectContaining({
          basePoints: 10,
          earnedPoints: 15,
          multiplier: 1.5,
          type: 'gameplay',
        }),
      );
    });

    it('requests only 10 recent transactions', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      await service.getRewards('p1');
      expect(dynamo.getTransactions).toHaveBeenCalledWith('p1', 10);
    });

    it('includes playerId in response', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });

      const result = await service.getRewards('p1');
      expect(result.playerId).toBe('p1');
    });
  });

  describe('getNotifications', () => {
    it('returns notifications with unread count', async () => {
      dynamo.getNotifications.mockResolvedValue([
        { playerId: 'p1', notificationId: 'n1', type: 'tier_upgrade', title: 'Upgraded!', description: 'Nice', dismissed: false, createdAt: '2026-03-01T00:00:00Z' },
        { playerId: 'p1', notificationId: 'n2', type: 'milestone', title: 'First Hand!', description: 'Welcome', dismissed: true, createdAt: '2026-02-01T00:00:00Z' },
      ]);

      const result = await service.getNotifications('p1', false);
      expect(result.notifications).toHaveLength(2);
      expect(result.unreadCount).toBe(1);
    });

    it('passes unreadOnly flag to dynamo', async () => {
      dynamo.getNotifications.mockResolvedValue([]);

      await service.getNotifications('p1', true);
      expect(dynamo.getNotifications).toHaveBeenCalledWith('p1', true);
    });

    it('maps notification fields correctly', async () => {
      dynamo.getNotifications.mockResolvedValue([
        { playerId: 'p1', notificationId: 'n1', type: 'milestone', title: 'Test', description: 'Desc', dismissed: false, createdAt: '2026-03-01T00:00:00Z' },
      ]);

      const result = await service.getNotifications('p1', false);
      const n = result.notifications[0];
      expect(n.notificationId).toBe('n1');
      expect(n.type).toBe('milestone');
      expect(n.title).toBe('Test');
      expect(n).not.toHaveProperty('playerId');
    });

    it('sets unreadCount to length when unreadOnly is true', async () => {
      dynamo.getNotifications.mockResolvedValue([
        { playerId: 'p1', notificationId: 'n1', type: 'tier_upgrade', title: 'Up', description: 'D', dismissed: false, createdAt: '2026-03-01T00:00:00Z' },
      ]);

      const result = await service.getNotifications('p1', true);
      expect(result.unreadCount).toBe(1);
    });
  });

  describe('dismissNotification', () => {
    it('returns success on valid dismiss', async () => {
      dynamo.dismissNotification.mockResolvedValue();

      const result = await service.dismissNotification('p1', 'n1');
      expect(result).toEqual({ success: true });
      expect(dynamo.dismissNotification).toHaveBeenCalledWith('p1', 'n1');
    });

    it('throws NotFoundException when notification does not exist', async () => {
      dynamo.dismissNotification.mockRejectedValue(
        new ConditionalCheckFailedException({ message: 'Condition not met', $metadata: {} }),
      );

      await expect(service.dismissNotification('p1', 'nonexistent')).rejects.toThrow(NotFoundException);
    });

    it('re-throws non-conditional errors', async () => {
      dynamo.dismissNotification.mockRejectedValue(new Error('Network error'));

      await expect(service.dismissNotification('p1', 'n1')).rejects.toThrow('Network error');
    });
  });

  describe('getHistory', () => {
    it('throws NotFoundException when player does not exist', async () => {
      dynamo.getPlayer.mockResolvedValue(null);
      await expect(service.getHistory('p1', 20)).rejects.toThrow(NotFoundException);
    });

    it('returns first page without cursor', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [makeTransaction()], lastKey: undefined });
      dynamo.countTransactions.mockResolvedValue(1);

      const result = await service.getHistory('p1', 20);
      expect(result.transactions).toHaveLength(1);
      expect(result.total).toBe(1);
      expect(result.limit).toBe(20);
      expect(result.cursor).toBeNull();
    });

    it('decodes base64url cursor and passes to DynamoDB', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [], lastKey: undefined });
      dynamo.countTransactions.mockResolvedValue(0);

      const cursorData = { playerId: 'p1', timestamp: 12345 };
      const encodedCursor = Buffer.from(JSON.stringify(cursorData)).toString('base64url');

      await service.getHistory('p1', 20, encodedCursor);
      expect(dynamo.getTransactions).toHaveBeenCalledWith('p1', 20, cursorData);
    });

    it('encodes lastKey as base64url cursor when more results exist', async () => {
      const lastKey = { playerId: 'p1', timestamp: 99999 };
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [makeTransaction()], lastKey });
      dynamo.countTransactions.mockResolvedValue(50);

      const result = await service.getHistory('p1', 20);
      expect(result.cursor).toBeTruthy();

      // Verify cursor decodes back to the original lastKey
      const decoded = JSON.parse(Buffer.from(result.cursor!, 'base64url').toString());
      expect(decoded).toEqual(lastKey);
    });

    it('returns null cursor on last page', async () => {
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [makeTransaction()], lastKey: undefined });
      dynamo.countTransactions.mockResolvedValue(1);

      const result = await service.getHistory('p1', 20);
      expect(result.cursor).toBeNull();
    });

    it('maps transaction fields correctly', async () => {
      const tx = makeTransaction({
        tableId: 5,
        tableStakes: '5/10',
        reason: 'hand_won',
        balanceAfter: 300,
      });
      dynamo.getPlayer.mockResolvedValue(makePlayer());
      dynamo.getTransactions.mockResolvedValue({ items: [tx], lastKey: undefined });
      dynamo.countTransactions.mockResolvedValue(1);

      const result = await service.getHistory('p1', 20);
      expect(result.transactions[0]).toEqual(
        expect.objectContaining({
          tableId: 5,
          tableStakes: '5/10',
          reason: 'hand_won',
          balanceAfter: 300,
        }),
      );
    });
  });
});
