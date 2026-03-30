import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import Leaderboard from '../components/Leaderboard';
import type { LeaderboardEntry } from '@shared/types/rewards';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
  },
}));

import apiClient from '../api/client';

const topEntries: LeaderboardEntry[] = [
  { rank: 1, playerId: 'player-010', displayName: 'AceKing', tier: 'Platinum', points: 19000 },
  { rank: 2, playerId: 'player-005', displayName: 'BluffMaster', tier: 'Gold', points: 15000 },
  { rank: 3, playerId: 'player-001', displayName: 'TestPlayer', tier: 'Silver', points: 800 },
];

const nearbyEntries: LeaderboardEntry[] = [
  { rank: 24, playerId: 'player-020', displayName: 'NearAbove', tier: 'Silver', points: 850 },
  { rank: 25, playerId: 'player-001', displayName: 'TestPlayer', tier: 'Silver', points: 800 },
  { rank: 26, playerId: 'player-030', displayName: 'NearBelow', tier: 'Bronze', points: 750 },
];

function mockLeaderboardResponses() {
  vi.mocked(apiClient.get).mockImplementation((_url: string, config?: { params?: Record<string, unknown> }) => {
    if (config?.params?.view === 'nearby') {
      return Promise.resolve({ data: { leaderboard: nearbyEntries, playerRank: 25 } });
    }
    return Promise.resolve({ data: { leaderboard: topEntries, playerRank: 3 } });
  });
}

function renderLeaderboard(playerId: string, simulationActive?: boolean, onUpdate = vi.fn()) {
  return render(
    <ThemeProvider theme={theme}>
      <Leaderboard
        playerId={playerId}
        simulationActive={simulationActive}
        onLeaderboardUpdate={onUpdate}
      />
    </ThemeProvider>,
  );
}

describe('Leaderboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the Top 10 heading', async () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-099');

    await waitFor(() => {
      expect(screen.getByText('Top 10')).toBeInTheDocument();
    });
  });

  it('renders player names from API data', async () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-099');

    await waitFor(() => {
      expect(screen.getByText('AceKing')).toBeInTheDocument();
      expect(screen.getByText('BluffMaster')).toBeInTheDocument();
    });
  });

  it('highlights the current player with (You) label', async () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-001');

    await waitFor(() => {
      expect(screen.getAllByText(/TestPlayer \(You\)/).length).toBeGreaterThan(0);
    });
  });

  it('calls onLeaderboardUpdate with top and nearby entries', async () => {
    mockLeaderboardResponses();
    const onUpdate = vi.fn();
    renderLeaderboard('player-099', undefined, onUpdate);

    await waitFor(() => {
      expect(onUpdate).toHaveBeenCalledWith(topEntries, nearbyEntries);
    });
  });

  it('does not make API calls when simulationActive is true', async () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-001', true);

    await new Promise((r) => setTimeout(r, 50));
    expect(apiClient.get).not.toHaveBeenCalled();
  });

  it('renders formatted point values', async () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-099');

    await waitFor(() => {
      expect(screen.getAllByText('19,000').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('15,000').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders the Leaderboard title', () => {
    mockLeaderboardResponses();
    renderLeaderboard('player-001');
    expect(screen.getAllByText('Leaderboard').length).toBeGreaterThanOrEqual(1);
  });
});
