import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import ActivityFeed from '../components/ActivityFeed';
import type { TransactionResponse } from '@shared/types/rewards';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
  },
}));

import apiClient from '../api/client';

const mockTransactions: TransactionResponse[] = [
  {
    timestamp: Date.now() - 60_000,
    type: 'gameplay',
    basePoints: 5,
    multiplier: 1,
    earnedPoints: 5,
    tableStakes: '$1/$2',
    balanceAfter: 1000,
  },
  {
    timestamp: Date.now() - 3_600_000,
    type: 'gameplay',
    basePoints: 10,
    multiplier: 1.5,
    earnedPoints: 15,
    tableStakes: '$5/$10',
    balanceAfter: 995,
  },
];

function mockHistory(txs: TransactionResponse[], cursor: string | null = null) {
  return { data: { transactions: txs, total: txs.length, limit: 20, cursor } };
}

function renderFeed(newTransaction?: TransactionResponse) {
  return render(
    <ThemeProvider theme={theme}>
      <ActivityFeed newTransaction={newTransaction} />
    </ThemeProvider>,
  );
}

describe('ActivityFeed', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches and renders transaction points', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions));
    renderFeed();

    await waitFor(() => {
      expect(screen.getAllByText('+5 pts').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('+15 pts').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('calls the history API with correct params', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions));
    renderFeed();

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/player/history', {
        params: { limit: 20 },
      });
    });
  });

  it('shows table stakes info', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions));
    renderFeed();

    await waitFor(() => {
      expect(screen.getAllByText('$1/$2 hand').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('$5/$10 hand (1.5x)').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows balance values', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions));
    renderFeed();

    await waitFor(() => {
      expect(screen.getAllByText(/^Balance:/).length).toBeGreaterThanOrEqual(2);
    });
  });

  it('shows Load More when cursor exists', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions, 'next'));
    renderFeed();

    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: /load more/i }).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('prepends new transactions from prop', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory(mockTransactions));

    const { rerender } = renderFeed();

    await waitFor(() => {
      expect(screen.getAllByText('+5 pts').length).toBeGreaterThanOrEqual(1);
    });

    const newTx: TransactionResponse = {
      timestamp: Date.now(),
      type: 'gameplay',
      basePoints: 8,
      multiplier: 1,
      earnedPoints: 8,
      tableStakes: '$1/$2',
      balanceAfter: 1008,
    };

    rerender(
      <ThemeProvider theme={theme}>
        <ActivityFeed newTransaction={newTx} />
      </ThemeProvider>,
    );

    expect(screen.getAllByText('+8 pts').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no transactions', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory([]));
    renderFeed();

    await waitFor(() => {
      expect(screen.getByText(/no transactions yet/i)).toBeInTheDocument();
    });
  });

  it('renders the Activity Feed title', async () => {
    vi.mocked(apiClient.get).mockResolvedValue(mockHistory([]));
    renderFeed();
    expect(screen.getAllByText('Activity Feed').length).toBeGreaterThanOrEqual(1);
  });
});
