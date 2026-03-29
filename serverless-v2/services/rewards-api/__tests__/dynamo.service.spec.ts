import { DynamoService } from '../src/dynamo/dynamo.service';

// Mock the shared DynamoDB config
const mockSend = jest.fn();
jest.mock('../../../shared/config/dynamo', () => ({
  docClient: { send: (...args: unknown[]) => mockSend(...args) },
}));

describe('DynamoService', () => {
  let service: DynamoService;

  beforeEach(() => {
    service = new DynamoService();
    mockSend.mockReset();
  });

  describe('getPlayer', () => {
    it('returns a player record when found', async () => {
      const player = { playerId: 'p1', username: 'Ace', tier: 1, points: 100, totalEarned: 200 };
      mockSend.mockResolvedValue({ Item: player });

      const result = await service.getPlayer('p1');
      expect(result).toEqual(player);
    });

    it('returns null when player not found', async () => {
      mockSend.mockResolvedValue({});

      const result = await service.getPlayer('nonexistent');
      expect(result).toBeNull();
    });

    it('passes correct table name and key', async () => {
      mockSend.mockResolvedValue({});
      await service.getPlayer('p1');

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-players');
      expect(command.input.Key).toEqual({ playerId: 'p1' });
    });
  });

  describe('getPlayers', () => {
    it('returns empty map for empty input', async () => {
      const result = await service.getPlayers([]);
      expect(result.size).toBe(0);
      expect(mockSend).not.toHaveBeenCalled();
    });

    it('returns a map of player records', async () => {
      const players = [
        { playerId: 'p1', username: 'Ace' },
        { playerId: 'p2', username: 'King' },
      ];
      mockSend.mockResolvedValue({
        Responses: { 'rewards-players': players },
      });

      const result = await service.getPlayers(['p1', 'p2']);
      expect(result.size).toBe(2);
      expect(result.get('p1')?.username).toBe('Ace');
      expect(result.get('p2')?.username).toBe('King');
    });

    it('handles missing players gracefully', async () => {
      mockSend.mockResolvedValue({
        Responses: { 'rewards-players': [{ playerId: 'p1', username: 'Ace' }] },
      });

      const result = await service.getPlayers(['p1', 'p2']);
      expect(result.size).toBe(1);
      expect(result.has('p2')).toBe(false);
    });
  });

  describe('putPlayer', () => {
    it('sends PutCommand with player data', async () => {
      mockSend.mockResolvedValue({});
      const player = { playerId: 'p1', username: 'Ace' } as any;

      await service.putPlayer(player);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-players');
      expect(command.input.Item).toEqual(player);
    });
  });

  describe('updatePlayer', () => {
    it('builds correct SET expression for single attribute', async () => {
      mockSend.mockResolvedValue({});
      await service.updatePlayer('p1', { points: 500 });

      const command = mockSend.mock.calls[0][0];
      expect(command.input.Key).toEqual({ playerId: 'p1' });
      expect(command.input.UpdateExpression).toBe('SET #k0 = :v0');
      expect(command.input.ExpressionAttributeNames['#k0']).toBe('points');
      expect(command.input.ExpressionAttributeValues[':v0']).toBe(500);
    });

    it('builds correct SET expression for multiple attributes', async () => {
      mockSend.mockResolvedValue({});
      await service.updatePlayer('p1', { points: 500, tier: 2 as any });

      const command = mockSend.mock.calls[0][0];
      expect(command.input.UpdateExpression).toBe('SET #k0 = :v0, #k1 = :v1');
    });
  });

  describe('addTransaction', () => {
    it('writes transaction with playerId and timestamp', async () => {
      mockSend.mockResolvedValue({});
      const now = Date.now();

      await service.addTransaction('p1', {
        type: 'gameplay',
        basePoints: 5,
        multiplier: 1.5,
        earnedPoints: 8,
        monthKey: '2026-03',
        createdAt: '2026-03-28T00:00:00Z',
      } as any);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-transactions');
      expect(command.input.Item.playerId).toBe('p1');
      expect(command.input.Item.timestamp).toBeGreaterThanOrEqual(now);
      expect(command.input.Item.type).toBe('gameplay');
    });
  });

  describe('getTransactions', () => {
    it('queries in reverse chronological order', async () => {
      mockSend.mockResolvedValue({ Items: [], LastEvaluatedKey: undefined });

      await service.getTransactions('p1');

      const command = mockSend.mock.calls[0][0];
      expect(command.input.ScanIndexForward).toBe(false);
      expect(command.input.Limit).toBe(20);
    });

    it('passes cursor as ExclusiveStartKey', async () => {
      mockSend.mockResolvedValue({ Items: [], LastEvaluatedKey: undefined });
      const cursor = { playerId: 'p1', timestamp: 12345 };

      await service.getTransactions('p1', 10, cursor);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.ExclusiveStartKey).toEqual(cursor);
      expect(command.input.Limit).toBe(10);
    });

    it('returns items and lastKey', async () => {
      const items = [{ playerId: 'p1', timestamp: 100 }];
      const lastKey = { playerId: 'p1', timestamp: 100 };
      mockSend.mockResolvedValue({ Items: items, LastEvaluatedKey: lastKey });

      const result = await service.getTransactions('p1');
      expect(result.items).toEqual(items);
      expect(result.lastKey).toEqual(lastKey);
    });
  });

  describe('countTransactions', () => {
    it('returns count from DynamoDB', async () => {
      mockSend.mockResolvedValue({ Count: 42 });
      const result = await service.countTransactions('p1');
      expect(result).toBe(42);
    });

    it('returns 0 when no transactions exist', async () => {
      mockSend.mockResolvedValue({});
      const result = await service.countTransactions('p1');
      expect(result).toBe(0);
    });
  });

  describe('getAllPlayers', () => {
    it('returns all player records', async () => {
      const players = [{ playerId: 'p1' }, { playerId: 'p2' }];
      mockSend.mockResolvedValue({ Items: players });

      const result = await service.getAllPlayers();
      expect(result).toEqual(players);
    });

    it('returns empty array when table is empty', async () => {
      mockSend.mockResolvedValue({});
      const result = await service.getAllPlayers();
      expect(result).toEqual([]);
    });
  });

  describe('addNotification', () => {
    it('writes notification to correct table', async () => {
      mockSend.mockResolvedValue({});
      const notification = {
        playerId: 'p1',
        notificationId: 'n1',
        type: 'tier_upgrade' as const,
        title: 'Upgraded!',
        description: 'You reached Gold',
        dismissed: false,
        createdAt: '2026-03-28T00:00:00Z',
      };

      await service.addNotification(notification);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-notifications');
      expect(command.input.Item).toEqual(notification);
    });
  });

  describe('getNotifications', () => {
    it('queries notifications in reverse order', async () => {
      const notifications = [
        { playerId: 'p1', notificationId: 'n2', type: 'milestone', title: 'Second' },
        { playerId: 'p1', notificationId: 'n1', type: 'tier_upgrade', title: 'First' },
      ];
      mockSend.mockResolvedValue({ Items: notifications });

      const result = await service.getNotifications('p1');

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-notifications');
      expect(command.input.ScanIndexForward).toBe(false);
      expect(result).toEqual(notifications);
    });

    it('applies dismissed filter when unreadOnly is true', async () => {
      mockSend.mockResolvedValue({ Items: [] });

      await service.getNotifications('p1', true);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.FilterExpression).toBe('dismissed = :false');
      expect(command.input.ExpressionAttributeValues[':false']).toBe(false);
    });

    it('does not filter when unreadOnly is false', async () => {
      mockSend.mockResolvedValue({ Items: [] });

      await service.getNotifications('p1', false);

      const command = mockSend.mock.calls[0][0];
      expect(command.input.FilterExpression).toBeUndefined();
    });

    it('returns empty array when no items', async () => {
      mockSend.mockResolvedValue({});
      const result = await service.getNotifications('p1');
      expect(result).toEqual([]);
    });
  });

  describe('dismissNotification', () => {
    it('sets dismissed to true with condition check', async () => {
      mockSend.mockResolvedValue({});

      await service.dismissNotification('p1', 'n1');

      const command = mockSend.mock.calls[0][0];
      expect(command.input.TableName).toBe('rewards-notifications');
      expect(command.input.Key).toEqual({ playerId: 'p1', notificationId: 'n1' });
      expect(command.input.UpdateExpression).toBe('SET dismissed = :true');
      expect(command.input.ExpressionAttributeValues[':true']).toBe(true);
      expect(command.input.ConditionExpression).toBe('attribute_exists(playerId)');
    });

    it('throws when notification does not exist', async () => {
      const { ConditionalCheckFailedException } = require('@aws-sdk/client-dynamodb');
      mockSend.mockRejectedValue(new ConditionalCheckFailedException({ message: 'Condition not met', $metadata: {} }));

      await expect(service.dismissNotification('p1', 'nonexistent')).rejects.toThrow();
    });
  });
});
