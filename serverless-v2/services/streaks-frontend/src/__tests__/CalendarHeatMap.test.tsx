import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import CalendarHeatMap, {
  ACTIVITY_COLORS,
  ACTIVITY_LABELS,
  getActivityColor,
  formatDate,
} from '../components/CalendarHeatMap';
import type { CalendarDay, CalendarResponse } from '../types/streaks.types';

vi.mock('../api/streaks.api', () => ({
  getCalendar: vi.fn(),
}));

import { getCalendar } from '../api/streaks.api';
const mockGetCalendar = vi.mocked(getCalendar);

/** Build days for the current month (all days in the month). */
function buildMonthDays(
  activity: CalendarDay['activity'] = 'none',
): CalendarDay[] {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); // 0-based
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  return Array.from({ length: daysInMonth }, (_, i) => {
    const day = i + 1;
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    return {
      date: dateStr,
      activity,
      loginStreak: 0,
      playStreak: 0,
    };
  });
}

function getCurrentMonthStr(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function mockCalendarResponse(days: CalendarDay[]) {
  mockGetCalendar.mockResolvedValue({ month: getCurrentMonthStr(), days } as CalendarResponse);
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('CalendarHeatMap', () => {
  it('renders day cells for the current month when calendar data loads', async () => {
    const days = buildMonthDays();
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cells = screen.getByTestId('calendar-grid').querySelectorAll('[data-testid^="calendar-cell-"]');
    expect(cells.length).toBe(days.length);
  });

  it('shows loading skeleton while data is being fetched', () => {
    mockGetCalendar.mockReturnValue(new Promise(() => {}));

    render(<CalendarHeatMap />);

    expect(screen.getByTestId('calendar-loading')).toBeInTheDocument();
  });

  it('renders the title with calendar icon', async () => {
    mockCalendarResponse(buildMonthDays());

    render(<CalendarHeatMap />);

    expect(screen.getByText('Activity')).toBeInTheDocument();
  });

  it('colors cells for activity "played"', async () => {
    const days = buildMonthDays('played');
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cell = screen.getByTestId(`calendar-cell-${days[0].date}`);
    expect(cell).toHaveAttribute('data-activity', 'played');
  });

  it('colors cells for activity "freeze"', async () => {
    const days = buildMonthDays('freeze');
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cell = screen.getByTestId(`calendar-cell-${days[0].date}`);
    expect(cell).toHaveAttribute('data-activity', 'freeze');
  });

  it('colors cells for activity "streak_broken"', async () => {
    const days = buildMonthDays('streak_broken');
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cell = screen.getByTestId(`calendar-cell-${days[0].date}`);
    expect(cell).toHaveAttribute('data-activity', 'streak_broken');
  });

  it('shows tooltip with date and activity on hover', async () => {
    const days = buildMonthDays('played');
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cell = screen.getByTestId(`calendar-cell-${days[0].date}`);
    await userEvent.hover(cell);

    await waitFor(() => {
      expect(screen.getByRole('tooltip')).toBeInTheDocument();
    });

    expect(screen.getByRole('tooltip')).toHaveTextContent('Played');
  });

  it('shows error message when API call fails', async () => {
    mockGetCalendar.mockRejectedValue(new Error('Network error'));

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-error')).toBeInTheDocument();
    });

    expect(screen.getByText('Network error')).toBeInTheDocument();
  });

  it('renders all cells with none activity when no calendar data is available', async () => {
    mockCalendarResponse([]);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const now = new Date();
    const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
    const cells = screen.getByTestId('calendar-grid').querySelectorAll('[data-activity="none"]');
    expect(cells.length).toBe(daysInMonth);
  });

  it('renders the legend with all activity types', async () => {
    mockCalendarResponse(buildMonthDays());

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-legend')).toBeInTheDocument();
    });

    expect(screen.getByText('No activity')).toBeInTheDocument();
    expect(screen.getByText('Login only')).toBeInTheDocument();
    expect(screen.getByText('Played')).toBeInTheDocument();
    expect(screen.getByText('Freeze used')).toBeInTheDocument();
    expect(screen.getByText('Streak broken')).toBeInTheDocument();
  });

  it('shows day numbers inside cells', async () => {
    const days = buildMonthDays('none');
    mockCalendarResponse(days);

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    const cell = screen.getByTestId(`calendar-cell-${days[0].date}`);
    expect(cell).toHaveTextContent('1');
  });

  it('renders weekday headers', async () => {
    mockCalendarResponse(buildMonthDays());

    render(<CalendarHeatMap />);

    await waitFor(() => {
      expect(screen.getByTestId('calendar-grid')).toBeInTheDocument();
    });

    expect(screen.getByText('Mon')).toBeInTheDocument();
    expect(screen.getByText('Sun')).toBeInTheDocument();
  });
});

describe('getActivityColor', () => {
  it('returns correct color for each activity type', () => {
    expect(getActivityColor('none')).toBe('#2A2D3A');
    expect(getActivityColor('login_only')).toBe('#A5D6A7');
    expect(getActivityColor('played')).toBe('#2E7D32');
    expect(getActivityColor('freeze')).toBe('#42A5F5');
    expect(getActivityColor('streak_broken')).toBe('#EF5350');
  });
});

describe('formatDate', () => {
  it('formats a date string as "Mon DD"', () => {
    const result = formatDate('2026-03-15');
    expect(result).toBe('Mar 15');
  });
});

describe('ACTIVITY_COLORS', () => {
  it('maps all activity types to the specified hex colors', () => {
    expect(ACTIVITY_COLORS.none).toBe('#2A2D3A');
    expect(ACTIVITY_COLORS.login_only).toBe('#A5D6A7');
    expect(ACTIVITY_COLORS.played).toBe('#2E7D32');
    expect(ACTIVITY_COLORS.freeze).toBe('#42A5F5');
    expect(ACTIVITY_COLORS.streak_broken).toBe('#EF5350');
  });
});

describe('ACTIVITY_LABELS', () => {
  it('maps all activity types to human-readable labels', () => {
    expect(ACTIVITY_LABELS.none).toBe('No activity');
    expect(ACTIVITY_LABELS.login_only).toBe('Login only');
    expect(ACTIVITY_LABELS.played).toBe('Played');
    expect(ACTIVITY_LABELS.freeze).toBe('Freeze used');
    expect(ACTIVITY_LABELS.streak_broken).toBe('Streak broken');
  });
});
