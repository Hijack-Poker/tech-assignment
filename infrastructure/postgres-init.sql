-- Hijack Poker - Postgres Schema (Fly.io deployment)
-- Converted from MySQL schema for Postgres compatibility

-- Players
CREATE TABLE IF NOT EXISTS players (
  id SERIAL PRIMARY KEY,
  guid VARCHAR(36) NOT NULL UNIQUE,
  username VARCHAR(50) NOT NULL,
  email VARCHAR(100),
  balance DECIMAL(12,2) DEFAULT 0.00,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tables (poker tables)
CREATE TABLE IF NOT EXISTS game_tables (
  id SERIAL PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  table_type VARCHAR(1) DEFAULT 's',
  game_type VARCHAR(20) DEFAULT 'texas',
  max_seats INT DEFAULT 9,
  small_blind DECIMAL(10,2) NOT NULL,
  big_blind DECIMAL(10,2) NOT NULL,
  min_buy_in DECIMAL(10,2) NOT NULL,
  max_buy_in DECIMAL(10,2) NOT NULL,
  status VARCHAR(10) DEFAULT 'active',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Games (hand history)
CREATE TABLE IF NOT EXISTS games (
  id SERIAL PRIMARY KEY,
  table_id INT NOT NULL REFERENCES game_tables(id),
  game_no INT NOT NULL,
  hand_step INT DEFAULT 0,
  dealer_seat INT DEFAULT 0,
  small_blind_seat INT DEFAULT 0,
  big_blind_seat INT DEFAULT 0,
  community_cards TEXT,
  pot DECIMAL(10,2) DEFAULT 0.00,
  side_pots TEXT,
  deck TEXT,
  current_bet DECIMAL(10,2) DEFAULT 0.00,
  last_raise_size DECIMAL(10,2) DEFAULT 0.00,
  winners TEXT,
  move INT DEFAULT 0,
  status VARCHAR(20) DEFAULT 'in_progress',
  started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  completed_at TIMESTAMP NULL,
  UNIQUE (table_id, game_no)
);

-- Game Players (per-hand player state)
CREATE TABLE IF NOT EXISTS game_players (
  id SERIAL PRIMARY KEY,
  game_id INT NOT NULL REFERENCES games(id),
  table_id INT NOT NULL,
  player_id INT NOT NULL REFERENCES players(id),
  seat INT NOT NULL,
  stack DECIMAL(10,2) DEFAULT 0.00,
  bet DECIMAL(10,2) DEFAULT 0.00,
  total_bet DECIMAL(10,2) DEFAULT 0.00,
  status VARCHAR(5) DEFAULT '1',
  action VARCHAR(10) DEFAULT '',
  cards TEXT,
  best_hand TEXT,
  hand_rank VARCHAR(50) DEFAULT '',
  is_winner SMALLINT DEFAULT 0,
  winnings DECIMAL(10,2) DEFAULT 0.00,
  UNIQUE (game_id, seat)
);

-- Ledger (financial transactions)
CREATE TABLE IF NOT EXISTS ledger (
  id SERIAL PRIMARY KEY,
  player_id INT NOT NULL REFERENCES players(id),
  table_id INT,
  game_id INT,
  type VARCHAR(20) NOT NULL,
  amount DECIMAL(12,2) NOT NULL,
  balance_after DECIMAL(12,2) NOT NULL,
  description VARCHAR(255),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Game Stats (aggregate player stats)
CREATE TABLE IF NOT EXISTS game_stats (
  id SERIAL PRIMARY KEY,
  player_id INT NOT NULL REFERENCES players(id),
  table_id INT NOT NULL REFERENCES game_tables(id),
  hands_played INT DEFAULT 0,
  hands_won INT DEFAULT 0,
  total_wagered DECIMAL(12,2) DEFAULT 0.00,
  total_won DECIMAL(12,2) DEFAULT 0.00,
  biggest_pot DECIMAL(10,2) DEFAULT 0.00,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE (player_id, table_id)
);

-- Seed Data
INSERT INTO players (guid, username, email, balance) VALUES
  ('p1-uuid-0001', 'Alice', 'alice@example.com', 1000.00),
  ('p2-uuid-0002', 'Bob', 'bob@example.com', 1500.00),
  ('p3-uuid-0003', 'Charlie', 'charlie@example.com', 800.00),
  ('p4-uuid-0004', 'Diana', 'diana@example.com', 2000.00),
  ('p5-uuid-0005', 'Eve', 'eve@example.com', 1200.00),
  ('p6-uuid-0006', 'Frank', 'frank@example.com', 900.00)
ON CONFLICT (guid) DO NOTHING;

INSERT INTO game_tables (name, table_type, game_type, max_seats, small_blind, big_blind, min_buy_in, max_buy_in) VALUES
  ('Starter Table', 's', 'texas', 6, 1.00, 2.00, 40.00, 200.00),
  ('High Stakes', 's', 'texas', 9, 5.00, 10.00, 200.00, 1000.00)
ON CONFLICT DO NOTHING;
