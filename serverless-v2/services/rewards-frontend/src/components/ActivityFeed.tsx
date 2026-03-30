import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Box,
  Button,
  Divider,
  Paper,
  Typography,
} from '@mui/material';
import type { TransactionResponse, PlayerHistoryResponse } from '@shared/types/rewards';
import apiClient from '../api/client';

interface ActivityFeedProps {
  newTransaction?: TransactionResponse;
}

function formatTimeAgo(timestamp: number): string {
  const seconds = Math.floor((Date.now() - timestamp) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

function ActivityFeed({ newTransaction }: ActivityFeedProps) {
  const [transactions, setTransactions] = useState<TransactionResponse[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const initialFetchDone = useRef(false);

  const fetchHistory = useCallback(async (nextCursor?: string) => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = { limit: 20 };
      if (nextCursor) params.cursor = nextCursor;
      const { data } = await apiClient.get<PlayerHistoryResponse>('/player/history', { params });
      setTransactions((prev) => nextCursor ? [...prev, ...data.transactions] : data.transactions);
      setCursor(data.cursor);
      setHasMore(data.cursor !== null);
    } catch (err) {
      console.error('Failed to fetch history:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!initialFetchDone.current) {
      initialFetchDone.current = true;
      fetchHistory();
    }
  }, [fetchHistory]);

  // Prepend new transactions from award responses
  const prevTxRef = useRef<TransactionResponse | undefined>();
  useEffect(() => {
    if (newTransaction && newTransaction !== prevTxRef.current) {
      prevTxRef.current = newTransaction;
      setTransactions((prev) => [newTransaction, ...prev]);
    }
  }, [newTransaction]);

  return (
    <Paper sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Typography variant="h6" fontWeight={700} mb={2}>
        Activity Feed
      </Typography>

      <Box sx={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column', gap: 0, pr: 1 }}>
        {transactions.map((tx, i) => (
          <Box key={`${tx.timestamp}-${i}`}>
            <Box sx={{ py: 1.5 }}>
              <Box display="flex" justifyContent="space-between" alignItems="baseline">
                <Typography
                  variant="body2"
                  fontWeight={600}
                  sx={{ color: '#4caf50' }}
                >
                  +{tx.earnedPoints} pts
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatTimeAgo(tx.timestamp)}
                </Typography>
              </Box>
              <Typography variant="caption" color="text.secondary" display="block">
                {tx.tableStakes ? `${tx.tableStakes} hand` : tx.reason ?? tx.type}
                {tx.multiplier > 1 && ` (${tx.multiplier}x)`}
              </Typography>
              {tx.balanceAfter != null && (
                <Typography variant="caption" color="text.secondary">
                  Balance: {tx.balanceAfter.toLocaleString()}
                </Typography>
              )}
            </Box>
            {i < transactions.length - 1 && <Divider />}
          </Box>
        ))}

        {transactions.length === 0 && !loading && (
          <Typography variant="body2" color="text.secondary" textAlign="center" mt={4}>
            No transactions yet. Play a hand!
          </Typography>
        )}
      </Box>

      {hasMore && transactions.length > 0 && (
        <Button
          size="small"
          onClick={() => cursor && fetchHistory(cursor)}
          disabled={loading}
          sx={{ mt: 2 }}
        >
          {loading ? 'Loading...' : 'Load More'}
        </Button>
      )}
    </Paper>
  );
}

export default ActivityFeed;
