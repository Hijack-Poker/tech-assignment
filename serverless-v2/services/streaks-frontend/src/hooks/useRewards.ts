import { useState, useEffect, useCallback } from 'react';
import { getRewards } from '../api/streaks.api';
import type { RewardsResponse } from '../types/streaks.types';

interface UseRewardsResult {
  data: RewardsResponse | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useRewards(): UseRewardsResult {
  const [data, setData] = useState<RewardsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRewards = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const rewards = await getRewards();
      setData(rewards);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load reward data';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRewards();
  }, [fetchRewards]);

  return { data, loading, error, refetch: fetchRewards };
}
