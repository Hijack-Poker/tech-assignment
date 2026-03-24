import { Card, CardContent, Box, Typography, Chip } from '@mui/material';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';
import type { NextMilestone } from '../types/streaks.types';

export interface MilestoneProgressProps {
  type: 'login' | 'play';
  currentStreak: number;
  nextMilestone: NextMilestone | null;
}

const MILESTONE_DAYS = [3, 7, 14, 30, 60, 90];

function getPreviousMilestoneDays(nextMilestoneDays: number): number {
  const index = MILESTONE_DAYS.indexOf(nextMilestoneDays);
  if (index <= 0) return 0;
  return MILESTONE_DAYS[index - 1];
}

function MilestoneProgress({ type, currentStreak, nextMilestone }: MilestoneProgressProps) {
  const isLogin = type === 'login';
  const label = isLogin ? 'Next Login Milestone' : 'Next Play Milestone';
  const gradient = isLogin
    ? 'linear-gradient(90deg, #FF6B35, #FF4444)'
    : 'linear-gradient(90deg, #7C3AED, #6D28D9)';
  const badgeBg = isLogin ? 'rgba(255,107,53,0.08)' : 'rgba(124,58,237,0.08)';
  const badgeColor = isLogin ? '#FF6B35' : '#A78BFA';

  if (!nextMilestone) {
    return (
      <Card data-testid={`milestone-progress-${type}`}>
        <CardContent>
          <Box display="flex" alignItems="center" gap={1} mb={1}>
            <EmojiEventsIcon sx={{ color: '#FBBF24', fontSize: 20 }} data-testid="trophy-icon" />
            <Typography variant="body2" sx={{ color: '#C4C7D4', fontWeight: 600, fontSize: 14 }}>
              {label}
            </Typography>
          </Box>
          <Typography variant="body1" fontWeight={600} data-testid={`milestone-max-${type}`}>
            All milestones reached!
          </Typography>
        </CardContent>
      </Card>
    );
  }

  const previousDays = getPreviousMilestoneDays(nextMilestone.days);
  const range = nextMilestone.days - previousDays;
  const progress = range > 0 ? Math.min(((currentStreak - previousDays) / range) * 100, 100) : 0;

  return (
    <Card data-testid={`milestone-progress-${type}`}>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <EmojiEventsIcon sx={{ color: '#FBBF24', fontSize: 20 }} data-testid="trophy-icon" />
            <Typography sx={{ color: '#C4C7D4', fontWeight: 600, fontSize: 14 }}>
              {label}
            </Typography>
          </Box>
          <Chip
            label={`${nextMilestone.daysRemaining} days left`}
            size="small"
            sx={{
              bgcolor: badgeBg,
              color: badgeColor,
              fontWeight: 600,
              fontSize: 12,
              height: 26,
            }}
          />
        </Box>

        {/* Progress bar */}
        <Box sx={{ width: '100%', height: 8, borderRadius: 4, bgcolor: '#2A2D3A', mb: 2 }}>
          <Box
            data-testid={`milestone-progress-bar-${type}`}
            sx={{
              width: `${progress}%`,
              height: '100%',
              borderRadius: 4,
              background: gradient,
              transition: 'width 0.6s ease',
            }}
          />
        </Box>

        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography sx={{ color: '#8B8FA3', fontSize: 13, fontWeight: 500 }} data-testid={`milestone-message-${type}`}>
            {nextMilestone.days}-day milestone
          </Typography>
          <Typography sx={{ color: '#FBBF24', fontSize: 13, fontWeight: 600 }}>
            {nextMilestone.reward} bonus points
          </Typography>
        </Box>
      </CardContent>
    </Card>
  );
}

export default MilestoneProgress;
export { getPreviousMilestoneDays, MILESTONE_DAYS };
