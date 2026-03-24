// ─── Domain Models (DynamoDB shapes) ─────────────────────────────────

export interface Player {
  playerId: string;
  username?: string;
  points: number;
  tier: string;
  createdAt: string;
  updatedAt: string;
}

export interface Transaction {
  playerId: string;
  timestamp: number;
  points: number;
  reason: string;
  balance?: number;
}

// ─── Request DTOs ────────────────────────────────────────────────────

export interface AwardPointsBody {
  playerId: string;
  points: number;
  reason: string;
}

// ─── Response DTOs ───────────────────────────────────────────────────

export interface AwardPointsResponse {
  playerId: string;
  newBalance: number;
  tier: string;
  transaction: Transaction;
}

export interface LeaderboardEntry {
  playerId: string;
  username: string;
  points: number;
  tier: string;
  rank: number;
}

export interface LeaderboardResponse {
  leaderboard: LeaderboardEntry[];
}

export interface PlayerRewardsResponse {
  playerId: string;
  tier: string;
  points: number;
  nextTierAt: number | null;
  recentTransactions: Transaction[];
}

export interface PlayerHistoryResponse {
  transactions: Transaction[];
  total: number;
}
