import { Card, CardContent, Box, Typography, Alert, Skeleton } from '@mui/material';
import AcUnitIcon from '@mui/icons-material/AcUnit';
import { useFreezes } from '../hooks/useFreezes';

function isFreezeActiveToday(history: { date: string }[]): boolean {
  const today = new Date().toISOString().slice(0, 10);
  return history.some((entry) => entry.date === today);
}

function formatSource(source: string): string {
  return source
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

function FreezeStatus() {
  const { data, loading, error } = useFreezes();

  if (error) {
    return (
      <Alert severity="error" data-testid="freeze-error">
        {error}
      </Alert>
    );
  }

  if (loading || !data) {
    return (
      <Card data-testid="freeze-loading">
        <CardContent>
          <Skeleton variant="rectangular" height={80} />
        </CardContent>
      </Card>
    );
  }

  const freezeActive = isFreezeActiveToday(data.history ?? []);

  return (
    <Card data-testid="freeze-status">
      <CardContent>
        <Box display="flex" alignItems="center" gap={2.5}>
          <Box
            sx={{
              width: 64,
              height: 64,
              borderRadius: 4,
              background: 'linear-gradient(180deg, rgba(59,130,246,0.12), rgba(37,99,235,0.06))',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <AcUnitIcon data-testid="freeze-icon" sx={{ fontSize: 32, color: '#60A5FA' }} />
          </Box>
          <Box>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: 13 }}>
              Streak Freezes
            </Typography>
            <Typography variant="h4" fontWeight={700} sx={{ lineHeight: 1.2 }} data-testid="freeze-count">
              {data.freezesAvailable}{' '}
              <Typography component="span" sx={{ fontSize: 20, fontWeight: 700, color: 'text.secondary' }}>
                available
              </Typography>
            </Typography>
            <Typography variant="caption" sx={{ color: '#60A5FA', fontWeight: 500, fontSize: 12 }}>
              {freezeActive ? 'Freeze active today' : '1 free monthly reset'}
            </Typography>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

export default FreezeStatus;
export { isFreezeActiveToday, formatSource };
