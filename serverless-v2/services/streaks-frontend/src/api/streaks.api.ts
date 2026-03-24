import apiClient from './client';
import type {
  StreakState,
  CheckInResponse,
  CalendarResponse,
  RewardsResponse,
  FreezeInfo,
  MissionsResponse,
  LeaderboardResponse,
  ResponsibleGamingSettings,
  ShareData,
  PlayerHistory,
} from '../types/streaks.types';

/** Fetch the player's current streak state. */
export async function getStreaks(): Promise<StreakState> {
  const { data } = await apiClient.get<StreakState>('/streaks');
  return data;
}

/** Perform a daily login check-in. */
export async function checkIn(): Promise<CheckInResponse> {
  const { data } = await apiClient.post<CheckInResponse>('/streaks/check-in');
  return data;
}

/** Fetch calendar activity data for a given month (YYYY-MM). */
export async function getCalendar(month: string): Promise<CalendarResponse> {
  const { data } = await apiClient.get<CalendarResponse>('/player/streaks/calendar', {
    params: { month },
  });
  return data;
}

/** Fetch the player's reward history. */
export async function getRewards(): Promise<RewardsResponse> {
  const { data } = await apiClient.get<RewardsResponse>('/player/streaks/rewards');
  return data;
}

/** Fetch the player's freeze balance and history. */
export async function getFreezes(): Promise<FreezeInfo> {
  const { data } = await apiClient.get<FreezeInfo>('/player/streaks/freezes');
  return data;
}

/** Fetch today's missions. */
export async function getMissions(): Promise<MissionsResponse> {
  const { data } = await apiClient.get<MissionsResponse>('/missions');
  return data;
}

/** Claim a completed mission. */
export async function claimMission(missionId: string): Promise<{ reward: number; pointsEarnedToday: number }> {
  const { data } = await apiClient.post(`/missions/${missionId}/claim`);
  return data;
}

/** Fetch leaderboard. */
export async function getLeaderboard(type: string = 'combined'): Promise<LeaderboardResponse> {
  const { data } = await apiClient.get<LeaderboardResponse>('/streaks/leaderboard', { params: { type } });
  return data;
}

/** Fetch responsible gaming settings. */
export async function getResponsibleGaming(): Promise<ResponsibleGamingSettings> {
  const { data } = await apiClient.get<ResponsibleGamingSettings>('/player/responsible-gaming');
  return data;
}

/** Update responsible gaming settings. */
export async function updateResponsibleGaming(settings: Partial<ResponsibleGamingSettings>): Promise<void> {
  await apiClient.put('/player/responsible-gaming', settings);
}

/** Self-exclude for N days. */
export async function selfExclude(days: number): Promise<{ selfExcludedUntil: string; message: string }> {
  const { data } = await apiClient.post('/player/responsible-gaming/self-exclude', { days });
  return data;
}

/** Fetch the player's streak data formatted for sharing. */
export async function getShareData(): Promise<ShareData> {
  const { data } = await apiClient.get<ShareData>('/streaks/share');
  return data;
}

/** Admin: Fetch a player's complete history. */
export async function getPlayerHistory(playerId: string): Promise<PlayerHistory> {
  const { data } = await apiClient.get<PlayerHistory>(`/admin/players/${encodeURIComponent(playerId)}`);
  return data;
}
