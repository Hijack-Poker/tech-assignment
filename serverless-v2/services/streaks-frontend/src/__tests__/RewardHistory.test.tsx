import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import RewardHistory, { formatRewardType, formatMilestone, sortByDateDesc } from '../components/RewardHistory';
import type { RewardsResponse, Reward } from '../types/streaks.types';

vi.mock('../api/streaks.api', () => ({
  getRewards: vi.fn(),
}));

import { getRewards } from '../api/streaks.api';
const mockGetRewards = vi.mocked(getRewards);

const mockRewardsData: RewardsResponse = {
  rewards: [
    { date: '2026-03-10', milestone: 7, type: 'login_milestone', points: 100 },
    { date: '2026-03-15', milestone: 14, type: 'play_milestone', points: 250 },
    { date: '2026-03-01', milestone: 3, type: 'login_milestone', points: 50 },
  ],
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('RewardHistory', () => {
  it('shows loading skeleton while data is being fetched', () => {
    mockGetRewards.mockReturnValue(new Promise(() => {}));

    render(<RewardHistory />);

    expect(screen.getByTestId('reward-loading')).toBeInTheDocument();
  });

  it('displays rewards sorted by date descending', async () => {
    mockGetRewards.mockResolvedValue(mockRewardsData);

    render(<RewardHistory />);

    await waitFor(() => {
      expect(screen.getByTestId('reward-history')).toBeInTheDocument();
    });

    expect(screen.getByTestId('reward-table')).toBeInTheDocument();

    // Milestones formatted
    expect(screen.getByText('14-day play milestone')).toBeInTheDocument();
    expect(screen.getByText('7-day login milestone')).toBeInTheDocument();
    expect(screen.getByText('3-day login milestone')).toBeInTheDocument();

    // Points displayed with + prefix
    expect(screen.getByText('+250')).toBeInTheDocument();
    expect(screen.getByText('+100')).toBeInTheDocument();
    expect(screen.getByText('+50')).toBeInTheDocument();
  });

  it('shows empty state when no rewards exist', async () => {
    mockGetRewards.mockResolvedValue({ rewards: [] });

    render(<RewardHistory />);

    await waitFor(() => {
      expect(screen.getByTestId('reward-history')).toBeInTheDocument();
    });

    expect(screen.getByTestId('reward-history-empty')).toBeInTheDocument();
    expect(screen.getByText('No rewards earned yet')).toBeInTheDocument();
  });

  it('shows error alert when API call fails', async () => {
    mockGetRewards.mockRejectedValue(new Error('Network error'));

    render(<RewardHistory />);

    await waitFor(() => {
      expect(screen.getByTestId('reward-error')).toBeInTheDocument();
    });

    expect(screen.getByText('Network error')).toBeInTheDocument();
  });

  it('displays the reward icon', async () => {
    mockGetRewards.mockResolvedValue(mockRewardsData);

    render(<RewardHistory />);

    await waitFor(() => {
      expect(screen.getByTestId('reward-icon')).toBeInTheDocument();
    });
  });
});

describe('formatRewardType', () => {
  it('formats login_milestone to Login', () => {
    expect(formatRewardType('login_milestone')).toBe('Login');
  });

  it('formats play_milestone to Play', () => {
    expect(formatRewardType('play_milestone')).toBe('Play');
  });

  it('formats unknown types with title case', () => {
    expect(formatRewardType('special_bonus')).toBe('Special Bonus');
  });
});

describe('formatMilestone', () => {
  it('formats login milestone', () => {
    expect(formatMilestone(7, 'login_milestone')).toBe('7-day login milestone');
  });

  it('formats play milestone', () => {
    expect(formatMilestone(14, 'play_milestone')).toBe('14-day play milestone');
  });
});

describe('sortByDateDesc', () => {
  it('sorts rewards by date descending', () => {
    const rewards: Reward[] = [
      { date: '2026-01-01', milestone: 3, type: 'login_milestone', points: 50 },
      { date: '2026-03-01', milestone: 7, type: 'login_milestone', points: 100 },
      { date: '2026-02-01', milestone: 14, type: 'play_milestone', points: 250 },
    ];
    const sorted = sortByDateDesc(rewards);
    expect(sorted[0].date).toBe('2026-03-01');
    expect(sorted[1].date).toBe('2026-02-01');
    expect(sorted[2].date).toBe('2026-01-01');
  });

  it('returns empty array for empty input', () => {
    expect(sortByDateDesc([])).toEqual([]);
  });

  it('does not mutate the original array', () => {
    const rewards: Reward[] = [
      { date: '2026-01-01', milestone: 3, type: 'login_milestone', points: 50 },
      { date: '2026-03-01', milestone: 7, type: 'login_milestone', points: 100 },
    ];
    sortByDateDesc(rewards);
    expect(rewards[0].date).toBe('2026-01-01');
  });
});
