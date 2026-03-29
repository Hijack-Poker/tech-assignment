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
import { STAKES_OPTIONS, TIER_ORDER, TIER_THRESHOLDS, generateId } from '../constants';
import apiClient from '../api/client';
import PlayerCard from '../components/PlayerCard';
import Leaderboard, { LeaderboardHandle } from '../components/Leaderboard';
import ActivityFeed from '../components/ActivityFeed';
import SimulationControls from '../components/SimulationControls';
import AdjustPointsModal from '../components/AdjustPointsModal';
import NotificationBell, { NotificationBellHandle } from '../components/NotificationBell';

function Dashboard() {
  const navigate = useNavigate();
  const playerId = localStorage.getItem('playerId');

  const [playerData, setPlayerData] = useState<PlayerRewardsResponse | null>(null);
  const [simulationEnabled, setSimulationEnabled] = useState(false);
  const [newTransaction, setNewTransaction] = useState<TransactionResponse | undefined>();
  const [adjustModalOpen, setAdjustModalOpen] = useState(false);
  const [adjustPlayerId, setAdjustPlayerId] = useState<string | null>(null);
  const leaderboardEntriesRef = useRef<LeaderboardEntry[]>([]);
  const leaderboardRef = useRef<LeaderboardHandle>(null);
  const notificationBellRef = useRef<NotificationBellHandle>(null);

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
    const tierIdx = TIER_ORDER.indexOf(response.tier);
    const nextTierName = tierIdx < TIER_ORDER.length - 1 ? TIER_ORDER[tierIdx + 1] : null;
    const nextTierAt = nextTierName ? TIER_THRESHOLDS[nextTierName] : null;

    setPlayerData((prev: PlayerRewardsResponse | null) =>
      prev
        ? {
            ...prev,
            points: response.newPoints,
            totalEarned: response.newTotalEarned,
            handsPlayed: prev.handsPlayed + 1,
            tier: response.tier,
            nextTierAt,
            nextTierName,
          }
        : prev,
    );
    setNewTransaction(response.transaction);
    notificationBellRef.current?.refresh();
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

  const openAdjustModal = useCallback((targetPlayerId: string) => {
    setAdjustPlayerId(targetPlayerId);
    setAdjustModalOpen(true);
  }, []);

  const handleAdjustSaved = useCallback(() => {
    // Refresh player data and leaderboard
    if (playerId) {
      apiClient
        .get<PlayerRewardsResponse>('/player/rewards')
        .then(({ data }) => setPlayerData(data));
    }
    leaderboardRef.current?.refresh();
  }, [playerId]);

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
        <Box display="flex" alignItems="center" gap={1}>
          <NotificationBell ref={notificationBellRef} />
          <SimulationControls enabled={simulationEnabled} onToggle={setSimulationEnabled} />
        </Box>
      </Box>

      {/* Three-panel layout */}
      <Box sx={{ display: 'flex', flex: 1, overflow: 'hidden', gap: 2, p: 2 }}>
        {/* Left panel — Player Card */}
        <Box sx={{ width: 320, flexShrink: 0 }}>
          <PlayerCard player={playerData} onPointsAwarded={handlePointsAwarded} onAdjustPoints={() => openAdjustModal(playerId)} />
        </Box>

        {/* Center panel — Leaderboard */}
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Leaderboard ref={leaderboardRef} playerId={playerId} simulationActive={simulationEnabled} onLeaderboardUpdate={handleLeaderboardUpdate} onPlayerClick={openAdjustModal} />
        </Box>

        {/* Right panel — Activity Feed */}
        <Box sx={{ width: 340, flexShrink: 0 }}>
          <ActivityFeed newTransaction={newTransaction} />
        </Box>
      </Box>

      {adjustPlayerId && (
        <AdjustPointsModal
          open={adjustModalOpen}
          onClose={() => setAdjustModalOpen(false)}
          playerId={adjustPlayerId}
          onSaved={handleAdjustSaved}
        />
      )}
    </Box>
  );
}

export default Dashboard;
