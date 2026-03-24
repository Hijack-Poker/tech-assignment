import { useState, useEffect, useCallback } from 'react';
import { getLeaderboard } from '../api/streaks.api';
import type { LeaderboardEntry } from '../types/streaks.types';

export function useLeaderboard(type: string = 'combined') {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [playerRank, setPlayerRank] = useState<LeaderboardEntry | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getLeaderboard(type);
      setLeaderboard(data.leaderboard);
      setPlayerRank(data.playerRank);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load leaderboard');
    } finally {
      setLoading(false);
    }
  }, [type]);

  useEffect(() => { fetch(); }, [fetch]);

  return { leaderboard, playerRank, loading, error, refetch: fetch };
}
