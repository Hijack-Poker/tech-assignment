import { useState } from 'react';
import { Box, Drawer, Fab, Typography, Tooltip } from '@mui/material';
import BuildIcon from '@mui/icons-material/Build';
import MonthlyResetButton from './MonthlyResetButton';

interface DevPanelProps {
  onResetComplete: () => void;
}

function DevPanel({ onResetComplete }: DevPanelProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Tooltip title="Dev Tools">
        <Fab
          size="small"
          color="warning"
          onClick={() => setOpen(true)}
          sx={{ position: 'fixed', bottom: 24, right: 24, zIndex: 1200 }}
          aria-label="dev tools"
        >
          <BuildIcon />
        </Fab>
      </Tooltip>

      <Drawer
        anchor="right"
        open={open}
        onClose={() => setOpen(false)}
        slotProps={{ paper: { sx: { width: 380, p: 3, bgcolor: 'background.default' } } }}
      >
        <Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>
          Dev Tools
        </Typography>

        <Box>
          <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
            Monthly Reset
          </Typography>
          <MonthlyResetButton onResetComplete={onResetComplete} />
        </Box>
      </Drawer>
    </>
  );
}

export default DevPanel;
