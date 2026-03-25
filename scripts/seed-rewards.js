#!/usr/bin/env node

/**
 * Seed rewards DynamoDB tables with sample data.
 *
 * Creates 50 players across all tiers with point balances and
 * recent transactions. Run after `docker compose --profile rewards up`.
 *
 * Usage: node scripts/seed-rewards.js
 */

'use strict';

const { DynamoDBClient } = require('@aws-sdk/client-dynamodb');
const { DynamoDBDocumentClient, PutCommand } = require('@aws-sdk/lib-dynamodb');
const { redisClient } = require('/shared/config/redis');

const ENDPOINT = process.env.DYNAMODB_ENDPOINT || 'http://localhost:8000';
const REGION = process.env.AWS_REGION || 'us-east-1';

const client = new DynamoDBClient({
  region: REGION,
  endpoint: ENDPOINT,
  credentials: { accessKeyId: 'local', secretAccessKey: 'local' },
});
const docClient = DynamoDBDocumentClient.from(client, {
  marshallOptions: { removeUndefinedValues: true },
});

const TIERS = [
  { number: 1, name: 'Bronze', minPoints: 0 },
  { number: 2, name: 'Silver', minPoints: 500 },
  { number: 3, name: 'Gold', minPoints: 2000 },
  { number: 4, name: 'Platinum', minPoints: 10000 },
];

const NAMES = [
  'Ace_High', 'RiverRat', 'PocketKings', 'BluffMaster', 'NutFlush',
  'CheckRaise', 'FullBoat', 'SetMiner', 'OverBet', 'ValueTown',
  'FloatPlay', 'TripleBarel', 'SqueezePot', 'ColdCall', 'ThreeWay',
  'HeadsUp', 'UTG_Open', 'BtnSteal', 'CutOff', 'SmallBlind',
  'BigBlind', 'Straddle', 'RunItTwice', 'AllInPre', 'SuckOut',
  'BadBeat', 'CoinFlip', 'Domination', 'DrawingDead', 'Freeroll',
  'Satellite', 'BountyHunt', 'KnockOut', 'FinalTable', 'BubbleBoy',
  'ChipLeader', 'ShortStack', 'BigStack', 'MiddleStack', 'DeepRun',
  'GrindMode', 'NitReg', 'LAG_Life', 'TAG_Player', 'ManiacMode',
  'RockSolid', 'LooseCall', 'TightFold', 'MixedGame', 'ActionJunkie',
];

const REASONS = [
  'hand_played', 'hand_won', 'tournament_entry', 'daily_login',
  'tournament_win', 'referral', 'hand_played', 'hand_won',
];

function getTier(points) {
  for (let i = TIERS.length - 1; i >= 0; i--) {
    if (points >= TIERS[i].minPoints) return TIERS[i].number;
  }
  return 1;
}

function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

async function seed() {
  await redisClient.connect();
  const monthKey = new Date().toISOString().slice(0, 7);

  console.log(`Seeding rewards data to ${ENDPOINT} (month: ${monthKey})...`);
  let playerCount = 0;
  let txCount = 0;

  for (let i = 0; i < 50; i++) {
    const playerId = `player-${String(i + 1).padStart(3, '0')}`;
    const points = randomInt(0, 20000);
    const tier = getTier(points);

    // Insert player profile
    await docClient.send(
      new PutCommand({
        TableName: 'rewards-players',
        Item: {
          playerId,
          username: NAMES[i],
          points,
          tier,
          totalEarned: points + randomInt(0, 5000),
          handsPlayed: randomInt(100, 10000),
          tournamentsPlayed: randomInt(0, 200),
          createdAt: new Date(Date.now() - randomInt(30, 365) * 86400000).toISOString(),
          updatedAt: new Date().toISOString(),
        },
      })
    );
    playerCount++;

    // Populate Redis leaderboard
    await redisClient.zadd(`leaderboard:${monthKey}`, points, playerId);

    // Insert 5-15 recent transactions
    const txCount_ = randomInt(5, 15);
    for (let j = 0; j < txCount_; j++) {
      const earnedPoints = randomInt(1, 10);
      const reason = REASONS[randomInt(0, REASONS.length - 1)];

      await docClient.send(
        new PutCommand({
          TableName: 'rewards-transactions',
          Item: {
            playerId,
            timestamp: Date.now() - randomInt(0, 30 * 86400000) + j, // ensure unique
            type: 'gameplay',
            basePoints: earnedPoints,
            multiplier: 1.0,
            earnedPoints,
            reason,
            monthKey,
            balanceAfter: points - randomInt(0, earnedPoints * 5),
            createdAt: new Date(Date.now() - randomInt(0, 30 * 86400000)).toISOString(),
          },
        })
      );
      txCount++;
    }
  }

  await redisClient.quit();
  console.log(`Seeded ${playerCount} players and ${txCount} transactions.`);
  console.log('Done!');
}

seed().catch((err) => {
  console.error('Seed failed:', err.message);
  process.exit(1);
});
