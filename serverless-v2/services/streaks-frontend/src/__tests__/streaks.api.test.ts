import { describe, it, expect, vi, beforeEach } from 'vitest';
import { getStreaks, checkIn, getCalendar, getRewards, getFreezes } from '../api/streaks.api';
import apiClient from '../api/client';
import type {
  StreakState,
  CheckInResponse,
  CalendarResponse,
  RewardsResponse,
  FreezeInfo,
} from '../types/streaks.types';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

const mockGet = vi.mocked(apiClient.get);
const mockPost = vi.mocked(apiClient.post);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('getStreaks', () => {
  it('returns streak state from GET /streaks', async () => {
    const streakState: StreakState = {
      loginStreak: 5,
      playStreak: 3,
      bestLoginStreak: 10,
      bestPlayStreak: 7,
      freezesAvailable: 1,
      nextLoginMilestone: { days: 7, reward: 150, daysRemaining: 2 },
      nextPlayMilestone: { days: 7, reward: 300, daysRemaining: 4 },
      lastLoginDate: '2026-03-21',
      lastPlayDate: '2026-03-20',
      comboActive: true,
      comboMultiplier: 1.0,
    };
    mockGet.mockResolvedValue({ data: streakState });

    const result = await getStreaks();

    expect(mockGet).toHaveBeenCalledWith('/streaks');
    expect(result).toEqual(streakState);
  });

  it('propagates errors from the API client', async () => {
    mockGet.mockRejectedValue(new Error('Network error'));

    await expect(getStreaks()).rejects.toThrow('Network error');
  });
});

describe('checkIn', () => {
  it('returns check-in response from POST /streaks/check-in', async () => {
    const checkInResponse: CheckInResponse = {
      playerId: 'player-1',
      loginStreak: 6,
      bestLoginStreak: 10,
      todayCheckedIn: true,
      milestone: null,
    };
    mockPost.mockResolvedValue({ data: checkInResponse });

    const result = await checkIn();

    expect(mockPost).toHaveBeenCalledWith('/streaks/check-in');
    expect(result).toEqual(checkInResponse);
  });

  it('returns milestone when one is awarded', async () => {
    const checkInResponse: CheckInResponse = {
      playerId: 'player-1',
      loginStreak: 7,
      bestLoginStreak: 7,
      todayCheckedIn: true,
      milestone: {
        playerId: 'player-1',
        rewardId: 'reward-abc',
        type: 'login_milestone',
        milestone: 7,
        points: 150,
        streakCount: 7,
        createdAt: '2026-03-22T00:00:00.000Z',
      },
    };
    mockPost.mockResolvedValue({ data: checkInResponse });

    const result = await checkIn();

    expect(result.milestone).not.toBeNull();
    expect(result.milestone!.points).toBe(150);
  });
});

describe('getCalendar', () => {
  it('passes month param to GET /player/streaks/calendar', async () => {
    const calendarResponse: CalendarResponse = {
      month: '2026-03',
      days: [
        { date: '2026-03-01', activity: 'login_only', loginStreak: 1, playStreak: 0 },
        { date: '2026-03-02', activity: 'none', loginStreak: 0, playStreak: 0 },
      ],
    };
    mockGet.mockResolvedValue({ data: calendarResponse });

    const result = await getCalendar('2026-03');

    expect(mockGet).toHaveBeenCalledWith('/player/streaks/calendar', {
      params: { month: '2026-03' },
    });
    expect(result.month).toBe('2026-03');
    expect(result.days).toHaveLength(2);
  });

  it('propagates errors for invalid month', async () => {
    mockGet.mockRejectedValue({ response: { status: 400, data: { error: 'Bad Request' } } });

    await expect(getCalendar('invalid')).rejects.toEqual(
      expect.objectContaining({ response: expect.objectContaining({ status: 400 }) })
    );
  });
});

describe('getRewards', () => {
  it('returns rewards from GET /player/streaks/rewards', async () => {
    const rewardsResponse: RewardsResponse = {
      rewards: [
        { date: '2026-03-22T00:00:00.000Z', milestone: 7, type: 'login_milestone', points: 150 },
      ],
    };
    mockGet.mockResolvedValue({ data: rewardsResponse });

    const result = await getRewards();

    expect(mockGet).toHaveBeenCalledWith('/player/streaks/rewards');
    expect(result.rewards).toHaveLength(1);
    expect(result.rewards[0].points).toBe(150);
  });
});

describe('getFreezes', () => {
  it('returns freeze info from GET /player/streaks/freezes', async () => {
    const freezeInfo: FreezeInfo = {
      freezesAvailable: 2,
      freezesUsedThisMonth: 1,
      history: [{ date: '2026-03-15', source: 'monthly_grant' }],
    };
    mockGet.mockResolvedValue({ data: freezeInfo });

    const result = await getFreezes();

    expect(mockGet).toHaveBeenCalledWith('/player/streaks/freezes');
    expect(result.freezesAvailable).toBe(2);
    expect(result.history).toHaveLength(1);
  });

  it('propagates errors from the API client', async () => {
    mockGet.mockRejectedValue(new Error('Unauthorized'));

    await expect(getFreezes()).rejects.toThrow('Unauthorized');
  });
});
