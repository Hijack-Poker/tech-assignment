import { Card, CardContent, Box, Typography, Chip } from '@mui/material';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';

export interface PersonalBestProps {
  bestLoginStreak: number;
  bestPlayStreak: number;
  currentLoginStreak: number;
  currentPlayStreak: number;
}

function PersonalBest({
  bestLoginStreak,
  bestPlayStreak,
  currentLoginStreak,
  currentPlayStreak,
}: PersonalBestProps) {
  const isLoginBest = currentLoginStreak > 0 && currentLoginStreak >= bestLoginStreak;
  const isPlayBest = currentPlayStreak > 0 && currentPlayStreak >= bestPlayStreak;

  return (
    <Card data-testid="personal-best">
      <CardContent>
        <Box display="flex" alignItems="center" gap={2} mb={2}>
          <EmojiEventsIcon sx={{ fontSize: 32, color: '#FFD700' }} data-testid="trophy-icon" />
          <Typography variant="h6" fontWeight={700}>
            Personal Best
          </Typography>
        </Box>

        <Box display="flex" flexDirection="column" gap={2}>
          <Box display="flex" alignItems="center" gap={1} data-testid="best-login">
            <Typography variant="body1">
              Best login streak: <strong>{bestLoginStreak} days</strong>
            </Typography>
            {isLoginBest && (
              <Chip
                label="Current best!"
                color="success"
                size="small"
                data-testid="login-best-indicator"
              />
            )}
          </Box>

          <Box display="flex" alignItems="center" gap={1} data-testid="best-play">
            <Typography variant="body1">
              Best play streak: <strong>{bestPlayStreak} days</strong>
            </Typography>
            {isPlayBest && (
              <Chip
                label="Current best!"
                color="success"
                size="small"
                data-testid="play-best-indicator"
              />
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

export default PersonalBest;
