/**
 * Data model types for the Streaks service.
 * Maps to four DynamoDB tables: streaks-players, streaks-activity,
 * streaks-rewards, streaks-freeze-history.
 */

/** streaks-players table — PK: playerId */
export interface StreakPlayer {
  playerId: string;
  username?: string;
  currentStreak: number;
  longestStreak: number;
  totalCheckIns: number;
  lastCheckIn: string | null;
  createdAt: string;
  updatedAt: string;
}

/** streaks-activity table — PK: playerId, SK: date */
export interface StreakActivity {
  playerId: string;
  date: string; // YYYY-MM-DD
  checkedIn: boolean;
  timestamp: string; // ISO-8601
}

/** streaks-rewards table — PK: playerId, SK: rewardId */
export interface StreakReward {
  playerId: string;
  rewardId: string;
  milestoneDays: number;
  rewardType: 'login' | 'play';
  amount: number;
  claimedAt: string; // ISO-8601
}

/** streaks-freeze-history table — PK: playerId, SK: date */
export interface StreakFreeze {
  playerId: string;
  date: string; // YYYY-MM-DD
  reason: string;
  streakPreserved: number;
  createdAt: string; // ISO-8601
}

/** Milestone definition from constants */
export interface Milestone {
  days: number;
  loginReward: number;
  playReward: number;
  description: string;
}

/** Table name constants */
export const TABLE_NAMES = {
  PLAYERS: 'streaks-players',
  ACTIVITY: 'streaks-activity',
  REWARDS: 'streaks-rewards',
  FREEZE_HISTORY: 'streaks-freeze-history',
} as const;
