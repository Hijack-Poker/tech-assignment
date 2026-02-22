-- Hijack Poker - Simplified Schema for Tech Assignment
-- This is a minimal subset of the production schema for local development.

CREATE DATABASE IF NOT EXISTS hijack_poker;
USE hijack_poker;

-- ─── Players ───────────────────────────────────────────────────────────

CREATE TABLE players (
  id INT AUTO_INCREMENT PRIMARY KEY,
  guid VARCHAR(36) NOT NULL UNIQUE,
  username VARCHAR(50) NOT NULL,
  email VARCHAR(100),
  balance DECIMAL(12,2) DEFAULT 0.00,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- ─── Tables (poker tables) ────────────────────────────────────────────

CREATE TABLE game_tables (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  table_type ENUM('s', 't', 'm') DEFAULT 's' COMMENT 's=cash, t=SNG, m=MTT',
  game_type VARCHAR(20) DEFAULT 'texas',
  max_seats INT DEFAULT 9,
  small_blind DECIMAL(10,2) NOT NULL,
  big_blind DECIMAL(10,2) NOT NULL,
  min_buy_in DECIMAL(10,2) NOT NULL,
  max_buy_in DECIMAL(10,2) NOT NULL,
  status ENUM('active', 'inactive', 'closed') DEFAULT 'active',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- ─── Games (hand history) ─────────────────────────────────────────────

CREATE TABLE games (
  id INT AUTO_INCREMENT PRIMARY KEY,
  table_id INT NOT NULL,
  game_no INT NOT NULL COMMENT 'Incrementing hand number per table',
  hand_step INT DEFAULT 0 COMMENT 'Current GAME_HAND step (0-16)',
  dealer_seat INT DEFAULT 0,
  small_blind_seat INT DEFAULT 0,
  big_blind_seat INT DEFAULT 0,
  community_cards JSON COMMENT '["AH","KD","QS","JC","10H"]',
  pot DECIMAL(10,2) DEFAULT 0.00,
  side_pots JSON COMMENT '[{"amount":100,"eligible":[1,3,5]}]',
  deck JSON COMMENT 'Remaining cards in shuffled deck',
  current_bet DECIMAL(10,2) DEFAULT 0.00 COMMENT 'Current bet to match',
  winners JSON COMMENT '[{"seat":1,"playerId":3}]',
  move INT DEFAULT 0 COMMENT 'Seat whose turn it is',
  status ENUM('in_progress', 'completed', 'cancelled') DEFAULT 'in_progress',
  started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  completed_at TIMESTAMP NULL,
  FOREIGN KEY (table_id) REFERENCES game_tables(id),
  UNIQUE KEY unique_table_game (table_id, game_no)
);

-- ─── Game Players (per-hand player state) ─────────────────────────────

CREATE TABLE game_players (
  id INT AUTO_INCREMENT PRIMARY KEY,
  game_id INT NOT NULL,
  table_id INT NOT NULL,
  player_id INT NOT NULL,
  seat INT NOT NULL,
  stack DECIMAL(10,2) DEFAULT 0.00,
  bet DECIMAL(10,2) DEFAULT 0.00,
  total_bet DECIMAL(10,2) DEFAULT 0.00,
  status VARCHAR(5) DEFAULT '1' COMMENT 'PLAYER_STATUS constant',
  action VARCHAR(10) DEFAULT '' COMMENT 'Last action (call/check/bet/raise/fold/allin)',
  cards JSON COMMENT '["AH","KD"]',
  hand_rank VARCHAR(50) DEFAULT '' COMMENT 'Best hand description',
  winnings DECIMAL(10,2) DEFAULT 0.00,
  FOREIGN KEY (game_id) REFERENCES games(id),
  FOREIGN KEY (player_id) REFERENCES players(id),
  UNIQUE KEY unique_game_seat (game_id, seat)
);

-- ─── Ledger (financial transactions) ──────────────────────────────────

CREATE TABLE ledger (
  id INT AUTO_INCREMENT PRIMARY KEY,
  player_id INT NOT NULL,
  table_id INT,
  game_id INT,
  type VARCHAR(20) NOT NULL COMMENT 'deposit/withdrawal/buy_in/cash_out/winnings',
  amount DECIMAL(12,2) NOT NULL,
  balance_after DECIMAL(12,2) NOT NULL,
  description VARCHAR(255),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (player_id) REFERENCES players(id)
);

-- ─── Game Stats (aggregate player stats) ──────────────────────────────

CREATE TABLE game_stats (
  id INT AUTO_INCREMENT PRIMARY KEY,
  player_id INT NOT NULL,
  table_id INT NOT NULL,
  hands_played INT DEFAULT 0,
  hands_won INT DEFAULT 0,
  total_wagered DECIMAL(12,2) DEFAULT 0.00,
  total_won DECIMAL(12,2) DEFAULT 0.00,
  biggest_pot DECIMAL(10,2) DEFAULT 0.00,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (player_id) REFERENCES players(id),
  FOREIGN KEY (table_id) REFERENCES game_tables(id),
  UNIQUE KEY unique_player_table (player_id, table_id)
);

-- ─── Seed Data ────────────────────────────────────────────────────────

-- Sample players
INSERT INTO players (guid, username, email, balance) VALUES
  ('p1-uuid-0001', 'Alice', 'alice@example.com', 1000.00),
  ('p2-uuid-0002', 'Bob', 'bob@example.com', 1500.00),
  ('p3-uuid-0003', 'Charlie', 'charlie@example.com', 800.00),
  ('p4-uuid-0004', 'Diana', 'diana@example.com', 2000.00),
  ('p5-uuid-0005', 'Eve', 'eve@example.com', 1200.00),
  ('p6-uuid-0006', 'Frank', 'frank@example.com', 900.00);

-- Sample table
INSERT INTO game_tables (name, table_type, game_type, max_seats, small_blind, big_blind, min_buy_in, max_buy_in) VALUES
  ('Starter Table', 's', 'texas', 6, 1.00, 2.00, 40.00, 200.00),
  ('High Stakes', 's', 'texas', 9, 5.00, 10.00, 200.00, 1000.00);
