import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import MonthlyResetButton from '../components/MonthlyResetButton';

vi.mock('../api/client', () => ({
  default: {
    post: vi.fn(),
  },
}));

import apiClient from '../api/client';

function renderButton(onResetComplete = vi.fn()) {
  return render(
    <ThemeProvider theme={theme}>
      <MonthlyResetButton onResetComplete={onResetComplete} />
    </ThemeProvider>,
  );
}

describe('MonthlyResetButton', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('renders the trigger button', () => {
    renderButton();
    expect(screen.getByRole('button', { name: /monthly reset/i })).toBeInTheDocument();
  });

  it('shows confirmation dialog on click', async () => {
    const user = userEvent.setup();
    renderButton();

    await user.click(screen.getByRole('button', { name: /monthly reset/i }));
    expect(screen.getByText(/Trigger Monthly Reset/)).toBeInTheDocument();
    expect(screen.getByText(/reset all players/i)).toBeInTheDocument();
  });

  it('calls API on confirm', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    vi.mocked(apiClient.post).mockResolvedValue({
      data: { processed: 50, downgrades: 10, resetMonth: '2026-04' },
    });

    renderButton(onComplete);
    await user.click(screen.getByRole('button', { name: /monthly reset/i }));
    await user.click(screen.getByRole('button', { name: /^reset$/i }));

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/dev/monthly-reset');
    });
    await waitFor(() => {
      expect(onComplete).toHaveBeenCalled();
    });
  });

  it('shows success message after reset', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.post).mockResolvedValue({
      data: { processed: 50, downgrades: 10, resetMonth: '2026-04' },
    });

    renderButton();
    await user.click(screen.getByRole('button', { name: /monthly reset/i }));
    await user.click(screen.getByRole('button', { name: /^reset$/i }));

    await waitFor(() => {
      expect(screen.getByText(/50 players processed, 10 tier downgrades/)).toBeInTheDocument();
    });
  });

  it('cancel button closes dialog without calling API', async () => {
    const user = userEvent.setup();
    renderButton();

    await user.click(screen.getByRole('button', { name: /monthly reset/i }));
    await user.click(screen.getByRole('button', { name: /cancel/i }));

    expect(apiClient.post).not.toHaveBeenCalled();
  });
});
