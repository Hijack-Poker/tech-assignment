import { Box, Button, ButtonGroup } from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import StopIcon from '@mui/icons-material/Stop';
import SkipNextIcon from '@mui/icons-material/SkipNext';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import SpeedIcon from '@mui/icons-material/Speed';

interface GameControlsProps {
  onNextStep: () => void;
  onToggleAuto: () => void;
  onCycleSpeed: () => void;
  onReset: () => void;
  isPlaying: boolean;
  speedLabel: string;
  disabled?: boolean;
}

const btnSx = {
  fontWeight: 700,
  textTransform: 'none' as const,
  borderRadius: 2,
  fontSize: { xs: 11, sm: 13 },
  px: { xs: 1, sm: 2 },
  py: { xs: 0.5, sm: 0.75 },
};

function GameControls({ onNextStep, onToggleAuto, onCycleSpeed, onReset, isPlaying, speedLabel, disabled }: GameControlsProps) {
  return (
    <Box display="flex" alignItems="center" gap={1} flexWrap="wrap" justifyContent="center">
      <Button
        variant="contained"
        startIcon={<SkipNextIcon sx={{ fontSize: { xs: 16, sm: 20 } }} />}
        onClick={onNextStep}
        disabled={disabled || isPlaying}
        size="small"
        data-testid="btn-next-step"
        sx={{ ...btnSx, bgcolor: '#2E7D32', '&:hover': { bgcolor: '#1B5E20' } }}
      >
        Next Step
      </Button>
      <Button
        variant="contained"
        startIcon={isPlaying ? <StopIcon sx={{ fontSize: { xs: 16, sm: 20 } }} /> : <PlayArrowIcon sx={{ fontSize: { xs: 16, sm: 20 } }} />}
        onClick={onToggleAuto}
        disabled={disabled}
        size="small"
        data-testid="btn-auto-play"
        sx={{ ...btnSx, bgcolor: isPlaying ? '#C62828' : '#1565C0', '&:hover': { bgcolor: isPlaying ? '#B71C1C' : '#0D47A1' } }}
      >
        {isPlaying ? 'Stop' : 'Auto'}
      </Button>
      <ButtonGroup variant="outlined" size="small">
        <Button
          startIcon={<SpeedIcon sx={{ fontSize: { xs: 14, sm: 18 } }} />}
          onClick={onCycleSpeed}
          data-testid="btn-speed"
          sx={{ ...btnSx, color: '#90CAF9', borderColor: '#37474F' }}
        >
          {speedLabel}
        </Button>
      </ButtonGroup>
      <Button
        variant="outlined"
        startIcon={<RestartAltIcon sx={{ fontSize: { xs: 16, sm: 20 } }} />}
        onClick={onReset}
        disabled={disabled}
        size="small"
        data-testid="btn-reset"
        sx={{ ...btnSx, color: '#B0BEC5', borderColor: '#37474F', '&:hover': { borderColor: '#546E7A', bgcolor: '#263238' } }}
      >
        Reset
      </Button>
    </Box>
  );
}

export default GameControls;
