import {
  TIERS,
  STAKES_POINTS,
  MILESTONES,
  getBasePointsForStakes,
  getTierForPoints,
  getNextTier,
  tierNumberToName,
  tierNameToNumber,
  checkMilestones,
} from '../src/config/constants';

describe('TIERS constant', () => {
  it('defines four tiers', () => {
    expect(Object.keys(TIERS)).toHaveLength(4);
  });

  it('has increasing minPoints thresholds', () => {
    const minPoints = Object.values(TIERS).map((t) => t.minPoints);
    for (let i = 1; i < minPoints.length; i++) {
      expect(minPoints[i]).toBeGreaterThan(minPoints[i - 1]);
    }
  });

  it('has increasing multipliers', () => {
    const multipliers = Object.values(TIERS).map((t) => t.multiplier);
    for (let i = 1; i < multipliers.length; i++) {
      expect(multipliers[i]).toBeGreaterThan(multipliers[i - 1]);
    }
  });
});

describe('getBasePointsForStakes', () => {
  it('returns 1 for micro stakes (bb <= 0.25)', () => {
    expect(getBasePointsForStakes(0.1)).toBe(1);
    expect(getBasePointsForStakes(0.25)).toBe(1);
  });

  it('returns 2 for low stakes (0.25 < bb <= 1.0)', () => {
    expect(getBasePointsForStakes(0.5)).toBe(2);
    expect(getBasePointsForStakes(1.0)).toBe(2);
  });

  it('returns 5 for mid stakes (1.0 < bb <= 5.0)', () => {
    expect(getBasePointsForStakes(2.0)).toBe(5);
    expect(getBasePointsForStakes(5.0)).toBe(5);
  });

  it('returns 10 for high stakes (bb > 5.0)', () => {
    expect(getBasePointsForStakes(10)).toBe(10);
    expect(getBasePointsForStakes(100)).toBe(10);
  });
});

describe('getTierForPoints', () => {
  it('returns Bronze for 0 points', () => {
    expect(getTierForPoints(0)).toEqual(TIERS.BRONZE);
  });

  it('returns Bronze below Silver threshold', () => {
    expect(getTierForPoints(499)).toEqual(TIERS.BRONZE);
  });

  it('returns Silver at exactly 500 points', () => {
    expect(getTierForPoints(500)).toEqual(TIERS.SILVER);
  });

  it('returns Gold at exactly 2000 points', () => {
    expect(getTierForPoints(2000)).toEqual(TIERS.GOLD);
  });

  it('returns Platinum at exactly 10000 points', () => {
    expect(getTierForPoints(10000)).toEqual(TIERS.PLATINUM);
  });

  it('returns Platinum for very high points', () => {
    expect(getTierForPoints(999999)).toEqual(TIERS.PLATINUM);
  });
});

describe('getNextTier', () => {
  it('returns Silver as next tier after Bronze', () => {
    expect(getNextTier('Bronze')).toEqual(TIERS.SILVER);
  });

  it('returns Gold as next tier after Silver', () => {
    expect(getNextTier('Silver')).toEqual(TIERS.GOLD);
  });

  it('returns Platinum as next tier after Gold', () => {
    expect(getNextTier('Gold')).toEqual(TIERS.PLATINUM);
  });

  it('returns null for Platinum (max tier)', () => {
    expect(getNextTier('Platinum')).toBeNull();
  });

  it('returns null for invalid tier name', () => {
    expect(getNextTier('Invalid' as any)).toBeNull();
  });
});

describe('tierNumberToName', () => {
  it('converts 1 to Bronze', () => {
    expect(tierNumberToName(1)).toBe('Bronze');
  });

  it('converts 2 to Silver', () => {
    expect(tierNumberToName(2)).toBe('Silver');
  });

  it('converts 3 to Gold', () => {
    expect(tierNumberToName(3)).toBe('Gold');
  });

  it('converts 4 to Platinum', () => {
    expect(tierNumberToName(4)).toBe('Platinum');
  });

  it('defaults to Bronze for invalid number', () => {
    expect(tierNumberToName(99 as any)).toBe('Bronze');
  });
});

describe('tierNameToNumber', () => {
  it('converts Bronze to 1', () => {
    expect(tierNameToNumber('Bronze')).toBe(1);
  });

  it('converts Silver to 2', () => {
    expect(tierNameToNumber('Silver')).toBe(2);
  });

  it('converts Gold to 3', () => {
    expect(tierNameToNumber('Gold')).toBe(3);
  });

  it('converts Platinum to 4', () => {
    expect(tierNameToNumber('Platinum')).toBe(4);
  });

  it('defaults to 1 for invalid name', () => {
    expect(tierNameToNumber('Invalid' as any)).toBe(1);
  });
});

describe('MILESTONES constant', () => {
  it('defines four milestones', () => {
    expect(MILESTONES).toHaveLength(4);
  });

  it('all milestones have required fields', () => {
    for (const m of MILESTONES) {
      expect(m).toHaveProperty('id');
      expect(m).toHaveProperty('field');
      expect(m).toHaveProperty('threshold');
      expect(m).toHaveProperty('title');
      expect(m).toHaveProperty('type', 'milestone');
    }
  });
});

describe('checkMilestones', () => {
  it('returns first_hand when handsPlayed crosses 1', () => {
    const result = checkMilestones(0, 1, 0, 5);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('first_hand');
  });

  it('returns hands_100 when handsPlayed crosses 100', () => {
    const result = checkMilestones(99, 100, 500, 505);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('hands_100');
  });

  it('returns hands_500 when handsPlayed crosses 500', () => {
    const result = checkMilestones(499, 500, 2000, 2005);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('hands_500');
  });

  it('returns points_1000 when totalEarned crosses 1000', () => {
    const result = checkMilestones(50, 51, 995, 1005);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('points_1000');
  });

  it('returns multiple milestones when multiple thresholds are crossed', () => {
    // Crossing both first_hand (handsPlayed 0→1) and points_1000 (totalEarned 999→1001)
    const result = checkMilestones(0, 1, 999, 1001);
    expect(result).toHaveLength(2);
    const ids = result.map((m) => m.id);
    expect(ids).toContain('first_hand');
    expect(ids).toContain('points_1000');
  });

  it('returns empty array when no thresholds are crossed', () => {
    const result = checkMilestones(5, 6, 100, 105);
    expect(result).toEqual([]);
  });

  it('does not trigger when value was already at or above threshold', () => {
    const result = checkMilestones(1, 2, 1000, 1005);
    expect(result).toEqual([]);
  });
});
