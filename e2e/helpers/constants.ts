export const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:3030';
export const HAND_VIEWER_URL = process.env.HAND_VIEWER_URL || 'http://localhost:8080';
export const WEBGL_URL = process.env.WEBGL_URL || 'http://localhost:8090';

export const TABLE_ID = 1;

export const STEP_NAMES = [
  'GAME_PREP',
  'SETUP_DEALER',
  'SETUP_SMALL_BLIND',
  'SETUP_BIG_BLIND',
  'DEAL_CARDS',
  'PRE_FLOP_BETTING_ROUND',
  'DEAL_FLOP',
  'FLOP_BETTING_ROUND',
  'DEAL_TURN',
  'TURN_BETTING_ROUND',
  'DEAL_RIVER',
  'RIVER_BETTING_ROUND',
  'AFTER_RIVER_BETTING_ROUND',
  'FIND_WINNERS',
  'PAY_WINNERS',
  'RECORD_STATS_AND_NEW_HAND',
] as const;

export const BETTING_STEPS = [
  'PRE_FLOP_BETTING_ROUND',
  'FLOP_BETTING_ROUND',
  'TURN_BETTING_ROUND',
  'RIVER_BETTING_ROUND',
] as const;

export const TOTAL_STEPS_PER_HAND = 16;
