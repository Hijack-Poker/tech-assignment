import { Chip } from '@mui/material';
import type { VipTier } from '../types/streaks.types';

const TIER_CONFIG: Record<VipTier, { label: string; color: string; bg: string }> = {
  bronze: { label: 'Bronze', color: '#CD7F32', bg: 'rgba(205,127,50,0.15)' },
  silver: { label: 'Silver', color: '#C0C0C0', bg: 'rgba(192,192,192,0.15)' },
  gold: { label: 'Gold', color: '#FFD700', bg: 'rgba(255,215,0,0.15)' },
  platinum: { label: 'Platinum', color: '#E5E4E2', bg: 'rgba(229,228,226,0.2)' },
};

export function getVipTier(loginStreak: number, playStreak: number): VipTier {
  const score = loginStreak + playStreak;
  if (score >= 90) return 'platinum';
  if (score >= 30) return 'gold';
  if (score >= 7) return 'silver';
  return 'bronze';
}

function VipBadge({ tier }: { tier: VipTier }) {
  const config = TIER_CONFIG[tier];
  return (
    <Chip
      label={`${config.label} Tier`}
      size="small"
      sx={{
        fontWeight: 700,
        fontSize: 11,
        color: config.color,
        bgcolor: config.bg,
        border: `1px solid ${config.color}`,
        borderRadius: 2,
      }}
    />
  );
}

export default VipBadge;
