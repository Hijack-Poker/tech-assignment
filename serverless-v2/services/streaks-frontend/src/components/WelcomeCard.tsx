import { Typography, Box, Card, CardContent, Chip } from '@mui/material';

function WelcomeCard() {
  return (
    <Card sx={{ bgcolor: 'linear-gradient(135deg, #1A1D27, #1E2433)', border: '1px solid #2A2D3A', borderRadius: 4, mb: 3, overflow: 'hidden' }}>
      <CardContent sx={{ p: 4, background: 'linear-gradient(135deg, rgba(255,107,53,0.05), rgba(96,165,250,0.05))' }}>
        <Box display="flex" flexDirection={{ xs: 'column', md: 'row' }} alignItems="center" gap={3}>
          <Box flex={1}>
            <Typography variant="h5" fontWeight={800} color="#fff" gutterBottom>
              Welcome to Daily Streaks!
            </Typography>
            <Typography sx={{ color: '#8B8FA3', fontSize: 14, mb: 2, lineHeight: 1.6 }}>
              Build your streak by checking in daily and playing poker hands.
              Earn rewards at milestones, climb the leaderboard, and complete daily missions for bonus points.
            </Typography>
            <Box display="flex" gap={1} flexWrap="wrap">
              <Chip label="1. Check in daily" sx={{ bgcolor: '#FF6B35', color: '#fff', fontWeight: 600, fontSize: 12 }} />
              <Chip label="2. Play poker hands" sx={{ bgcolor: '#1B5E20', color: '#fff', fontWeight: 600, fontSize: 12 }} />
              <Chip label="3. Earn rewards" sx={{ bgcolor: '#0D47A1', color: '#fff', fontWeight: 600, fontSize: 12 }} />
            </Box>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

export default WelcomeCard;
