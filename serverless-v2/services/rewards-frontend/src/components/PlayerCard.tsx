import { useState } from 'react';
import {
  Box,
  Button,
  Chip,
  IconButton,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Tooltip,
  Typography,
} from '@mui/material';
import TuneIcon from '@mui/icons-material/Tune';
import type { PlayerRewardsResponse, AwardPointsResponse } from '@shared/types/rewards';
import { TIER_COLORS, TIER_THRESHOLDS, TIER_ORDER, STAKES_OPTIONS, generateId } from '../constants';
import apiClient from '../api/client';
import TierTimeline from './TierTimeline';

interface PlayerCardProps {
  player: PlayerRewardsResponse;
  onPointsAwarded: (response: AwardPointsResponse) => void;
  onAdjustPoints: () => void;
}

function PlayerCard({ player, onPointsAwarded, onAdjustPoints }: PlayerCardProps) {
  const [stakesIndex, setStakesIndex] = useState(2);
  const [loading, setLoading] = useState(false);

  const tierIndex = TIER_ORDER.indexOf(player.tier);
  const currentThreshold = TIER_THRESHOLDS[player.tier];
  const nextThreshold = player.nextTierAt;
  const progress = nextThreshold
    ? ((player.totalEarned - currentThreshold) / (nextThreshold - currentThreshold)) * 100
    : 100;

  const handlePlayHand = async () => {
    const stakes = STAKES_OPTIONS[stakesIndex];
    setLoading(true);
    try {
      const { data } = await apiClient.post<AwardPointsResponse>('/points/award', {
        tableId: stakes.tableId,
        tableStakes: stakes.tableStakes,
        bigBlind: stakes.bigBlind,
        handId: generateId(),
      });
      onPointsAwarded(data);
    } catch (err) {
      console.error('Failed to award points:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Paper sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column', gap: 2.5, overflow: 'auto' }}>
      <Typography variant="h6" fontWeight={700}>
        Player Card
      </Typography>

      <Box display="flex" alignItems="center" gap={1.5}>
        <Chip
          label={player.tier}
          sx={{
            bgcolor: TIER_COLORS[player.tier],
            color: player.tier === 'Gold' || player.tier === 'Platinum' ? '#000' : '#fff',
            fontWeight: 700,
            fontSize: '0.85rem',
          }}
        />
        <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
          {player.playerId}
        </Typography>
        <Tooltip title="Adjust points">
          <IconButton size="small" onClick={onAdjustPoints} sx={{ color: 'text.secondary' }}>
            <TuneIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>

      <Box>
        <Typography variant="h4" fontWeight={700} color="primary.main">
          {player.points.toLocaleString()}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          current points
        </Typography>
      </Box>

      <Box>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
          Total earned: {player.totalEarned.toLocaleString()}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Hands played: {player.handsPlayed.toLocaleString()}
        </Typography>
      </Box>

      <Box>
        <Box display="flex" justifyContent="space-between" mb={0.5}>
          <Typography variant="caption" color="text.secondary">
            {player.tier}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {player.nextTierName ?? 'Max'}
          </Typography>
        </Box>
        <LinearProgress
          variant="determinate"
          value={Math.min(progress, 100)}
          sx={{
            height: 8,
            borderRadius: 4,
            bgcolor: 'rgba(255,255,255,0.08)',
            '& .MuiLinearProgress-bar': {
              borderRadius: 4,
              background: `linear-gradient(90deg, ${TIER_COLORS[player.tier]}, ${
                TIER_COLORS[TIER_ORDER[Math.min(tierIndex + 1, TIER_ORDER.length - 1)]]
              })`,
            },
          }}
        />
        {nextThreshold && (
          <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
            {player.totalEarned >= nextThreshold
              ? `Play a hand to re-promote to ${player.nextTierName}`
              : `${(nextThreshold - player.totalEarned).toLocaleString()} pts to ${player.nextTierName}`}
          </Typography>
        )}
      </Box>

      <Box>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          Stakes
        </Typography>
        <Select
          fullWidth
          size="small"
          value={stakesIndex}
          onChange={(e) => setStakesIndex(e.target.value as number)}
          sx={{ mb: 2 }}
        >
          {STAKES_OPTIONS.map((opt, i) => (
            <MenuItem key={i} value={i}>
              {opt.label}
            </MenuItem>
          ))}
        </Select>
        <Button
          fullWidth
          variant="contained"
          size="large"
          onClick={handlePlayHand}
          disabled={loading}
          sx={{ fontWeight: 700 }}
        >
          {loading ? 'Dealing...' : 'Play Hand'}
        </Button>
      </Box>

      <Box>
        <Typography variant="caption" color="text.secondary" fontWeight={600} sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
          Tier History
        </Typography>
        <TierTimeline playerId={player.playerId} refreshKey={player.totalEarned} currentTier={player.tier} currentPoints={player.points} />
      </Box>
    </Paper>
  );
}

export default PlayerCard;
