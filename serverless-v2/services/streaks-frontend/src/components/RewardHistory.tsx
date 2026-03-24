import { Card, CardContent, Box, Typography, Alert, Skeleton } from '@mui/material';
import CardGiftcardIcon from '@mui/icons-material/CardGiftcard';
import { useRewards } from '../hooks/useRewards';
import type { Reward } from '../types/streaks.types';

function formatRewardType(type: string): string {
  if (type.startsWith('login')) return 'Login';
  if (type.startsWith('play')) return 'Play';
  return type
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

function formatMilestone(milestone: number, type: string): string {
  const streakType = formatRewardType(type).toLowerCase();
  return `${milestone}-day ${streakType} milestone`;
}

function sortByDateDesc(rewards: Reward[]): Reward[] {
  return [...rewards].sort((a, b) => b.date.localeCompare(a.date));
}

function formatDisplayDate(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function RewardHistory() {
  const { data, loading, error } = useRewards();

  if (error) {
    return (
      <Alert severity="error" data-testid="reward-error">
        {error}
      </Alert>
    );
  }

  if (loading || !data) {
    return (
      <Card data-testid="reward-loading">
        <CardContent>
          <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 2 }} />
        </CardContent>
      </Card>
    );
  }

  const sorted = sortByDateDesc(data.rewards);

  return (
    <Card data-testid="reward-history" sx={{ height: '100%' }}>
      <CardContent>
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          <CardGiftcardIcon sx={{ color: '#F472B6', fontSize: 20 }} data-testid="reward-icon" />
          <Typography fontWeight={600} fontSize={16}>
            Recent Rewards
          </Typography>
        </Box>

        {sorted.length > 0 ? (
          <Box display="flex" flexDirection="column" gap={1.5} data-testid="reward-table">
            {sorted.slice(0, 5).map((reward, index) => (
              <Box
                key={`${reward.date}-${reward.milestone}-${index}`}
                sx={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  bgcolor: '#1F2231',
                  borderRadius: 2.5,
                  px: 2,
                  py: 1.5,
                }}
              >
                <Box>
                  <Typography sx={{ color: '#C4C7D4', fontSize: 13, fontWeight: 600 }}>
                    {formatMilestone(reward.milestone, reward.type)}
                  </Typography>
                  <Typography sx={{ color: '#8B8FA3', fontSize: 11 }}>
                    {formatDisplayDate(reward.date)}
                  </Typography>
                </Box>
                <Typography sx={{ color: '#4ADE80', fontSize: 16, fontWeight: 700 }}>
                  +{reward.points}
                </Typography>
              </Box>
            ))}
          </Box>
        ) : (
          <Typography
            variant="body2"
            sx={{ color: '#8B8FA3' }}
            data-testid="reward-history-empty"
          >
            No rewards earned yet
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

export default RewardHistory;
export { formatRewardType, formatMilestone, sortByDateDesc };
