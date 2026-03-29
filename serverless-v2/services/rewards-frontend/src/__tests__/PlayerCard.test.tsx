import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import PlayerCard from '../components/PlayerCard';
import type { PlayerRewardsResponse } from '@shared/types/rewards';

vi.mock('../api/client', () => ({
  default: {
    post: vi.fn(),
  },
}));

import apiClient from '../api/client';

const mockPlayer: PlayerRewardsResponse = {
  playerId: 'player-001',
  tier: 'Silver',
  points: 750,
  totalEarned: 750,
  handsPlayed: 42,
  nextTierAt: 2000,
  nextTierName: 'Gold',
  recentTransactions: [],
};

function renderCard(player = mockPlayer, onPointsAwarded = vi.fn(), onAdjustPoints = vi.fn()) {
  return render(
    <ThemeProvider theme={theme}>
      <PlayerCard player={player} onPointsAwarded={onPointsAwarded} onAdjustPoints={onAdjustPoints} />
    </ThemeProvider>,
  );
}

describe('PlayerCard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('displays tier badge as a chip', () => {
    renderCard();
    expect(screen.getByText('Silver', { selector: '.MuiChip-label' })).toBeInTheDocument();
  });

  it('displays player ID', () => {
    renderCard();
    expect(screen.getAllByText('player-001').length).toBeGreaterThanOrEqual(1);
  });

  it('displays current points', () => {
    renderCard();
    expect(screen.getAllByText('750').length).toBeGreaterThanOrEqual(1);
  });

  it('shows points remaining to next tier', () => {
    renderCard();
    expect(screen.getAllByText(/1,250 pts to Gold/).length).toBeGreaterThanOrEqual(1);
  });

  it('shows "Max" label when at highest tier', () => {
    renderCard({
      ...mockPlayer,
      tier: 'Platinum',
      nextTierAt: null,
      nextTierName: null,
    });
    expect(screen.getByText('Max')).toBeInTheDocument();
  });

  it('posts to award endpoint on Play Hand click', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.post).mockResolvedValue({
      data: {
        playerId: 'player-001',
        earnedPoints: 5,
        newPoints: 755,
        newTotalEarned: 755,
        tier: 'Silver',
        transaction: { timestamp: Date.now(), type: 'gameplay', basePoints: 5, multiplier: 1, earnedPoints: 5, balanceAfter: 755 },
      },
    });

    renderCard();

    const buttons = screen.getAllByRole('button', { name: /play hand/i });
    await user.click(buttons[0]);

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/points/award', expect.objectContaining({
        tableStakes: expect.any(String),
        bigBlind: expect.any(Number),
        handId: expect.any(String),
      }));
    });
  });

  it('renders the default stakes option', () => {
    const { container } = renderCard();
    const selectDisplay = container.querySelector('.MuiSelect-select');
    expect(selectDisplay).toHaveTextContent(/Medium/);
  });

  it('shows the Play Hand button', () => {
    renderCard();
    expect(screen.getAllByRole('button', { name: /play hand/i }).length).toBeGreaterThanOrEqual(1);
  });

  it('shows total earned', () => {
    renderCard();
    expect(screen.getAllByText(/Total earned/).length).toBeGreaterThanOrEqual(1);
  });
});
