'use strict';

/**
 * Player Streaks — streaks-players table.
 * PK: playerId
 */
export interface PlayerStreak {
  playerId: string;
  loginStreak: number;
  playStreak: number;
  bestLoginStreak: number;
  bestPlayStreak: number;
  lastLoginDate: string;    // YYYY-MM-DD
  lastPlayDate: string;     // YYYY-MM-DD
  freezesAvailable: number;
  freezesUsedThisMonth: number;
  lastFreezeGrantDate: string; // YYYY-MM
  updatedAt: string;        // ISO 8601
}

/**
 * Daily Activity — streaks-activity table.
 * PK: playerId, SK: date
 */
export interface DailyActivity {
  playerId: string;
  date: string;             // YYYY-MM-DD (sort key)
  loggedIn: boolean;
  played: boolean;
  freezeUsed: boolean;
  streakBroken: boolean;
  loginStreakAtDay: number;
  playStreakAtDay: number;
}

/**
 * Streak Rewards — streaks-rewards table.
 * PK: playerId, SK: rewardId
 */
export interface StreakReward {
  playerId: string;
  rewardId: string;         // ULID or UUID (sort key)
  type: 'login_milestone' | 'play_milestone';
  milestone: number;        // 3, 7, 14, 30, 60, 90
  points: number;
  streakCount: number;
  createdAt: string;        // ISO 8601
}

/**
 * Freeze History — streaks-freeze-history table.
 * PK: playerId, SK: date
 */
export interface FreezeHistory {
  playerId: string;
  date: string;             // YYYY-MM-DD (sort key)
  source: 'free_monthly' | 'purchased';
}
