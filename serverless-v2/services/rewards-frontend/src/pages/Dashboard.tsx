import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';
import type {
  AwardPointsResponse,
  LeaderboardEntry,
  PlayerRewardsResponse,
  TransactionResponse,
} from '@shared/types/rewards';
import { STAKES_OPTIONS, generateId } from '../constants';
import apiClient from '../api/client';
import PlayerCard from '../components/PlayerCard';
import Leaderboard, { LeaderboardHandle } from '../components/Leaderboard';
import ActivityFeed from '../components/ActivityFeed';
import SimulationControls from '../components/SimulationControls';

function Dashboard() {
  const navigate = useNavigate();
  const playerId = localStorage.getItem('playerId');

  const [playerData, setPlayerData] = useState<PlayerRewardsResponse | null>(null);
  const [simulationEnabled, setSimulationEnabled] = useState(false);
  const [newTransaction, setNewTransaction] = useState<TransactionResponse | undefined>();
  const leaderboardEntriesRef = useRef<LeaderboardEntry[]>([]);
  const leaderboardRef = useRef<LeaderboardHandle>(null);

  // Redirect to login if no player ID
  useEffect(() => {
    if (!playerId) {
      navigate('/login');
    }
  }, [playerId, navigate]);

  // Fetch player rewards on mount
  useEffect(() => {
    if (!playerId) return;
    apiClient
      .get<PlayerRewardsResponse>('/player/rewards')
      .then(({ data }) => setPlayerData(data))
      .catch((err) => {
        console.error('Failed to fetch player data:', err);
        if (err.response?.status === 401 || err.response?.status === 404) {
          navigate('/login');
        }
      });
  }, [playerId, navigate]);

  // Handle points awarded (from Play Hand button)
  const handlePointsAwarded = useCallback((response: AwardPointsResponse) => {
    setPlayerData((prev) =>
      prev
        ? {
            ...prev,
            points: response.newPoints,
            totalEarned: response.newTotalEarned,
            tier: response.tier,
          }
        : prev,
    );
    setNewTransaction(response.transaction);
  }, []);

  // Track leaderboard entries for simulation targeting (top + nearby, deduplicated)
  const handleLeaderboardUpdate = useCallback((top: LeaderboardEntry[], nearby: LeaderboardEntry[]) => {
    const seen = new Set<string>();
    const merged: LeaderboardEntry[] = [];
    for (const entry of [...top, ...nearby]) {
      if (!seen.has(entry.playerId)) {
        seen.add(entry.playerId);
        merged.push(entry);
      }
    }
    leaderboardEntriesRef.current = merged;
  }, []);

  // Simulation: award points to random leaderboard players every 2.5s
  useEffect(() => {
    if (!simulationEnabled) return;

    const interval = setInterval(() => {
      const entries = leaderboardEntriesRef.current.filter((e) => e.playerId !== playerId);
      if (entries.length === 0) return;

      const count = Math.floor(Math.random() * 3) + 1;
      const shuffled = [...entries].sort(() => Math.random() - 0.5);
      const targets = shuffled.slice(0, Math.min(count, shuffled.length));

      const promises = targets.map((target) => {
        const stakes = STAKES_OPTIONS[Math.floor(Math.random() * STAKES_OPTIONS.length)];
        return apiClient
          .post(
            '/points/award',
            {
              tableId: stakes.tableId,
              tableStakes: stakes.tableStakes,
              bigBlind: stakes.bigBlind,
              handId: generateId(),
            },
            {
              headers: { 'X-Player-Id': target.playerId },
            },
          )
          .catch(() => {});
      });
      Promise.all(promises).then(() => leaderboardRef.current?.refresh());
    }, 1000);

    return () => clearInterval(interval);
  }, [simulationEnabled, playerId]);

  if (!playerId) return null;

  if (!playerData) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          px: 3,
          py: 2,
          borderBottom: '1px solid rgba(255,255,255,0.08)',
        }}
      >
        <Box display="flex" alignItems="center" gap={1.5}>
          <EmojiEventsIcon sx={{ fontSize: 28, color: 'primary.main' }} />
          <Typography variant="h6" fontWeight={700}>
            Rewards Dashboard
          </Typography>
        </Box>
        <SimulationControls enabled={simulationEnabled} onToggle={setSimulationEnabled} />
      </Box>

      {/* Three-panel layout */}
      <Box sx={{ display: 'flex', flex: 1, overflow: 'hidden', gap: 2, p: 2 }}>
        {/* Left panel — Player Card */}
        <Box sx={{ width: 320, flexShrink: 0 }}>
          <PlayerCard player={playerData} onPointsAwarded={handlePointsAwarded} />
        </Box>

        {/* Center panel — Leaderboard */}
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Leaderboard ref={leaderboardRef} playerId={playerId} simulationActive={simulationEnabled} onLeaderboardUpdate={handleLeaderboardUpdate} />
        </Box>

        {/* Right panel — Activity Feed */}
        <Box sx={{ width: 340, flexShrink: 0 }}>
          <ActivityFeed newTransaction={newTransaction} />
        </Box>
      </Box>
    </Box>
  );
}

export default Dashboard;
