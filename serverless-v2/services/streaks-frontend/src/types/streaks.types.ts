/** Next milestone the player is approaching. */
export interface NextMilestone {
  days: number;
  reward: number;
  daysRemaining: number;
}

/** GET /api/v1/streaks response. */
export interface StreakState {
  loginStreak: number;
  playStreak: number;
  bestLoginStreak: number;
  bestPlayStreak: number;
  freezesAvailable: number;
  nextLoginMilestone: NextMilestone | null;
  nextPlayMilestone: NextMilestone | null;
  lastLoginDate: string;
  lastPlayDate: string;
  comboActive: boolean;
  comboMultiplier: number;
}

/** Milestone reward returned inline with check-in. */
export interface MilestoneReward {
  playerId: string;
  rewardId: string;
  type: 'login_milestone' | 'play_milestone';
  milestone: number;
  points: number;
  streakCount: number;
  createdAt: string;
}

/** POST /api/v1/streaks/check-in response. */
export interface CheckInResponse {
  playerId: string;
  loginStreak: number;
  bestLoginStreak: number;
  todayCheckedIn: boolean;
  milestone: MilestoneReward | null;
}

/** Activity type for a single calendar day. */
export type ActivityType = 'freeze' | 'played' | 'login_only' | 'streak_broken' | 'none';

/** Single day entry in the calendar response. */
export interface CalendarDay {
  date: string;
  activity: ActivityType;
  loginStreak: number;
  playStreak: number;
}

/** GET /api/v1/player/streaks/calendar response. */
export interface CalendarResponse {
  month: string;
  days: CalendarDay[];
}

/** Single reward entry from the rewards endpoint. */
export interface Reward {
  date: string;
  milestone: number;
  type: string;
  points: number;
}

/** GET /api/v1/player/streaks/rewards response. */
export interface RewardsResponse {
  rewards: Reward[];
}

/** Single freeze history entry. */
export interface FreezeHistoryEntry {
  date: string;
  source: string;
}

/** GET /api/v1/player/streaks/freezes response. */
export interface FreezeInfo {
  freezesAvailable: number;
  freezesUsedThisMonth: number;
  history: FreezeHistoryEntry[];
}

/** Standard API error response. */
export interface ApiError {
  error: string;
  message?: string;
}

/** Single mission entry. */
export interface Mission {
  missionId: string;
  id: string;
  title: string;
  description: string;
  target: number;
  progress: number;
  reward: number;
  type: string;
  status: 'active' | 'completed' | 'claimed';
}

/** GET /api/v1/missions response. */
export interface MissionsResponse {
  date: string;
  missions: Mission[];
  pointsEarnedToday: number;
}

/** Leaderboard entry. */
export interface LeaderboardEntry {
  rank: number;
  playerId: string;
  displayName: string;
  loginStreak: number;
  playStreak: number;
  score: number;
  tier: 'bronze' | 'silver' | 'gold' | 'platinum';
}

/** GET /api/v1/streaks/leaderboard response. */
export interface LeaderboardResponse {
  leaderboard: LeaderboardEntry[];
  playerRank: LeaderboardEntry | null;
}

/** Responsible gaming settings. */
export interface ResponsibleGamingSettings {
  sessionLimitMinutes: number | null;
  dailyHandLimit: number | null;
  selfExcludedUntil: string | null;
  reminderEnabled: boolean;
  coolOffEnabled: boolean;
}

/** Admin player history response. */
export interface PlayerProfile {
  playerId: string;
  displayName: string;
  loginStreak: number;
  playStreak: number;
  bestLoginStreak: number;
  bestPlayStreak: number;
  freezesAvailable: number;
  lastLoginDate: string;
  lastPlayDate: string;
  selfExcludedUntil: string | null;
  updatedAt: string;
}

export interface ActivityRecord {
  date: string;
  loggedIn: boolean;
  played: boolean;
  freezeUsed: boolean;
  streakBroken: boolean;
  loginStreakAtDay: number;
  playStreakAtDay: number;
}

export interface RewardRecord {
  rewardId: string;
  type: string;
  milestone: number;
  points: number;
  createdAt: string;
}

export interface PlayerHistory {
  player: PlayerProfile;
  activity: ActivityRecord[];
  rewards: RewardRecord[];
  freezeHistory: FreezeHistoryEntry[];
}

/** VIP tier type. */
export type VipTier = 'bronze' | 'silver' | 'gold' | 'platinum';

/** GET /api/v1/streaks/share response. */
export interface ShareData {
  playerName: string;
  loginStreak: number;
  playStreak: number;
  bestLoginStreak: number;
  bestPlayStreak: number;
  tier: VipTier;
  totalRewards: number;
  shareText: string;
}
