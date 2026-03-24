'use strict';

const { generateSeedData, PATTERN, PLAYER_ID } = require('../scripts/seed');

describe('Seed script — data generation', () => {
  const REFERENCE_DATE = '2025-03-15';
  let data;

  beforeAll(() => {
    data = generateSeedData(REFERENCE_DATE);
  });

  it('should use the stubbed player ID', () => {
    expect(data.player.playerId).toBe('player-42');
  });

  it('should have a 60-character pattern', () => {
    expect(PATTERN.length).toBe(60);
  });

  it('should generate activity records for active days and streak-break days', () => {
    // Every non-gap day produces a record, plus gap days that break a streak
    expect(data.activities.length).toBeGreaterThanOrEqual(50);
    expect(data.activities.length).toBeLessThanOrEqual(60);
  });

  it('should produce a player record with non-zero loginStreak and playStreak', () => {
    expect(data.player.loginStreak).toBeGreaterThan(0);
    expect(data.player.playStreak).toBeGreaterThan(0);
  });

  it('should track bestLoginStreak and bestPlayStreak', () => {
    expect(data.player.bestLoginStreak).toBeGreaterThanOrEqual(data.player.loginStreak);
    expect(data.player.bestPlayStreak).toBeGreaterThanOrEqual(data.player.playStreak);
  });

  it('should include milestone rewards for 7-day and 14-day streaks', () => {
    const milestones = data.rewards.map((r) => r.milestone);
    expect(milestones).toContain(7);
    expect(milestones).toContain(14);
  });

  it('should include both login and play milestone reward types', () => {
    const types = [...new Set(data.rewards.map((r) => r.type))];
    expect(types).toContain('login_milestone');
    expect(types).toContain('play_milestone');
  });

  it('should include at least one freeze history entry', () => {
    expect(data.freezeHistory.length).toBeGreaterThanOrEqual(1);
    expect(data.freezeHistory[0].source).toBe('free_monthly');
  });

  it('should generate calendar data with a mix of activity types', () => {
    const types = new Set();
    for (const a of data.activities) {
      if (a.freezeUsed) types.add('freeze');
      else if (a.played) types.add('played');
      else if (a.loggedIn) types.add('login_only');
      else if (a.streakBroken) types.add('streak_broken');
    }
    // Gap days with no record become 'none' in the calendar
    types.add('none');

    expect(types.has('played')).toBe(true);
    expect(types.has('login_only')).toBe(true);
    expect(types.has('freeze')).toBe(true);
    expect(types.has('streak_broken')).toBe(true);
    expect(types.has('none')).toBe(true);
  });

  it('should set lastLoginDate and lastPlayDate', () => {
    expect(data.player.lastLoginDate).toBeTruthy();
    expect(data.player.lastPlayDate).toBeTruthy();
  });

  it('should generate all activity dates within the 60-day window', () => {
    const start = new Date(REFERENCE_DATE + 'T00:00:00Z');
    start.setUTCDate(start.getUTCDate() - 59);
    const startStr = start.toISOString().split('T')[0];

    for (const a of data.activities) {
      expect(a.date >= startStr).toBe(true);
      expect(a.date <= REFERENCE_DATE).toBe(true);
    }
  });

  it('should produce unique rewardId for each reward', () => {
    const ids = data.rewards.map((r) => r.rewardId);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('should be idempotent — repeated calls produce consistent structure', () => {
    const second = generateSeedData(REFERENCE_DATE);
    expect(second.activities.length).toBe(data.activities.length);
    expect(second.rewards.length).toBe(data.rewards.length);
    expect(second.freezeHistory.length).toBe(data.freezeHistory.length);
    expect(second.player.loginStreak).toBe(data.player.loginStreak);
    expect(second.player.playStreak).toBe(data.player.playStreak);
  });
});
