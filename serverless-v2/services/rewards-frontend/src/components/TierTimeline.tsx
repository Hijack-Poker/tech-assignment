import { useCallback, useEffect, useState } from 'react';
import { Box, Chip, CircularProgress, Typography } from '@mui/material';
import type { TierTimelineResponse, TierHistoryEntry } from '@shared/types/rewards';
import type { TierName } from '@shared/types/rewards';
import { TIER_COLORS } from '../constants';
import apiClient from '../api/client';

interface TierTimelineProps {
  playerId: string;
  refreshKey?: number;
  currentTier?: string;
  currentPoints?: number;
}

function entryLabel(entry: TierHistoryEntry): string {
  if (entry.reason === 'current') return 'Now';
  if (entry.createdAt) {
    const d = new Date(entry.createdAt);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
  // Fallback for legacy monthKey-only entries
  const [year, month] = entry.monthKey.split('-');
  const date = new Date(Number(year), Number(month) - 1);
  return date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' });
}

function TierTimeline({ playerId, refreshKey, currentTier, currentPoints }: TierTimelineProps) {
  const [history, setHistory] = useState<TierHistoryEntry[]>([]);
  const [initialLoading, setInitialLoading] = useState(true);
  const [error, setError] = useState(false);

  const fetchHistory = useCallback(() => {
    setError(false);
    apiClient
      .get<TierTimelineResponse>(`/dev/tier-history/${playerId}`)
      .then(({ data }) => setHistory(data.history))
      .catch(() => setError(true))
      .finally(() => setInitialLoading(false));
  }, [playerId]);

  useEffect(() => {
    fetchHistory();
  }, [fetchHistory, refreshKey]);

  // Append a live "Now" entry showing current player state
  const displayHistory = (() => {
    if (!currentTier || currentPoints === undefined) return history;
    const liveEntry: TierHistoryEntry = {
      monthKey: '',
      tier: currentTier as TierName,
      points: currentPoints,
      totalEarned: 0,
      reason: 'current',
    };
    return [...history, liveEntry];
  })();

  if (initialLoading) {
    return (
      <Box display="flex" justifyContent="center" py={2}>
        <CircularProgress size={24} />
      </Box>
    );
  }

  if (error) {
    return (
      <Typography variant="body2" color="error" sx={{ py: 1 }}>
        Failed to load tier history
      </Typography>
    );
  }

  if (displayHistory.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
        No tier history available. Run a monthly reset to generate history.
      </Typography>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', py: 1 }}>
      {displayHistory.map((entry, i) => {
        const prevTier = i > 0 ? displayHistory[i - 1].tier : null;
        const changed = prevTier && prevTier !== entry.tier;
        const upgraded = changed &&
          ['Bronze', 'Silver', 'Gold', 'Platinum'].indexOf(entry.tier) >
          ['Bronze', 'Silver', 'Gold', 'Platinum'].indexOf(prevTier!);
        const lineColor = changed ? (upgraded ? '#4caf50' : '#f44336') : 'rgba(255,255,255,0.12)';

        return (
          <Box key={entry.createdAt || `live-${i}`} sx={{ display: 'flex', alignItems: 'stretch' }}>
            {/* Timeline connector */}
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: 24, mr: 1.5 }}>
              <Box sx={{
                width: 10,
                height: 10,
                borderRadius: '50%',
                bgcolor: TIER_COLORS[entry.tier as TierName],
                flexShrink: 0,
                mt: 1.2,
              }} />
              {i < displayHistory.length - 1 && (
                <Box sx={{ width: 2, flex: 1, bgcolor: lineColor }} />
              )}
            </Box>

            {/* Entry content */}
            <Box sx={{ pb: 2, flex: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" fontWeight={600} sx={{ minWidth: 52 }}>
                  {entryLabel(entry)}
                </Typography>
                <Chip
                  label={entry.tier}
                  size="small"
                  sx={{
                    bgcolor: TIER_COLORS[entry.tier as TierName],
                    color: entry.tier === 'Gold' || entry.tier === 'Platinum' ? '#000' : '#fff',
                    fontWeight: 700,
                    fontSize: '0.7rem',
                    height: 22,
                  }}
                />
                <Typography variant="caption" color="text.secondary">
                  {entry.points.toLocaleString()} pts
                </Typography>
              </Box>
              {changed && (
                <Typography variant="caption" sx={{ color: upgraded ? '#4caf50' : '#f44336', ml: '52px' }}>
                  {upgraded ? 'Upgraded' : 'Downgraded'} from {prevTier}
                </Typography>
              )}
            </Box>
          </Box>
        );
      })}
    </Box>
  );
}

export default TierTimeline;
