import type { TierName } from '@shared/types/rewards';

/** crypto.randomUUID() requires a secure context (HTTPS). Fall back to Math.random for HTTP. */
export function generateId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

export const TIER_COLORS: Record<TierName, string> = {
  Bronze: '#CD7F32',
  Silver: '#C0C0C0',
  Gold: '#FFD700',
  Platinum: '#E5E4E2',
};

export const TIER_THRESHOLDS: Record<TierName, number> = {
  Bronze: 0,
  Silver: 500,
  Gold: 2000,
  Platinum: 10000,
};

export const TIER_ORDER: TierName[] = ['Bronze', 'Silver', 'Gold', 'Platinum'];

export const STAKES_OPTIONS = [
  { label: 'Micro ($0.01/$0.02)', tableStakes: '$0.01/$0.02', bigBlind: 0.02, tableId: 1 },
  { label: 'Low ($0.05/$0.10)', tableStakes: '$0.05/$0.10', bigBlind: 0.10, tableId: 2 },
  { label: 'Medium ($0.25/$0.50)', tableStakes: '$0.25/$0.50', bigBlind: 0.50, tableId: 3 },
  { label: 'High ($1/$2)', tableStakes: '$1/$2', bigBlind: 2.00, tableId: 4 },
  { label: 'Premium ($5/$10)', tableStakes: '$5/$10', bigBlind: 10.00, tableId: 5 },
];
