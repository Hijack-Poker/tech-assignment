import { Container, Typography, Paper, Box, Alert } from '@mui/material';
import LocalFireDepartmentIcon from '@mui/icons-material/LocalFireDepartment';

function Dashboard() {
  return (
    <Container maxWidth="lg" sx={{ mt: 4 }}>
      <Box display="flex" alignItems="center" gap={2} mb={4}>
        <LocalFireDepartmentIcon sx={{ fontSize: 40, color: 'primary.main' }} />
        <Typography variant="h4" fontWeight={700}>
          Daily Streaks
        </Typography>
      </Box>

      <Alert severity="info" sx={{ mb: 3 }}>
        This is the placeholder dashboard. Build your streaks UI here!
      </Alert>

      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h6" color="text.secondary" gutterBottom>
          Your Challenge
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Build a daily streaks dashboard with a check-in button, streak counter,
          calendar view, milestone badges, and a leaderboard.
        </Typography>
        <Typography variant="body2" color="text.secondary">
          The API is running at <code>http://localhost:5001</code> — check the
          challenge docs for the full API contract.
        </Typography>
      </Paper>
    </Container>
  );
}

export default Dashboard;
