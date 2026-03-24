import { useState, useEffect, useCallback } from 'react';
import { getStreaks } from '../api/streaks.api';
import type { StreakState } from '../types/streaks.types';

interface UseStreaksResult {
  data: StreakState | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useStreaks(): UseStreaksResult {
  const [data, setData] = useState<StreakState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStreaks = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const streaks = await getStreaks();
      setData(streaks);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load streak data';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStreaks();
  }, [fetchStreaks]);

  return { data, loading, error, refetch: fetchStreaks };
}
