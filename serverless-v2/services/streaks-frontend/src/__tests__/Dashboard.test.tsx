import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Dashboard from '../pages/Dashboard';
import type { StreakState } from '../types/streaks.types';
import { ToastProvider } from '../context/ToastContext';

const mockStreakData: StreakState = {
  loginStreak: 12,
  playStreak: 5,
  bestLoginStreak: 20,
  bestPlayStreak: 10,
  freezesAvailable: 2,
  nextLoginMilestone: { days: 14, reward: 400, daysRemaining: 2 },
  nextPlayMilestone: { days: 7, reward: 300, daysRemaining: 2 },
  lastLoginDate: '2026-03-22',
  lastPlayDate: '2026-03-21',
  comboActive: true,
  comboMultiplier: 1.1,
};

vi.mock('../api/streaks.api', () => ({
  getStreaks: vi.fn(),
  checkIn: vi.fn().mockResolvedValue({}),
  getFreezes: vi.fn().mockResolvedValue({ freezesAvailable: 2, freezesUsedThisMonth: 0, history: [] }),
  getCalendar: vi.fn().mockResolvedValue({ month: '2026-03', days: [] }),
  getRewards: vi.fn().mockResolvedValue({ rewards: [] }),
  getMissions: vi.fn().mockResolvedValue({ missions: [] }),
  claimMission: vi.fn().mockResolvedValue({}),
  getLeaderboard: vi.fn().mockResolvedValue({ leaderboard: [], playerRank: null }),
  getResponsibleGaming: vi.fn().mockResolvedValue({ selfExcludedUntil: null }),
  updateResponsibleGaming: vi.fn().mockResolvedValue({}),
  selfExclude: vi.fn().mockResolvedValue({}),
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(() => ({
    user: { uid: 'test-user', displayName: 'Test Player', email: 'test@test.com' },
    loading: false,
    signOut: vi.fn(),
  })),
}));

vi.mock('../config/firebase', () => ({
  auth: {},
  googleProvider: {},
}));

import { getStreaks } from '../api/streaks.api';
const mockGetStreaks = vi.mocked(getStreaks);

function renderDashboard() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <Dashboard />
      </ToastProvider>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Dashboard', () => {
  it('shows loading skeleton while data is being fetched', () => {
    mockGetStreaks.mockReturnValue(new Promise(() => {}));

    renderDashboard();

    expect(screen.getByTestId('loading-skeleton')).toBeInTheDocument();
    expect(screen.getByText('Daily Streaks')).toBeInTheDocument();
  });

  it('renders login and play streak counters after data loads', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    const loginCounter = screen.getByTestId('streak-counter-login');
    expect(loginCounter).toHaveTextContent('12');
    expect(screen.getByText('Login Streak')).toBeInTheDocument();
    const playCounter = screen.getByTestId('streak-counter-play');
    expect(playCounter).toHaveTextContent('5');
    expect(screen.getByText('Play Streak')).toBeInTheDocument();
    expect(screen.getByTestId('flame-icon')).toBeInTheDocument();
    expect(screen.getByTestId('cards-icon')).toBeInTheDocument();
  });

  it('shows error alert when API call fails', async () => {
    mockGetStreaks.mockRejectedValue(new Error('Network error'));

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('error-alert')).toBeInTheDocument();
    });

    expect(screen.getByText('Network error')).toBeInTheDocument();
  });

  it('does not render streak counters when there is an error', async () => {
    mockGetStreaks.mockRejectedValue(new Error('Server error'));

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('error-alert')).toBeInTheDocument();
    });

    expect(screen.queryByTestId('streak-counters')).not.toBeInTheDocument();
  });

  it('renders the page title with fire icon', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    expect(screen.getByText('Daily Streaks')).toBeInTheDocument();
  });

  it('renders higher login streak with flame icon', async () => {
    const highStreakData: StreakState = {
      ...mockStreakData,
      loginStreak: 31,
    };
    mockGetStreaks.mockResolvedValue(highStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    expect(screen.getByTestId('streak-counter-login')).toHaveTextContent('31');
    expect(screen.getByTestId('flame-icon')).toBeInTheDocument();
  });

  it('renders milestone progress for login and play streaks', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    expect(screen.getByTestId('milestone-progress-login')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-progress-play')).toBeInTheDocument();
  });

  it('renders max milestone message when all milestones reached', async () => {
    const maxStreakData: StreakState = {
      ...mockStreakData,
      loginStreak: 95,
      playStreak: 100,
      nextLoginMilestone: null,
      nextPlayMilestone: null,
    };
    mockGetStreaks.mockResolvedValue(maxStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    expect(screen.getByTestId('milestone-max-login')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-max-play')).toBeInTheDocument();
  });

  it('renders RewardHistory section', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByTestId('reward-history')).toBeInTheDocument();
    });
  });

  it('renders Check In Today button', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    expect(screen.getByText('Check In Today')).toBeInTheDocument();
  });

  it('shows best streaks in the counter cards', async () => {
    mockGetStreaks.mockResolvedValue(mockStreakData);

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('streak-counters')).toBeInTheDocument();
    });

    expect(screen.getByText('Best: 20 days')).toBeInTheDocument();
    expect(screen.getByText('Best: 10 days')).toBeInTheDocument();
  });
});
