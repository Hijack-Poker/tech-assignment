import { forwardRef, useCallback, useImperativeHandle, useRef, useState } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { LeaderboardEntry, LeaderboardResponse } from '@shared/types/rewards';
import { TIER_COLORS } from '../constants';
import { usePolling } from '../hooks/usePolling';
import apiClient from '../api/client';

export interface LeaderboardHandle {
  refresh: () => void;
}

interface LeaderboardProps {
  playerId: string;
  simulationActive?: boolean;
  onLeaderboardUpdate: (top: LeaderboardEntry[], nearby: LeaderboardEntry[]) => void;
  onPlayerClick?: (playerId: string) => void;
}

const tableStyles = { tableLayout: 'fixed' } as const;
const colGroup = (
  <colgroup>
    <col style={{ width: 50 }} />
    <col />
    <col style={{ width: 60 }} />
    <col style={{ width: 140 }} />
  </colgroup>
);

function LeaderboardTable({
  entries,
  playerId,
  deltas,
  onPlayerClick,
}: {
  entries: LeaderboardEntry[];
  playerId: string;
  deltas: Record<string, number>;
  onPlayerClick?: (playerId: string) => void;
}) {
  return (
    <Table size="small" sx={tableStyles}>
      {colGroup}
      <TableHead>
        <TableRow>
          <TableCell sx={{ fontWeight: 700 }}>#</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Player</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Tier</TableCell>
          <TableCell sx={{ fontWeight: 700, textAlign: 'right' }}>Points</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {entries.map((entry) => {
          const isCurrentPlayer = entry.playerId === playerId;
          return (
            <TableRow
              key={entry.playerId}
              onClick={() => onPlayerClick?.(entry.playerId)}
              sx={{
                cursor: onPlayerClick ? 'pointer' : undefined,
                '&:hover': onPlayerClick
                  ? { bgcolor: 'rgba(255,255,255,0.04)' }
                  : undefined,
                ...(isCurrentPlayer && { bgcolor: 'rgba(108, 99, 255, 0.15)' }),
              }}
            >
              <TableCell>
                <Typography variant="body2" fontWeight={isCurrentPlayer ? 700 : 400}>
                  {entry.rank}
                </Typography>
              </TableCell>
              <TableCell>
                <Typography variant="body2" fontWeight={isCurrentPlayer ? 700 : 400}>
                  {entry.displayName}
                  {isCurrentPlayer && ' (You)'}
                </Typography>
              </TableCell>
              <TableCell>
                <Box
                  sx={{
                    width: 10,
                    height: 10,
                    borderRadius: '50%',
                    bgcolor: TIER_COLORS[entry.tier],
                    display: 'inline-block',
                  }}
                />
              </TableCell>
              <TableCell align="right">
                <Box display="flex" alignItems="center" justifyContent="flex-end" gap={0.75}>
                  {deltas[entry.playerId] != null && (
                    <Typography
                      variant="caption"
                      fontWeight={600}
                      sx={{
                        color: deltas[entry.playerId] > 0 ? '#4caf50' : '#f44336',
                        opacity: 0.9,
                      }}
                    >
                      {deltas[entry.playerId] > 0 ? '+' : ''}
                      {deltas[entry.playerId]}
                    </Typography>
                  )}
                  <Typography variant="body2" fontWeight={isCurrentPlayer ? 700 : 400}>
                    {entry.points.toLocaleString()}
                  </Typography>
                </Box>
              </TableCell>
            </TableRow>
          );
        })}
        {entries.length === 0 && (
          <TableRow>
            <TableCell colSpan={4} align="center">
              <Typography variant="body2" color="text.secondary">
                Loading...
              </Typography>
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
}

const Leaderboard = forwardRef<LeaderboardHandle, LeaderboardProps>(
  function Leaderboard({ playerId, simulationActive, onLeaderboardUpdate, onPlayerClick }, ref) {
    const [topEntries, setTopEntries] = useState<LeaderboardEntry[]>([]);
    const [nearbyEntries, setNearbyEntries] = useState<LeaderboardEntry[]>([]);
    const [playerRank, setPlayerRank] = useState<number | undefined>();
    const [deltas, setDeltas] = useState<Record<string, number>>({});
    const prevPointsRef = useRef<Record<string, number>>({});

    const fetchLeaderboard = useCallback(async () => {
      try {
        const [topRes, nearbyRes] = await Promise.all([
          apiClient.get<LeaderboardResponse>('/points/leaderboard', { params: { limit: 10 } }),
          apiClient.get<LeaderboardResponse>('/points/leaderboard', { params: { view: 'nearby' } }),
        ]);

        // Compute deltas from all entries combined
        const allEntries = [...topRes.data.leaderboard, ...nearbyRes.data.leaderboard];
        const prev = prevPointsRef.current;
        const newDeltas: Record<string, number> = {};
        const snapshot: Record<string, number> = {};
        for (const entry of allEntries) {
          if (entry.playerId in prev && !(entry.playerId in newDeltas)) {
            const diff = entry.points - prev[entry.playerId];
            if (diff !== 0) newDeltas[entry.playerId] = diff;
          }
          snapshot[entry.playerId] = entry.points;
        }
        setDeltas(newDeltas);
        prevPointsRef.current = snapshot;

        setTopEntries(topRes.data.leaderboard);
        setNearbyEntries(nearbyRes.data.leaderboard);
        setPlayerRank(topRes.data.playerRank ?? nearbyRes.data.playerRank);
        onLeaderboardUpdate(topRes.data.leaderboard, nearbyRes.data.leaderboard);
      } catch (err) {
        console.error('Failed to fetch leaderboard:', err);
      }
    }, [onLeaderboardUpdate]);

    usePolling(fetchLeaderboard, 5000, !simulationActive);

    useImperativeHandle(ref, () => ({ refresh: fetchLeaderboard }), [fetchLeaderboard]);

    // Check if current player is already visible in Top 10
    const inTop10 = topEntries.some((e) => e.playerId === playerId);

    return (
      <Paper sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6" fontWeight={700}>
            Leaderboard
          </Typography>
          {playerRank != null && (
            <Typography variant="body2" color="primary.main" fontWeight={600}>
              Your Rank: #{playerRank}
            </Typography>
          )}
        </Box>

        <Box sx={{ flex: 1, overflow: 'auto' }}>
          <Typography variant="overline" color="text.secondary" sx={{ px: 1 }}>
            Top 10
          </Typography>
          <TableContainer>
            <LeaderboardTable entries={topEntries} playerId={playerId} deltas={deltas} onPlayerClick={onPlayerClick} />
          </TableContainer>

          {!inTop10 && nearbyEntries.length > 0 && (
            <Box sx={{ mt: 3 }}>
              <Typography variant="overline" color="text.secondary" sx={{ px: 1 }}>
                My Rank
              </Typography>
              <TableContainer>
                <LeaderboardTable entries={nearbyEntries} playerId={playerId} deltas={deltas} onPlayerClick={onPlayerClick} />
              </TableContainer>
            </Box>
          )}
        </Box>
      </Paper>
    );
  },
);

export default Leaderboard;
