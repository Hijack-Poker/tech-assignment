import { useState, useEffect, useCallback } from 'react';
import { getMissions, claimMission as claimMissionApi } from '../api/streaks.api';
import type { Mission } from '../types/streaks.types';

export function useMissions() {
  const [missions, setMissions] = useState<Mission[]>([]);
  const [pointsEarnedToday, setPointsEarnedToday] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchMissions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getMissions();
      setMissions(data.missions);
      setPointsEarnedToday(data.pointsEarnedToday);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load missions');
    } finally {
      setLoading(false);
    }
  }, []);

  const claimMission = useCallback(async (missionId: string) => {
    const result = await claimMissionApi(missionId);
    setPointsEarnedToday(result.pointsEarnedToday);
    // Update local state
    setMissions((prev) =>
      prev.map((m) => (m.missionId === missionId ? { ...m, status: 'claimed' as const } : m))
    );
    return result;
  }, []);

  useEffect(() => { fetchMissions(); }, [fetchMissions]);

  return { missions, pointsEarnedToday, loading, error, refetch: fetchMissions, claimMission };
}
