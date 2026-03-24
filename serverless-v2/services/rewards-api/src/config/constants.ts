import { TierDefinition, TierNumber, TierName } from '../../../shared/types/rewards';

export type { TierDefinition, TierNumber, TierName };

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
