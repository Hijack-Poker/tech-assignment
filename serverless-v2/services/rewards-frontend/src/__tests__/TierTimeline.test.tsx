import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, cleanup } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import TierTimeline from '../components/TierTimeline';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
  },
}));

import apiClient from '../api/client';

function renderTimeline(playerId = 'player-001', refreshKey = 0, currentTier?: string, currentPoints?: number) {
  return render(
    <ThemeProvider theme={theme}>
      <TierTimeline playerId={playerId} refreshKey={refreshKey} currentTier={currentTier} currentPoints={currentPoints} />
    </ThemeProvider>,
  );
}

describe('TierTimeline', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('renders timeline entries after fetch', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: {
        playerId: 'player-001',
        history: [
          { monthKey: '2026-01', tier: 'Bronze', points: 100, totalEarned: 100, reason: 'monthly_reset', createdAt: '2026-01-15T10:00:00.000Z' },
          { monthKey: '2026-02', tier: 'Silver', points: 600, totalEarned: 700, reason: 'tier_change', createdAt: '2026-02-20T14:30:00.000Z' },
        ],
      },
    });

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText('Bronze')).toBeInTheDocument();
      expect(screen.getByText('Silver')).toBeInTheDocument();
    });
  });

  it('handles empty history', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: { playerId: 'player-001', history: [] },
    });

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText(/no tier history/i)).toBeInTheDocument();
    });
  });

  it('shows error on fetch failure', async () => {
    vi.mocked(apiClient.get).mockRejectedValue(new Error('Network error'));

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
    });
  });

  it('shows points for each entry', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: {
        playerId: 'player-001',
        history: [
          { monthKey: '2026-03', tier: 'Gold', points: 2500, totalEarned: 3000, reason: 'monthly_reset', createdAt: '2026-03-01T00:00:00.000Z' },
        ],
      },
    });

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText('2,500 pts')).toBeInTheDocument();
    });
  });

  it('shows multiple entries within the same month', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: {
        playerId: 'player-001',
        history: [
          { monthKey: '2026-03', tier: 'Bronze', points: 400, totalEarned: 400, reason: 'monthly_reset', createdAt: '2026-03-01T00:00:00.000Z' },
          { monthKey: '2026-03', tier: 'Silver', points: 600, totalEarned: 600, reason: 'tier_change', createdAt: '2026-03-15T12:00:00.000Z' },
        ],
      },
    });

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText('Bronze')).toBeInTheDocument();
      expect(screen.getByText('Silver')).toBeInTheDocument();
      expect(screen.getByText('400 pts')).toBeInTheDocument();
      expect(screen.getByText('600 pts')).toBeInTheDocument();
    });
  });

  it('appends "Now" live entry from props', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: {
        playerId: 'player-001',
        history: [
          { monthKey: '2026-01', tier: 'Bronze', points: 100, totalEarned: 100, reason: 'monthly_reset', createdAt: '2026-01-15T10:00:00.000Z' },
        ],
      },
    });

    renderTimeline('player-001', 0, 'Silver', 450);

    await waitFor(() => {
      expect(screen.getByText('Bronze')).toBeInTheDocument();
      expect(screen.getByText('Silver')).toBeInTheDocument();
      expect(screen.getByText('Now')).toBeInTheDocument();
      expect(screen.getByText('450 pts')).toBeInTheDocument();
    });
  });

  it('shows live entry even with empty history', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: { playerId: 'player-001', history: [] },
    });

    renderTimeline('player-001', 0, 'Bronze', 50);

    await waitFor(() => {
      expect(screen.getByText('Now')).toBeInTheDocument();
      expect(screen.getByText('Bronze')).toBeInTheDocument();
      expect(screen.getByText('50 pts')).toBeInTheDocument();
    });
  });

  it('uses date labels for entries with createdAt', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: {
        playerId: 'player-001',
        history: [
          { monthKey: '2026-03', tier: 'Silver', points: 600, totalEarned: 600, reason: 'tier_change', createdAt: '2026-03-15T12:00:00.000Z' },
        ],
      },
    });

    renderTimeline();

    await waitFor(() => {
      expect(screen.getByText('Mar 15')).toBeInTheDocument();
    });
  });
});
