import { TierDefinition, TierNumber, TierName, NotificationType } from '../../../shared/types/rewards';

export type { TierDefinition, TierNumber, TierName };

// ─── Milestone Definitions ──────────────────────────────────────────

export interface MilestoneDefinition {
  id: string;
  field: 'handsPlayed' | 'totalEarned';
  threshold: number;
  title: string;
  description: string;
  type: NotificationType;
}

export const MILESTONES: MilestoneDefinition[] = [
  { id: 'first_hand', field: 'handsPlayed', threshold: 1, title: 'First Hand!', description: 'You played your very first hand. Welcome to the tables!', type: 'milestone' },
  { id: 'hands_100', field: 'handsPlayed', threshold: 100, title: 'Century Club', description: "You've played 100 hands. You're getting into the groove!", type: 'milestone' },
  { id: 'hands_500', field: 'handsPlayed', threshold: 500, title: 'Card Shark', description: "500 hands played! You're a true card shark.", type: 'milestone' },
  { id: 'points_1000', field: 'totalEarned', threshold: 1000, title: 'Point Collector', description: "You've earned 1,000 total points. Keep stacking!", type: 'milestone' },
];

/**
 * Returns milestones that were crossed between old and new values.
 */
export function checkMilestones(
  oldHands: number,
  newHands: number,
  oldTotal: number,
  newTotal: number,
): MilestoneDefinition[] {
  return MILESTONES.filter((m) => {
    const oldVal = m.field === 'handsPlayed' ? oldHands : oldTotal;
    const newVal = m.field === 'handsPlayed' ? newHands : newTotal;
    return oldVal < m.threshold && newVal >= m.threshold;
  });
}

/**
 * Rewards tier definitions.
 * Points thresholds for tier progression.
 */
export const TIERS: Record<string, TierDefinition> = {
  BRONZE: { number: 1, name: 'Bronze', minPoints: 0, multiplier: 1.0 },
  SILVER: { number: 2, name: 'Silver', minPoints: 500, multiplier: 1.25 },
  GOLD: { number: 3, name: 'Gold', minPoints: 2000, multiplier: 1.5 },
  PLATINUM: { number: 4, name: 'Platinum', minPoints: 10000, multiplier: 2.0 },
};

/**
 * Base points per hand by table stakes (big blind).
 */
export const STAKES_POINTS: { maxBB: number; basePoints: number }[] = [
  { maxBB: 0.25, basePoints: 1 },
  { maxBB: 1.0, basePoints: 2 },
  { maxBB: 5.0, basePoints: 5 },
  { maxBB: Infinity, basePoints: 10 },
];

/**
 * Get base points for a given big blind amount.
 */
export function getBasePointsForStakes(bigBlind: number): number {
  const bracket = STAKES_POINTS.find((s) => bigBlind <= s.maxBB);
  return bracket ? bracket.basePoints : 10;
}

/**
 * Get tier definition for a given monthly point total.
 */
export function getTierForPoints(points: number): TierDefinition {
  const tiers = Object.values(TIERS).sort((a, b) => b.minPoints - a.minPoints);
  return tiers.find((t) => points >= t.minPoints) || TIERS.BRONZE;
}

/**
 * Get the next tier above the current one (or null if at max).
 */
export function getNextTier(currentTierName: TierName): TierDefinition | null {
  const tierOrder: TierName[] = ['Bronze', 'Silver', 'Gold', 'Platinum'];
  const currentIndex = tierOrder.indexOf(currentTierName);
  if (currentIndex === -1 || currentIndex === tierOrder.length - 1) return null;
  const nextName = tierOrder[currentIndex + 1];
  return Object.values(TIERS).find((t) => t.name === nextName) || null;
}

/**
 * Convert tier number (DynamoDB) to tier name (API).
 */
export function tierNumberToName(tierNumber: TierNumber): TierName {
  const tier = Object.values(TIERS).find((t) => t.number === tierNumber);
  return tier ? tier.name : 'Bronze';
}

/**
 * Convert tier name (API) to tier number (DynamoDB).
 */
export function tierNameToNumber(tierName: TierName): TierNumber {
  const tier = Object.values(TIERS).find((t) => t.name === tierName);
  return tier ? tier.number : 1;
}
