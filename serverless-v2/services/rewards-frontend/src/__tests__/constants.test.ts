import { describe, it, expect } from 'vitest';
import {
  TIER_COLORS,
  TIER_THRESHOLDS,
  TIER_ORDER,
  STAKES_OPTIONS,
  generateId,
} from '../constants';

describe('constants', () => {
  it('defines colors for every tier', () => {
    for (const tier of TIER_ORDER) {
      expect(TIER_COLORS[tier]).toBeDefined();
      expect(TIER_COLORS[tier]).toMatch(/^#[0-9A-Fa-f]{6}$/);
    }
  });

  it('has ascending tier thresholds', () => {
    for (let i = 1; i < TIER_ORDER.length; i++) {
      expect(TIER_THRESHOLDS[TIER_ORDER[i]]).toBeGreaterThan(
        TIER_THRESHOLDS[TIER_ORDER[i - 1]],
      );
    }
  });

  it('provides at least one stakes option with required fields', () => {
    expect(STAKES_OPTIONS.length).toBeGreaterThan(0);
    for (const opt of STAKES_OPTIONS) {
      expect(opt).toHaveProperty('label');
      expect(opt).toHaveProperty('tableStakes');
      expect(opt).toHaveProperty('bigBlind');
      expect(opt).toHaveProperty('tableId');
      expect(opt.bigBlind).toBeGreaterThan(0);
    }
  });

  it('has ascending big blind values across stakes options', () => {
    for (let i = 1; i < STAKES_OPTIONS.length; i++) {
      expect(STAKES_OPTIONS[i].bigBlind).toBeGreaterThan(
        STAKES_OPTIONS[i - 1].bigBlind,
      );
    }
  });
});

describe('generateId', () => {
  it('returns a string in UUID-like format', () => {
    const id = generateId();
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/,
    );
  });

  it('generates unique IDs', () => {
    const ids = new Set(Array.from({ length: 50 }, () => generateId()));
    expect(ids.size).toBe(50);
  });
});
