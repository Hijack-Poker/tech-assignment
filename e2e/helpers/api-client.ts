import { API_BASE_URL, TABLE_ID, STEP_NAMES, BETTING_STEPS } from './constants.js';

export interface GameState {
  handStep: number;
  stepName: string;
  gameNo: number;
  pot: number;
  communityCards: string[];
  dealerSeat: number;
  smallBlindSeat: number;
  bigBlindSeat: number;
  move: number;
}

export interface Player {
  playerId: number;
  username: string;
  seat: number;
  stack: number;
  bet: number;
  totalBet: number;
  status: string;
  action: string | null;
  cards: string[];
  handRank: string | null;
  winnings: number;
}

export interface TableState {
  game: GameState;
  players: Player[];
}

export interface ProcessResult {
  success: boolean;
  result: {
    stepName: string;
    [key: string]: unknown;
  };
}

export interface SeatInfo {
  seat: number;
  available: boolean;
  hasPlayer: boolean;
  username: string | null;
}

export interface HealthResponse {
  service: string;
  status: string;
  timestamp: string;
}

export class PokerApiClient {
  constructor(private baseUrl: string = API_BASE_URL) {}

  private async get<T>(path: string): Promise<{ status: number; body: T }> {
    const res = await fetch(`${this.baseUrl}${path}`);
    const body = await res.json() as T;
    return { status: res.status, body };
  }

  private async post<T>(path: string, data?: unknown): Promise<{ status: number; body: T }> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: data ? JSON.stringify(data) : undefined,
    });
    const body = await res.json() as T;
    return { status: res.status, body };
  }

  async health(): Promise<{ status: number; body: HealthResponse }> {
    return this.get<HealthResponse>('/health');
  }

  async getTable(tableId: number = TABLE_ID): Promise<{ status: number; body: TableState }> {
    return this.get<TableState>(`/table/${tableId}`);
  }

  async getSeats(tableId: number = TABLE_ID): Promise<{ status: number; body: { seats: SeatInfo[] } }> {
    return this.get<{ seats: SeatInfo[] }>(`/table/${tableId}/seats`);
  }

  async process(tableId: number = TABLE_ID): Promise<{ status: number; body: ProcessResult }> {
    return this.post<ProcessResult>('/process', { tableId });
  }

  async join(
    tableId: number = TABLE_ID,
    playerName: string = 'TestPlayer',
    seat?: number,
  ): Promise<{ status: number; body: { seat: number; playerId: number } & { error?: string } }> {
    return this.post('/join', { tableId, playerName, seat });
  }

  async leave(tableId: number = TABLE_ID): Promise<{ status: number; body: { success: boolean } }> {
    return this.post('/leave', { tableId });
  }

  async action(
    tableId: number = TABLE_ID,
    seat: number,
    action: string,
    amount?: number,
  ): Promise<{ status: number; body: { success?: boolean; error?: string; game?: GameState; players?: Player[] } }> {
    return this.post('/action', { tableId, seat, action, amount });
  }

  async playFullHand(tableId: number = TABLE_ID): Promise<TableState> {
    for (let i = 0; i < 16; i++) {
      await this.process(tableId);
    }
    const { body } = await this.getTable(tableId);
    return body;
  }

  async advanceUntilStep(
    tableId: number = TABLE_ID,
    targetStep: string,
    maxIterations: number = 32,
  ): Promise<TableState> {
    for (let i = 0; i < maxIterations; i++) {
      const { body: table } = await this.getTable(tableId);
      if (table.game.stepName === targetStep) return table;
      await this.process(tableId);
    }
    const { body } = await this.getTable(tableId);
    if (body.game.stepName !== targetStep) {
      throw new Error(`Did not reach step ${targetStep} within ${maxIterations} iterations (at ${body.game.stepName})`);
    }
    return body;
  }

  /**
   * Advance until game.move equals the given seat during a betting round.
   * Returns the table state when paused at the human's turn.
   */
  async advanceUntilHumanTurn(
    tableId: number = TABLE_ID,
    humanSeat: number,
    maxIterations: number = 32,
  ): Promise<TableState> {
    for (let i = 0; i < maxIterations; i++) {
      const { body: table } = await this.getTable(tableId);
      if (
        BETTING_STEPS.includes(table.game.stepName as typeof BETTING_STEPS[number]) &&
        table.game.move === humanSeat
      ) {
        return table;
      }
      await this.process(tableId);
    }
    const { body } = await this.getTable(tableId);
    if (
      !BETTING_STEPS.includes(body.game.stepName as typeof BETTING_STEPS[number]) ||
      body.game.move !== humanSeat
    ) {
      throw new Error(
        `Did not reach human turn at seat ${humanSeat} within ${maxIterations} iterations ` +
        `(at step ${body.game.stepName}, move=${body.game.move})`,
      );
    }
    return body;
  }

  /** Total chip count across all players (stacks + bets + pot) */
  totalChips(table: TableState): number {
    const playerChips = table.players.reduce(
      (sum, p) => sum + p.stack + p.bet,
      0,
    );
    return playerChips + table.game.pot;
  }
}
