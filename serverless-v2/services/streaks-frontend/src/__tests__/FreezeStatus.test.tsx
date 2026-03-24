import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import FreezeStatus, { isFreezeActiveToday, formatSource } from '../components/FreezeStatus';
import type { FreezeInfo } from '../types/streaks.types';

vi.mock('../api/streaks.api', () => ({
  getFreezes: vi.fn(),
}));

import { getFreezes } from '../api/streaks.api';
const mockGetFreezes = vi.mocked(getFreezes);

const today = new Date().toISOString().slice(0, 10);

const mockFreezeData: FreezeInfo = {
  freezesAvailable: 2,
  freezesUsedThisMonth: 1,
  history: [
    { date: '2026-03-15', source: 'free_monthly' },
    { date: '2026-03-10', source: 'purchased' },
  ],
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('FreezeStatus', () => {
  it('shows loading skeleton while data is being fetched', () => {
    mockGetFreezes.mockReturnValue(new Promise(() => {}));

    render(<FreezeStatus />);

    expect(screen.getByTestId('freeze-loading')).toBeInTheDocument();
  });

  it('displays freeze count when freezes are available', async () => {
    mockGetFreezes.mockResolvedValue(mockFreezeData);

    render(<FreezeStatus />);

    await waitFor(() => {
      expect(screen.getByTestId('freeze-status')).toBeInTheDocument();
    });

    expect(screen.getByTestId('freeze-count')).toHaveTextContent('2');
    expect(screen.getByTestId('freeze-icon')).toBeInTheDocument();
  });

  it('displays zero when count is zero', async () => {
    mockGetFreezes.mockResolvedValue({
      ...mockFreezeData,
      freezesAvailable: 0,
    });

    render(<FreezeStatus />);

    await waitFor(() => {
      expect(screen.getByTestId('freeze-status')).toBeInTheDocument();
    });

    expect(screen.getByTestId('freeze-count')).toHaveTextContent('0');
  });

  it('shows "Freeze active today" when freeze was used today', async () => {
    mockGetFreezes.mockResolvedValue({
      ...mockFreezeData,
      history: [
        { date: today, source: 'free_monthly' },
        ...mockFreezeData.history,
      ],
    });

    render(<FreezeStatus />);

    await waitFor(() => {
      expect(screen.getByTestId('freeze-status')).toBeInTheDocument();
    });

    expect(screen.getByText('Freeze active today')).toBeInTheDocument();
  });

  it('shows "1 free monthly reset" when no freeze active today', async () => {
    mockGetFreezes.mockResolvedValue(mockFreezeData);

    render(<FreezeStatus />);

    await waitFor(() => {
      expect(screen.getByTestId('freeze-status')).toBeInTheDocument();
    });

    expect(screen.getByText('1 free monthly reset')).toBeInTheDocument();
  });

  it('shows error alert when API call fails', async () => {
    mockGetFreezes.mockRejectedValue(new Error('Network error'));

    render(<FreezeStatus />);

    await waitFor(() => {
      expect(screen.getByTestId('freeze-error')).toBeInTheDocument();
    });

    expect(screen.getByText('Network error')).toBeInTheDocument();
  });
});

describe('isFreezeActiveToday', () => {
  it('returns true when today is in the history', () => {
    expect(isFreezeActiveToday([{ date: today }])).toBe(true);
  });

  it('returns false when today is not in the history', () => {
    expect(isFreezeActiveToday([{ date: '2025-01-01' }])).toBe(false);
  });

  it('returns false for empty history', () => {
    expect(isFreezeActiveToday([])).toBe(false);
  });
});

describe('formatSource', () => {
  it('formats free_monthly correctly', () => {
    expect(formatSource('free_monthly')).toBe('Free Monthly');
  });

  it('formats purchased correctly', () => {
    expect(formatSource('purchased')).toBe('Purchased');
  });
});
