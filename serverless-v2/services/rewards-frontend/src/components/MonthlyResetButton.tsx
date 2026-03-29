import { useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Snackbar,
} from '@mui/material';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import type { MonthlyResetResponse } from '@shared/types/rewards';
import apiClient from '../api/client';

interface MonthlyResetButtonProps {
  onResetComplete: () => void;
}

function MonthlyResetButton({ onResetComplete }: MonthlyResetButtonProps) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<MonthlyResetResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await apiClient.post<MonthlyResetResponse>('/dev/monthly-reset');
      setResult(data);
      setDialogOpen(false);
      onResetComplete();
    } catch (err) {
      setError('Failed to run monthly reset');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Button
        variant="outlined"
        color="warning"
        startIcon={<RestartAltIcon />}
        onClick={() => { setDialogOpen(true); setResult(null); setError(null); }}
        size="small"
      >
        Monthly Reset
      </Button>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>Trigger Monthly Reset</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This will reset all players&apos; monthly points to 0 and may downgrade tiers
            (with tier floor protection — max 1 tier drop). Continue?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)} disabled={loading}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} color="warning" variant="contained" disabled={loading}>
            {loading ? <CircularProgress size={20} /> : 'Reset'}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={!!result} autoHideDuration={5000} onClose={() => setResult(null)} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
        <Alert severity="success" onClose={() => setResult(null)} variant="filled">
          Reset complete. {result?.processed} players processed, {result?.downgrades} tier downgrades.
        </Alert>
      </Snackbar>
      <Snackbar open={!!error} autoHideDuration={5000} onClose={() => setError(null)} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
        <Alert severity="error" onClose={() => setError(null)} variant="filled">
          {error}
        </Alert>
      </Snackbar>
    </>
  );
}

export default MonthlyResetButton;
