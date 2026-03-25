// ─── Tier Types ──────────────────────────────────────────────────────

/** Tier stored as number (1-4) in DynamoDB */
export type TierNumber = 1 | 2 | 3 | 4;

/** Tier name used in API responses and frontend display */
export type TierName = 'Bronze' | 'Silver' | 'Gold' | 'Platinum';

export interface TierDefinition {
  number: TierNumber;
  name: TierName;
  minPoints: number;
  multiplier: number;
}

// ─── Notification Types ──────────────────────────────────────────────

export type NotificationType = 'tier_upgrade' | 'tier_downgrade' | 'milestone';

// ─── Transaction Types ───────────────────────────────────────────────

export type TransactionType = 'gameplay' | 'adjustment' | 'bonus';

// ─── DynamoDB Models (match table schemas exactly) ───────────────────

/** rewards-players table */
export interface PlayerRecord {
  playerId: string;
  username: string;
  tier: TierNumber;
  points: number;
  totalEarned: number;
  handsPlayed: number;
  tournamentsPlayed: number;
  createdAt: string;
  updatedAt: string;
}

/** rewards-transactions table */
export interface TransactionRecord {
  playerId: string;
  timestamp: number;
  type: TransactionType;
  basePoints: number;
  multiplier: number;
  earnedPoints: number;
  tableId?: number;
  tableStakes?: string;
  monthKey: string;
  createdAt: string;
  reason?: string;
  balanceAfter?: number;
}

/** rewards-notifications table */
export interface NotificationRecord {
  playerId: string;
  notificationId: string;
  type: NotificationType;
  title: string;
  description: string;
  dismissed: boolean;
  createdAt: string;
}

// ─── Request DTOs ────────────────────────────────────────────────────

/** POST /api/v1/points/award — called by game processor */
export interface AwardPointsBody {
  playerId: string;
  tableId: number;
  tableStakes: string;
  bigBlind: number;
  handId: string;
}

/** POST /admin/points/adjust — manual credit/debit */
export interface AdminAdjustPointsBody {
  playerId: string;
  points: number;
  reason: string;
}

/** POST /admin/tier/override — manual tier set */
export interface AdminTierOverrideBody {
  playerId: string;
  tier: TierNumber;
  expiry: string;
}

// ─── API Response DTOs (tier as TierName for display) ────────────────

/** POST /api/v1/points/award response */
export interface AwardPointsResponse {
  playerId: string;
  earnedPoints: number;
  newPoints: number;
  newTotalEarned: number;
  tier: TierName;
  transaction: TransactionResponse;
}

/** Single transaction in API responses */
export interface TransactionResponse {
  timestamp: number;
  type: TransactionType;
  basePoints: number;
  multiplier: number;
  earnedPoints: number;
  tableId?: number;
  tableStakes?: string;
  reason?: string;
  balanceAfter?: number;
}

/** GET /api/v1/player/rewards response */
export interface PlayerRewardsResponse {
  playerId: string;
  tier: TierName;
  points: number;
  totalEarned: number;
  nextTierAt: number | null;
  nextTierName: TierName | null;
  recentTransactions: TransactionResponse[];
}

/** GET /api/v1/player/rewards/history response */
export interface PlayerHistoryResponse {
  transactions: TransactionResponse[];
  total: number;
  limit: number;
  cursor: string | null;
}

/** Single entry in leaderboard API responses */
export interface LeaderboardEntry {
  rank: number;
  playerId: string;
  displayName: string;
  tier: TierName;
  points: number;
}

/** GET /api/v1/leaderboard response */
export interface LeaderboardResponse {
  leaderboard: LeaderboardEntry[];
  playerRank?: number;
}

/** Single notification in API responses */
export interface NotificationResponse {
  notificationId: string;
  type: NotificationType;
  title: string;
  description: string;
  dismissed: boolean;
  createdAt: string;
}

/** GET /api/v1/player/notifications response */
export interface NotificationsResponse {
  notifications: NotificationResponse[];
  unreadCount: number;
}

/** GET /admin/players/:playerId/rewards response */
export interface AdminPlayerRewardsResponse extends PlayerRewardsResponse {
  createdAt: string;
  updatedAt: string;
}

/** GET /admin/leaderboard response — same entries but with playerId visible */
export interface AdminLeaderboardEntry extends LeaderboardEntry {
  playerId: string;
}
