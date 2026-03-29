import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import AdjustPointsModal from '../components/AdjustPointsModal';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    put: vi.fn(),
  },
}));

import apiClient from '../api/client';

const mockPlayerData = {
  playerId: 'player-001',
  tier: 'Silver',
  points: 750,
  totalEarned: 1200,
  nextTierAt: 2000,
  nextTierName: 'Gold',
  recentTransactions: [],
};

function renderModal(props: Partial<React.ComponentProps<typeof AdjustPointsModal>> = {}) {
  const defaultProps = {
    open: true,
    onClose: vi.fn(),
    playerId: 'player-001',
    onSaved: vi.fn(),
    ...props,
  };
  return render(
    <ThemeProvider theme={theme}>
      <AdjustPointsModal {...defaultProps} />
    </ThemeProvider>,
  );
}

function getDialog() {
  return screen.getAllByRole('dialog').find((el) => !el.closest('[aria-hidden="true"]'))!;
}

describe('AdjustPointsModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderModal();
    expect(screen.getByRole('progressbar')).toBeTruthy();
  });

  it('shows player data after fetch', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal();

    await waitFor(() => {
      expect(screen.getAllByText('Silver').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getAllByText(/Points: 750/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Total: 1,200/).length).toBeGreaterThanOrEqual(1);
  });

  it('pre-fills input fields with current values', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal();

    await waitFor(() => {
      const dialog = getDialog();
      const pointsInput = within(dialog).getByLabelText('Points') as HTMLInputElement;
      expect(pointsInput.value).toBe('750');
      const totalInput = within(dialog).getByLabelText('Total Earned') as HTMLInputElement;
      expect(totalInput.value).toBe('1200');
    });
  });

  it('computes tier preview from totalEarned value', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal();

    // Initial tier preview should show Silver (matching current tier)
    await waitFor(() => {
      const dialog = getDialog();
      const chips = within(dialog).getAllByText('Silver');
      expect(chips.length).toBeGreaterThanOrEqual(1);
    });

    // Verify the tier preview text exists
    expect(screen.getAllByText(/Tier preview/).length).toBeGreaterThanOrEqual(1);
  });

  it('cancel button is present and enabled after load', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal();

    await waitFor(() => {
      const dialog = getDialog();
      const cancelBtn = within(dialog).getByRole('button', { name: /cancel/i });
      expect(cancelBtn).not.toBeDisabled();
    });
  });

  it('save button is present and enabled after load', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal();

    await waitFor(() => {
      const dialog = getDialog();
      const saveBtn = within(dialog).getByRole('button', { name: /save/i });
      expect(saveBtn).not.toBeDisabled();
    });
  });

  it('calls PUT API on save with correct payload', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    (apiClient.put as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    const onSaved = vi.fn();
    const onClose = vi.fn();
    renderModal({ onSaved, onClose });

    await waitFor(() => {
      const dialog = getDialog();
      expect(within(dialog).getByRole('button', { name: /save/i })).not.toBeDisabled();
    });

    const dialog = getDialog();
    within(dialog).getByRole('button', { name: /save/i }).click();

    await waitFor(() => {
      expect(apiClient.put).toHaveBeenCalledWith('/dev/player/player-001/points', {
        points: 750,
        totalEarned: 1200,
        reason: 'dev_adjustment',
      });
      expect(onSaved).toHaveBeenCalled();
    });
  });

  it('shows error on fetch failure', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: { data: { message: 'Player not found' } },
    });
    renderModal();

    await waitFor(() => {
      expect(screen.getAllByText('Player not found').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('displays the player ID in the title', async () => {
    (apiClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPlayerData });
    renderModal({ playerId: 'player-042' });

    expect(screen.getAllByText(/player-042/).length).toBeGreaterThanOrEqual(1);
  });
});
