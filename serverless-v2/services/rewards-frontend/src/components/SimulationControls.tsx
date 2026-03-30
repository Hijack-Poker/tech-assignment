import { Box, Switch, Typography } from '@mui/material';

interface SimulationControlsProps {
  enabled: boolean;
  onToggle: (enabled: boolean) => void;
}

function SimulationControls({ enabled, onToggle }: SimulationControlsProps) {
  return (
    <Box display="flex" alignItems="center" gap={1}>
      {enabled && (
        <Box
          sx={{
            width: 8,
            height: 8,
            borderRadius: '50%',
            bgcolor: '#f44336',
            animation: 'pulse 1.5s ease-in-out infinite',
            '@keyframes pulse': {
              '0%, 100%': { opacity: 1 },
              '50%': { opacity: 0.3 },
            },
          }}
        />
      )}
      {enabled && (
        <Typography variant="caption" sx={{ color: '#f44336', fontWeight: 700, mr: 0.5 }}>
          LIVE
        </Typography>
      )}
      <Typography variant="body2" color="text.secondary">
        Simulate Activity
      </Typography>
      <Switch
        size="small"
        checked={enabled}
        onChange={(_, checked) => onToggle(checked)}
      />
    </Box>
  );
}

export default SimulationControls;
