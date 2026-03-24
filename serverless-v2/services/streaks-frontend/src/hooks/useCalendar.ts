import { useState, useEffect, useCallback } from 'react';
import { getCalendar } from '../api/streaks.api';
import type { CalendarDay } from '../types/streaks.types';

interface UseCalendarResult {
  days: CalendarDay[];
  month: string; // YYYY-MM
  loading: boolean;
  error: string | null;
  goToMonth: (month: string) => void;
  nextMonth: () => void;
  prevMonth: () => void;
  refetch: () => void;
}

function getCurrentMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function shiftMonth(month: string, delta: number): string {
  const [y, m] = month.split('-').map(Number);
  const d = new Date(y, m - 1 + delta, 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
}

export function useCalendar(): UseCalendarResult {
  const [month, setMonth] = useState(getCurrentMonth);
  const [days, setDays] = useState<CalendarDay[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCalendar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getCalendar(month);
      setDays(response.days || []);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load calendar data';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [month]);

  useEffect(() => {
    fetchCalendar();
  }, [fetchCalendar]);

  const goToMonth = useCallback((m: string) => setMonth(m), []);
  const nextMonth = useCallback(() => {
    const current = getCurrentMonth();
    // Don't go beyond current month
    if (month < current) setMonth((prev) => shiftMonth(prev, 1));
  }, [month]);
  const prevMonth = useCallback(() => setMonth((prev) => shiftMonth(prev, -1)), []);

  return { days, month, loading, error, goToMonth, nextMonth, prevMonth, refetch: fetchCalendar };
}
