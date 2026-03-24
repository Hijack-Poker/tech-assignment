import { MILESTONES, getMilestone, getAchievedMilestones } from '../src/config/milestones';

describe('milestones config', () => {
  it('defines 6 milestone tiers', () => {
    expect(MILESTONES).toHaveLength(6);
  });

  it('milestone days are 3, 7, 14, 30, 60, 90', () => {
    const days = MILESTONES.map((m) => m.days);
    expect(days).toEqual([3, 7, 14, 30, 60, 90]);
  });

  it('each milestone has loginReward and playReward', () => {
    for (const m of MILESTONES) {
      expect(m.loginReward).toBeGreaterThan(0);
      expect(m.playReward).toBeGreaterThan(0);
      expect(m.playReward).toBeGreaterThan(m.loginReward);
    }
  });
});

describe('getMilestone', () => {
  it('returns the milestone for an exact match', () => {
    const m = getMilestone(7);
    expect(m).not.toBeNull();
    expect(m!.days).toBe(7);
    expect(m!.loginReward).toBe(150);
    expect(m!.playReward).toBe(300);
  });

  it('returns null when no milestone matches', () => {
    expect(getMilestone(5)).toBeNull();
    expect(getMilestone(0)).toBeNull();
    expect(getMilestone(100)).toBeNull();
  });
});

describe('getAchievedMilestones', () => {
  it('returns all milestones up to the given streak length', () => {
    const achieved = getAchievedMilestones(14);
    expect(achieved).toHaveLength(3);
    expect(achieved.map((m) => m.days)).toEqual([3, 7, 14]);
  });

  it('returns empty array for streak below first milestone', () => {
    expect(getAchievedMilestones(2)).toEqual([]);
  });

  it('returns all milestones for very long streaks', () => {
    const achieved = getAchievedMilestones(365);
    expect(achieved).toHaveLength(6);
  });
});
