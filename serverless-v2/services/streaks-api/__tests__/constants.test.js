'use strict';

const { MILESTONES, getMilestone, getAchievedMilestones } = require('../src/config/constants');

describe('Streaks — Constants', () => {
  describe('MILESTONES', () => {
    it('should define milestones at expected day thresholds', () => {
      const days = MILESTONES.map((m) => m.days);
      expect(days).toEqual([3, 7, 14, 30, 60, 90]);
    });

    it('should have increasing rewards for each milestone', () => {
      for (let i = 1; i < MILESTONES.length; i++) {
        expect(MILESTONES[i].loginReward).toBeGreaterThan(MILESTONES[i - 1].loginReward);
        expect(MILESTONES[i].playReward).toBeGreaterThan(MILESTONES[i - 1].playReward);
      }
    });
  });

  describe('getMilestone', () => {
    it('should return the milestone for an exact day match', () => {
      const milestone = getMilestone(7);
      expect(milestone).not.toBeNull();
      expect(milestone.days).toBe(7);
      expect(milestone.loginReward).toBe(150);
      expect(milestone.playReward).toBe(300);
    });

    it('should return null for a non-milestone day', () => {
      expect(getMilestone(5)).toBeNull();
      expect(getMilestone(0)).toBeNull();
      expect(getMilestone(100)).toBeNull();
    });
  });

  describe('getAchievedMilestones', () => {
    it('should return all milestones up to the given streak length', () => {
      const achieved = getAchievedMilestones(14);
      expect(achieved).toHaveLength(3);
      expect(achieved.map((m) => m.days)).toEqual([3, 7, 14]);
    });

    it('should return empty array for streak below first milestone', () => {
      expect(getAchievedMilestones(2)).toEqual([]);
      expect(getAchievedMilestones(0)).toEqual([]);
    });

    it('should return all milestones for streak of 90+', () => {
      const achieved = getAchievedMilestones(90);
      expect(achieved).toHaveLength(6);
    });
  });
});
