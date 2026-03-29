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

      {result && (
        <Alert severity="success" sx={{ mt: 1 }} onClose={() => setResult(null)}>
          Reset complete. {result.processed} players processed, {result.downgrades} tier downgrades.
        </Alert>
      )}
      {error && (
        <Alert severity="error" sx={{ mt: 1 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

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
    </>
  );
}

export default MonthlyResetButton;
