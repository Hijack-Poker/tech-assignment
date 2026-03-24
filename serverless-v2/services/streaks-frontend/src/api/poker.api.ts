import axios from 'axios';
import type { TableState, ProcessResult } from '../types/poker.types';

const HOLDEM_URL = import.meta.env.VITE_HOLDEM_API_URL || 'http://localhost:3030';

const pokerClient = axios.create({
  baseURL: HOLDEM_URL,
  headers: { 'Content-Type': 'application/json' },
});

export async function getTable(tableId: number): Promise<TableState> {
  const { data } = await pokerClient.get<TableState>(`/table/${tableId}`);
  return data;
}

export async function processStep(
  tableId: number,
  action?: { seat: number; action: string; amount?: number }
): Promise<ProcessResult> {
  const body: Record<string, unknown> = { tableId };
  if (action) {
    body.seat = action.seat;
    body.action = action.action;
    if (action.amount !== undefined) body.amount = action.amount;
  }
  const { data } = await pokerClient.post<ProcessResult>('/process', body);
  return data;
}

export async function resetTable(tableId: number): Promise<ProcessResult> {
  const { data } = await pokerClient.post<ProcessResult>('/reset', { tableId });
  return data;
}

export async function healthCheck(): Promise<boolean> {
  try {
    await pokerClient.get('/health');
    return true;
  } catch {
    return false;
  }
}

/**
 * Notify the streaks API that a player completed a hand.
 * This updates their play streak.
 */
const STREAKS_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

const streaksClient = axios.create({
  baseURL: STREAKS_URL,
  headers: { 'Content-Type': 'application/json' },
});

export async function notifyHandCompleted(
  playerId: string,
  tableId: number,
  handId: string,
): Promise<{ streakUpdated: boolean }> {
  try {
    const { data } = await streaksClient.post('/internal/streaks/hand-completed', {
      playerId,
      tableId,
      handId,
      completedAt: new Date().toISOString(),
    });
    // Backend returns alreadyPlayedToday: true if streak was already counted
    return { streakUpdated: !data.alreadyPlayedToday };
  } catch {
    // non-critical — don't block the game if streaks API is down
    console.warn('Failed to notify streaks API of hand completion');
    return { streakUpdated: false };
  }
}
