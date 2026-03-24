'use strict';

jest.mock('uuid', () => ({
  v4: jest.fn(() => 'test-uuid-1234'),
}));

jest.mock('../shared/config/dynamo', () => {
  const sendMock = jest.fn();
  return {
    docClient: { send: sendMock },
    __sendMock: sendMock,
  };
});

const { __sendMock: sendMock } = require('../shared/config/dynamo');
const { checkAndAwardMilestone, buildStreakAtRiskNotification } = require('../src/services/rewards.service');

beforeEach(() => {
  sendMock.mockReset();
  jest.spyOn(console, 'log').mockImplementation(() => {});
});

afterEach(() => {
  jest.restoreAllMocks();
});

describe('Rewards Service — checkAndAwardMilestone', () => {
  describe('login milestones', () => {
    it('should award 50 points for a 3-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 3, 'login');

      expect(reward).not.toBeNull();
      expect(reward.playerId).toBe('player-1');
      expect(reward.rewardId).toBe('test-uuid-1234');
      expect(reward.type).toBe('login_milestone');
      expect(reward.milestone).toBe(3);
      expect(reward.points).toBe(50);
      expect(reward.streakCount).toBe(3);
      expect(reward.createdAt).toBeDefined();
      expect(sendMock).toHaveBeenCalledTimes(1);
    });

    it('should award 150 points for a 7-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 7, 'login');

      expect(reward).not.toBeNull();
      expect(reward.type).toBe('login_milestone');
      expect(reward.milestone).toBe(7);
      expect(reward.points).toBe(150);
    });

    it('should award 400 points for a 14-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 14, 'login');
      expect(reward.points).toBe(400);
    });

    it('should award 1000 points for a 30-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 30, 'login');
      expect(reward.points).toBe(1000);
    });

    it('should award 2500 points for a 60-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 60, 'login');
      expect(reward.points).toBe(2500);
    });

    it('should award 5000 points for a 90-day login streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 90, 'login');
      expect(reward.points).toBe(5000);
    });
  });

  describe('play milestones', () => {
    it('should award 100 points for a 3-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 3, 'play');

      expect(reward).not.toBeNull();
      expect(reward.type).toBe('play_milestone');
      expect(reward.milestone).toBe(3);
      expect(reward.points).toBe(100);
      expect(reward.streakCount).toBe(3);
    });

    it('should award 300 points for a 7-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 7, 'play');

      expect(reward.type).toBe('play_milestone');
      expect(reward.milestone).toBe(7);
      expect(reward.points).toBe(300);
    });

    it('should award 800 points for a 14-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 14, 'play');
      expect(reward.points).toBe(800);
    });

    it('should award 2000 points for a 30-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 30, 'play');
      expect(reward.points).toBe(2000);
    });

    it('should award 5000 points for a 60-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 60, 'play');
      expect(reward.points).toBe(5000);
    });

    it('should award 10000 points for a 90-day play streak', async () => {
      sendMock.mockResolvedValueOnce({});
      const reward = await checkAndAwardMilestone('player-1', 90, 'play');
      expect(reward.points).toBe(10000);
    });
  });

  describe('non-milestone streak counts', () => {
    it('should return null for streak count 1 (no milestone)', async () => {
      const reward = await checkAndAwardMilestone('player-1', 1, 'login');

      expect(reward).toBeNull();
      expect(sendMock).not.toHaveBeenCalled();
    });

    it('should return null for streak count 5 (not a milestone)', async () => {
      const reward = await checkAndAwardMilestone('player-1', 5, 'play');

      expect(reward).toBeNull();
      expect(sendMock).not.toHaveBeenCalled();
    });

    it('should return null for streak count 8 (between milestones)', async () => {
      const reward = await checkAndAwardMilestone('player-1', 8, 'login');

      expect(reward).toBeNull();
      expect(sendMock).not.toHaveBeenCalled();
    });

    it('should return null for streak count 100 (beyond max milestone)', async () => {
      const reward = await checkAndAwardMilestone('player-1', 100, 'play');

      expect(reward).toBeNull();
      expect(sendMock).not.toHaveBeenCalled();
    });
  });

  describe('reward record structure', () => {
    it('should include all required fields in the reward record', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 7, 'login');

      expect(reward).toEqual({
        playerId: 'player-1',
        rewardId: 'test-uuid-1234',
        type: 'login_milestone',
        milestone: 7,
        points: 150,
        streakCount: 7,
        createdAt: expect.any(String),
        notification: {
          title: '7-Day Streak!',
          body: 'You earned 150 bonus points for your login streak!',
          type: 'streak_milestone',
        },
      });
    });

    it('should log a notification when a reward is awarded', async () => {
      sendMock.mockResolvedValueOnce({});

      await checkAndAwardMilestone('player-1', 3, 'login');

      expect(console.log).toHaveBeenCalledWith(
        expect.stringContaining('[NOTIFICATION]')
      );
    });
  });

  describe('notification field in reward', () => {
    it('should include a notification object in the reward for a login milestone', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 3, 'login');

      expect(reward.notification).toEqual({
        title: '3-Day Streak!',
        body: 'You earned 50 bonus points for your login streak!',
        type: 'streak_milestone',
      });
    });

    it('should include a notification object in the reward for a play milestone', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 7, 'play');

      expect(reward.notification).toEqual({
        title: '7-Day Streak!',
        body: 'You earned 300 bonus points for your play streak!',
        type: 'streak_milestone',
      });
    });

    it('should include correct points in notification body for high milestones', async () => {
      sendMock.mockResolvedValueOnce({});

      const reward = await checkAndAwardMilestone('player-1', 90, 'play');

      expect(reward.notification).toEqual({
        title: '90-Day Streak!',
        body: 'You earned 10000 bonus points for your play streak!',
        type: 'streak_milestone',
      });
    });
  });

  describe('buildStreakAtRiskNotification', () => {
    it('should return a streak at risk notification for a login streak', () => {
      const notification = buildStreakAtRiskNotification('login', 14);

      expect(notification).toEqual({
        title: 'Streak at Risk!',
        body: 'Your 14-day login streak will reset if you don\'t check in before midnight UTC!',
        type: 'streak_at_risk',
      });
    });

    it('should return a streak at risk notification for a play streak', () => {
      const notification = buildStreakAtRiskNotification('play', 7);

      expect(notification).toEqual({
        title: 'Streak at Risk!',
        body: 'Your 7-day play streak will reset if you don\'t check in before midnight UTC!',
        type: 'streak_at_risk',
      });
    });

    it('should include the current streak count in the body', () => {
      const notification = buildStreakAtRiskNotification('login', 42);

      expect(notification.body).toContain('42-day');
    });

    it('should always set type to streak_at_risk', () => {
      const notification = buildStreakAtRiskNotification('play', 1);

      expect(notification.type).toBe('streak_at_risk');
    });
  });

  describe('DynamoDB write failure', () => {
    it('should propagate errors from DynamoDB', async () => {
      sendMock.mockRejectedValueOnce(new Error('DynamoDB error'));

      await expect(
        checkAndAwardMilestone('player-1', 3, 'login')
      ).rejects.toThrow('DynamoDB error');
    });
  });
});
