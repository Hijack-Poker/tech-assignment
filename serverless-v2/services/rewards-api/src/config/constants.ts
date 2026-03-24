export interface Tier {
  name: string;
  minPoints: number;
  multiplier: number;
}

export interface PointRule {
  points: number;
  description: string;
}

/**
 * Rewards tier definitions.
 * Points thresholds for tier progression.
 */
export const TIERS: Record<string, Tier> = {
  BRONZE: { name: 'Bronze', minPoints: 0, multiplier: 1.0 },
  SILVER: { name: 'Silver', minPoints: 500, multiplier: 1.25 },
  GOLD: { name: 'Gold', minPoints: 2000, multiplier: 1.5 },
  PLATINUM: { name: 'Platinum', minPoints: 10000, multiplier: 2.0 },
};

/**
 * Point award rules — how points are earned.
 */
export const POINT_RULES: Record<string, PointRule> = {
  HAND_PLAYED: { points: 1, description: 'Played a hand' },
  HAND_WON: { points: 5, description: 'Won a hand' },
  TOURNAMENT_ENTRY: { points: 10, description: 'Entered a tournament' },
  TOURNAMENT_WIN: { points: 50, description: 'Won a tournament' },
  DAILY_LOGIN: { points: 2, description: 'Daily login bonus' },
  REFERRAL: { points: 100, description: 'Referred a friend' },
};

/**
 * Get tier for a given point total.
 */
export function getTierForPoints(points: number): Tier {
  const tiers = Object.values(TIERS).sort((a, b) => b.minPoints - a.minPoints);
  return tiers.find((t) => points >= t.minPoints) || TIERS.BRONZE;
}

/**
 * Get the next tier above the current one (or null if at max).
 */
export function getNextTier(currentTierName: string): Tier | null {
  const tierOrder = ['Bronze', 'Silver', 'Gold', 'Platinum'];
  const currentIndex = tierOrder.indexOf(currentTierName);
  if (currentIndex === -1 || currentIndex === tierOrder.length - 1) return null;
  const nextName = tierOrder[currentIndex + 1];
  return Object.values(TIERS).find((t) => t.name === nextName) || null;
}
