import { Container, Typography, Paper, Box, Alert } from '@mui/material';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';

function Dashboard() {
  return (
    <Container maxWidth="lg" sx={{ mt: 4 }}>
      <Box display="flex" alignItems="center" gap={2} mb={4}>
        <EmojiEventsIcon sx={{ fontSize: 40, color: 'primary.main' }} />
        <Typography variant="h4" fontWeight={700}>
          Rewards Dashboard
        </Typography>
      </Box>

      <Alert severity="info" sx={{ mb: 3 }}>
        This is the placeholder dashboard. Build your rewards UI here!
      </Alert>

      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h6" color="text.secondary" gutterBottom>
          Your Challenge
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Build a rewards dashboard that displays player tier, points balance,
          transaction history, and a leaderboard.
        </Typography>
        <Typography variant="body2" color="text.secondary">
          The API is running at <code>http://localhost:5000</code> — check the
          challenge docs for the full API contract.
        </Typography>
      </Paper>
    </Container>
  );
}

export default Dashboard;
