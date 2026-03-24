import { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, CircularProgress, Alert, IconButton, Button } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import BlockIcon from '@mui/icons-material/Block';
import PokerTable from '../components/poker/table/PokerTable';
import GameControls from '../components/poker/controls/GameControls';
import ActionButtons from '../components/poker/controls/ActionButtons';
import PhaseIndicator from '../components/poker/info/PhaseIndicator';
import HandHistoryLog from '../components/poker/info/HandHistoryLog';
import Celebration from '../components/Celebration';
import WinCelebration from '../components/poker/WinCelebration';
import { useGameState } from '../hooks/useGameState';
import { useAutoPlay } from '../hooks/useAutoPlay';
import { useHandHistory } from '../hooks/useHandHistory';
import { useTurnTimer } from '../hooks/useTurnTimer';
import { PHASE_LABELS } from '../types/poker.types';
import { notifyHandCompleted } from '../api/poker.api';
import { getResponsibleGaming } from '../api/streaks.api';

const TABLE_ID = 1;

function PokerGame() {
  const navigate = useNavigate();
  const { tableState, loading, error, refresh, advance, sendAction, reset } = useGameState(TABLE_ID);
  const { entries, addEntry } = useHandHistory();
  const prevStepRef = useRef<string | null>(null);
  const [celebrating, setCelebrating] = useState(false);
  const [winCelebrating, setWinCelebrating] = useState(false);
  const [winAmount, setWinAmount] = useState(0);
  const [winnerName, setWinnerName] = useState('');
  const [selfExcludedUntil, setSelfExcludedUntil] = useState<string | null>(null);
  const [exclusionChecked, setExclusionChecked] = useState(false);

  useEffect(() => {
    getResponsibleGaming()
      .then((data) => {
        setSelfExcludedUntil(data.selfExcludedUntil);
        setExclusionChecked(true);
      })
      .catch(() => setExclusionChecked(true));
  }, []);

  const isExcluded = selfExcludedUntil && new Date(selfExcludedUntil) > new Date();

  const handleAdvance = async () => {
    // During betting rounds, auto-play for the acting player (check or call)
    let result;
    if (tableState?.game.stepName.includes('BETTING')) {
      const seat = tableState.game.move;
      const player = tableState.players.find((p) => p.seat === seat && p.status === '1');
      if (player) {
        const toCall = tableState.game.currentBet - player.bet;
        const action = toCall > 0 ? 'call' : 'check';
        result = await sendAction(seat, action);
      } else {
        result = await advance();
      }
    } else {
      result = await advance();
    }
    if (result) {
      const label = PHASE_LABELS[result.game.stepName] || result.game.stepName;
      addEntry(`Hand #${result.game.gameNo} — ${label}`, 'step');

      if (result.game.stepName === 'PAY_WINNERS') {
        const winners = result.players.filter((p) => p.winnings > 0);
        winners.forEach((p) => {
          addEntry(`${p.username} wins $${p.winnings.toFixed(2)}${p.handRank ? ` (${p.handRank})` : ''}`, 'winner');
        });

        // Trigger win celebration
        if (winners.length > 0) {
          const topWinner = winners.reduce((a, b) => a.winnings > b.winnings ? a : b);
          setWinAmount(topWinner.winnings);
          setWinnerName(topWinner.username);
          setWinCelebrating(true);
        }

        // Notify streaks API — updates play streak for the logged-in player
        const playerId = localStorage.getItem('playerId');
        if (playerId) {
          const handId = `hand-${result.game.gameNo}-${Date.now()}`;
          const { streakUpdated } = await notifyHandCompleted(playerId, TABLE_ID, handId);
          if (streakUpdated) {
            addEntry('Play streak updated!', 'info');
            setCelebrating(true);
          }
        }
      }
    }
  };

  const handleAction = useCallback(async (seat: number, action: string, amount?: number) => {
    const result = await sendAction(seat, action, amount);
    if (result) {
      const player = result.players.find((p) => p.seat === seat);
      const name = player?.username || `Seat ${seat}`;
      const amountStr = amount ? ` $${amount}` : '';
      addEntry(`${name}: ${action.toUpperCase()}${amountStr}`, 'step');
    }
  }, [sendAction, addEntry]);

  // Determine acting seat for the timer
  const isBettingStep = tableState?.game.stepName.includes('BETTING') ?? false;
  const actingSeat = isBettingStep && tableState
    ? (() => {
        const seat = tableState.game.move;
        const player = tableState.players.find((p) => p.seat === seat && p.status === '1');
        return player ? seat : null;
      })()
    : null;

  // Auto-fold on timeout
  const handleTimeout = useCallback((seat: number) => {
    handleAction(seat, 'fold');
    addEntry(`Seat ${seat} auto-folded (time expired)`, 'error');
  }, [handleAction, addEntry]);

  const { timeLeft, progress } = useTurnTimer(actingSeat, handleTimeout);

  const { isPlaying, speedLabel, toggle, cycleSpeed, stop } = useAutoPlay(handleAdvance);

  // Detect hand transitions
  useEffect(() => {
    if (!tableState) return;
    const currentStep = tableState.game.stepName;
    if (prevStepRef.current === 'RECORD_STATS_AND_NEW_HAND' && currentStep === 'GAME_PREP') {
      addEntry('--- New Hand ---', 'info');
    }
    prevStepRef.current = currentStep;
  }, [tableState, addEntry]);

  if (!exclusionChecked || loading) {
    return (
      <Box display="flex" alignItems="center" justifyContent="center" minHeight="100vh" bgcolor="#0a0e14">
        <CircularProgress />
      </Box>
    );
  }

  if (isExcluded) {
    return (
      <Box display="flex" flexDirection="column" alignItems="center" justifyContent="center" minHeight="100vh" bgcolor="#0a0e14" gap={2} p={4}>
        <BlockIcon sx={{ fontSize: 64, color: '#EF5350' }} />
        <Typography variant="h5" fontWeight={700} color="#fff">
          Self-Exclusion Active
        </Typography>
        <Typography color="#8B8FA3" fontSize={14} textAlign="center" maxWidth={400}>
          You are self-excluded until {new Date(selfExcludedUntil!).toLocaleDateString()}.
          Playing is blocked during this period.
        </Typography>
        <Button
          onClick={() => navigate('/')}
          startIcon={<ArrowBackIcon />}
          sx={{ color: '#90CAF9', textTransform: 'none', mt: 1 }}
        >
          Back to Dashboard
        </Button>
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" flexDirection="column" alignItems="center" justifyContent="center" minHeight="100vh" bgcolor="#0a0e14" gap={2} p={4}>
        <Alert severity="error" sx={{ maxWidth: 500 }}>{error}</Alert>
        <Typography color="text.secondary" fontSize={13}>
          Make sure the engine profile is running: docker compose --profile engine up
        </Typography>
        <IconButton onClick={() => navigate('/')} sx={{ color: '#90CAF9' }}>
          <ArrowBackIcon /> <Typography sx={{ ml: 1, fontSize: 14 }}>Back to Dashboard</Typography>
        </IconButton>
      </Box>
    );
  }

  if (!tableState) return null;

  return (
    <Box sx={{ height: '100vh', bgcolor: '#0a0e14', display: 'flex', overflow: 'hidden' }}>
      <Celebration active={celebrating} onComplete={() => setCelebrating(false)} />
      <WinCelebration active={winCelebrating} amount={winAmount} winnerName={winnerName} onComplete={() => setWinCelebrating(false)} />
      {/* Main area */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0 }}>
        {/* Header */}
        <Box
          sx={{
            px: { xs: 1, md: 2 },
            py: 1,
            display: 'flex',
            alignItems: 'center',
            bgcolor: '#0d1219',
            borderBottom: '1px solid #1e2a3a',
            flexShrink: 0,
          }}
        >
          <IconButton onClick={() => navigate('/')} sx={{ color: '#90CAF9', mr: 1 }} size="small">
            <ArrowBackIcon fontSize="small" />
          </IconButton>
          <Box flex={1} display="flex" justifyContent="center">
            <PhaseIndicator
              handStep={tableState.game.handStep}
              stepName={tableState.game.stepName}
              gameNo={tableState.game.gameNo}
            />
          </Box>
        </Box>

        {/* Table */}
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <PokerTable
            tableState={tableState}
            timerProgress={isBettingStep ? progress : undefined}
            timeLeft={isBettingStep ? timeLeft : undefined}
            heroPlayerId={localStorage.getItem('playerId')}
            heroDisplayName={localStorage.getItem('displayName')}
          />
        </Box>

        {/* Bottom panel */}
        <Box
          sx={{
            flexShrink: 0,
            bgcolor: '#0d1219',
            borderTop: '1px solid #1e2a3a',
            px: { xs: 1, md: 2 },
            py: 1,
            display: 'flex',
            flexDirection: 'column',
            gap: 1,
          }}
        >
          <ActionButtons
            game={isBettingStep ? tableState.game : undefined}
            players={isBettingStep ? tableState.players : undefined}
            onAction={isBettingStep ? handleAction : undefined}
          />
          <GameControls
            onNextStep={handleAdvance}
            onToggleAuto={toggle}
            onCycleSpeed={cycleSpeed}
            onReset={() => { stop(); reset(); }}
            isPlaying={isPlaying}
            speedLabel={speedLabel}
          />
        </Box>
      </Box>

      {/* Hand history sidebar */}
      <Box
        sx={{
          width: { md: 220, lg: 250 },
          flexShrink: 0,
          bgcolor: '#0d1117',
          borderLeft: '1px solid #1e2a3a',
          p: 1.5,
          overflowY: 'auto',
          display: { xs: 'none', md: 'block' },
        }}
      >
        <HandHistoryLog entries={entries} />
      </Box>
    </Box>
  );
}

export default PokerGame;
