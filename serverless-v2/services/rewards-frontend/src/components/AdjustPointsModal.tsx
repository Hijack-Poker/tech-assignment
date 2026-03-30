import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material';
import type { PlayerRewardsResponse } from '@shared/types/rewards';
import { TIER_COLORS, TIER_ORDER, TIER_THRESHOLDS } from '../constants';
import apiClient from '../api/client';

interface AdjustPointsModalProps {
  open: boolean;
  onClose: () => void;
  playerId: string;
  onSaved: () => void;
}

function getTierPreview(totalEarned: number) {
  for (let i = TIER_ORDER.length - 1; i >= 0; i--) {
    if (totalEarned >= TIER_THRESHOLDS[TIER_ORDER[i]]) return TIER_ORDER[i];
  }
  return TIER_ORDER[0];
}

function AdjustPointsModal({ open, onClose, playerId, onSaved }: AdjustPointsModalProps) {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [playerData, setPlayerData] = useState<PlayerRewardsResponse | null>(null);
  const [points, setPoints] = useState('');
  const [totalEarned, setTotalEarned] = useState('');
  const [reason, setReason] = useState('dev_adjustment');

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    setError(null);
    setPlayerData(null);
    apiClient
      .get<PlayerRewardsResponse>(`/dev/player/${playerId}`)
      .then(({ data }) => {
        setPlayerData(data);
        setPoints(String(data.points));
        setTotalEarned(String(data.totalEarned));
      })
      .catch((err) => {
        setError(err.response?.data?.message || 'Failed to load player data');
      })
      .finally(() => setLoading(false));
  }, [open, playerId]);

  const previewTier = getTierPreview(Number(totalEarned) || 0);
  const tierChanged = playerData && previewTier !== playerData.tier;

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      await apiClient.put(`/dev/player/${playerId}/points`, {
        points: Number(points),
        totalEarned: Number(totalEarned),
        reason: reason || 'dev_adjustment',
      });
      onSaved();
      onClose();
    } catch (err) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setError(axiosErr.response?.data?.message || 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ pb: 1 }}>Adjust Points — {playerId}</DialogTitle>
      <DialogContent>
        {loading && (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        )}

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {playerData && !loading && (
          <Box display="flex" flexDirection="column" gap={2} pt={1}>
            {/* Current state */}
            <Box display="flex" alignItems="center" gap={1}>
              <Typography variant="body2" color="text.secondary">
                Current tier:
              </Typography>
              <Chip
                label={playerData.tier}
                size="small"
                sx={{
                  bgcolor: TIER_COLORS[playerData.tier],
                  color: playerData.tier === 'Gold' || playerData.tier === 'Platinum' ? '#000' : '#fff',
                  fontWeight: 700,
                }}
              />
            </Box>

            <Box display="flex" gap={2}>
              <Typography variant="body2" color="text.secondary">
                Points: {playerData.points.toLocaleString()}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total: {playerData.totalEarned.toLocaleString()}
              </Typography>
            </Box>

            {/* Inputs */}
            <TextField
              label="Points"
              type="number"
              fullWidth
              size="small"
              value={points}
              onChange={(e) => setPoints(e.target.value)}
            />
            <TextField
              label="Total Earned"
              type="number"
              fullWidth
              size="small"
              value={totalEarned}
              onChange={(e) => setTotalEarned(e.target.value)}
            />
            <TextField
              label="Reason"
              fullWidth
              size="small"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />

            {/* Tier preview */}
            <Box display="flex" alignItems="center" gap={1}>
              <Typography variant="body2" color="text.secondary">
                Tier preview:
              </Typography>
              <Chip
                label={previewTier}
                size="small"
                sx={{
                  bgcolor: TIER_COLORS[previewTier],
                  color: previewTier === 'Gold' || previewTier === 'Platinum' ? '#000' : '#fff',
                  fontWeight: 700,
                }}
              />
              {tierChanged && (
                <Typography variant="caption" color="warning.main" fontWeight={600}>
                  Tier will change!
                </Typography>
              )}
            </Box>
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={saving}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={loading || saving || !playerData}
        >
          {saving ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default AdjustPointsModal;
