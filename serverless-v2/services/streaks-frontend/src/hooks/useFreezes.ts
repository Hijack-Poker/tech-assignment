import { useState, useEffect, useCallback } from 'react';
import { getFreezes } from '../api/streaks.api';
import type { FreezeInfo } from '../types/streaks.types';

interface UseFreezesResult {
  data: FreezeInfo | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useFreezes(): UseFreezesResult {
  const [data, setData] = useState<FreezeInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchFreezes = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const freezes = await getFreezes();
      setData(freezes);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load freeze data';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchFreezes();
  }, [fetchFreezes]);

  return { data, loading, error, refetch: fetchFreezes };
}
