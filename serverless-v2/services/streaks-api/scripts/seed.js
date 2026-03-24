'use strict';

const { DynamoDBClient } = require('@aws-sdk/client-dynamodb');
const { DynamoDBDocumentClient, BatchWriteCommand } = require('@aws-sdk/lib-dynamodb');
const crypto = require('crypto');

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
const ENDPOINT = process.env.DYNAMODB_ENDPOINT || 'http://localhost:8000';
const REGION = process.env.AWS_REGION || 'us-east-1';

const PLAYERS_TABLE = process.env.STREAKS_PLAYERS_TABLE || 'streaks-players';
const ACTIVITY_TABLE = process.env.STREAKS_ACTIVITY_TABLE || 'streaks-activity';
const REWARDS_TABLE = process.env.STREAKS_REWARDS_TABLE || 'streaks-rewards';
const FREEZE_TABLE = process.env.STREAKS_FREEZE_HISTORY_TABLE || 'streaks-freeze-history';

// Player ID matching the stubbed JWT token used in tests / manual testing
const PLAYER_ID = 'player-42';

// Milestone definitions (mirrors src/config/constants.js)
const MILESTONES = [
  { days: 3, loginReward: 50, playReward: 100 },
  { days: 7, loginReward: 150, playReward: 300 },
  { days: 14, loginReward: 400, playReward: 800 },
  { days: 30, loginReward: 1000, playReward: 2000 },
  { days: 60, loginReward: 2500, playReward: 5000 },
  { days: 90, loginReward: 5000, playReward: 10000 },
];

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Format a Date as YYYY-MM-DD (UTC). */
function formatDate(date) {
  return date.toISOString().split('T')[0];
}

/** Return a new YYYY-MM-DD string offset by `days` from `dateStr`. */
function addDays(dateStr, days) {
  const d = new Date(dateStr + 'T00:00:00Z');
  d.setUTCDate(d.getUTCDate() + days);
  return formatDate(d);
}

// ---------------------------------------------------------------------------
// Data generation
// ---------------------------------------------------------------------------

/**
 * 60-day activity pattern.
 *   P = login + play
 *   L = login only
 *   F = freeze (missed day, freeze preserves streak)
 *   _ = nothing (streak breaks)
 *
 * Layout (60 chars):
 *   Days  1-8  : PPPPPPPP    (streak 1->8, milestones 3,7)
 *   Day   9    : _           (streak broken)
 *   Days 10-25 : PPPPPPPPPPPPPPPP (streak 1->16, milestones 3,7,14)
 *   Day  26    : F           (freeze preserves streak at 16)
 *   Days 27-31 : PPPPP       (streak 17->21)
 *   Day  32    : _           (streak broken)
 *   Day  33    : L           (login only, login streak 1)
 *   Days 34-36 : PPP         (login 2->4, play 1->3, milestone 3)
 *   Days 37-38 : __          (streak broken)
 *   Days 39-60 : PPPPPPPPPPPPPPPPPPPPPP (streak 1->22, milestones 3,7,14)
 */
const PATTERN = 'PPPPPPPP_PPPPPPPPPPPPPPPPFPPPPP_LPPP__PPPPPPPPPPPPPPPPPPPPPP';

/**
 * Generate all seed records from the activity pattern.
 *
 * @param {string} [referenceDate] - YYYY-MM-DD to treat as "today" (default: actual today UTC)
 * @returns {{ player, activities, rewards, freezeHistory }}
 */
function generateSeedData(referenceDate) {
  const today = referenceDate || formatDate(new Date());
  const startDate = addDays(today, -(PATTERN.length - 1));

  const activities = [];
  const rewards = [];
  const freezeHistory = [];

  let loginStreak = 0;
  let playStreak = 0;
  let bestLoginStreak = 0;
  let bestPlayStreak = 0;
  let lastLoginDate = null;
  let lastPlayDate = null;
  let freezesAvailable = 1;
  let freezesUsedThisMonth = 0;
  let lastFreezeGrantDate = null;

  for (let i = 0; i < PATTERN.length; i++) {
    const code = PATTERN[i];
    const date = addDays(startDate, i);
    const currentMonth = date.substring(0, 7);
    const ts = new Date(date + 'T12:00:00Z').toISOString();

    // Lazy monthly freeze grant (mirrors check-in logic)
    if (lastFreezeGrantDate !== currentMonth) {
      freezesAvailable = 1;
      freezesUsedThisMonth = 0;
      lastFreezeGrantDate = currentMonth;
    }

    // -- Gap day -----------------------------------------------------------
    if (code === '_') {
      const hadStreak = loginStreak > 0 || playStreak > 0;
      loginStreak = 0;
      playStreak = 0;
      if (hadStreak) {
        activities.push({
          playerId: PLAYER_ID,
          date,
          loggedIn: false,
          played: false,
          freezeUsed: false,
          streakBroken: true,
          loginStreakAtDay: 0,
          playStreakAtDay: 0,
          checkedIn: false,
          timestamp: ts,
        });
      }
      continue;
    }

    // -- Freeze day --------------------------------------------------------
    if (code === 'F') {
      if (freezesAvailable > 0) {
        freezesAvailable--;
        freezesUsedThisMonth++;
        activities.push({
          playerId: PLAYER_ID,
          date,
          loggedIn: false,
          played: false,
          freezeUsed: true,
          streakBroken: false,
          loginStreakAtDay: loginStreak,
          playStreakAtDay: playStreak,
          checkedIn: false,
          timestamp: ts,
        });
        freezeHistory.push({
          playerId: PLAYER_ID,
          date,
          source: 'free_monthly',
          createdAt: ts,
        });
      }
      continue;
    }

    // -- Active day (P or L) -----------------------------------------------
    loginStreak++;
    lastLoginDate = date;

    const played = code === 'P';
    if (played) {
      playStreak++;
      lastPlayDate = date;
    } else {
      // Login-only day resets play streak
      playStreak = 0;
    }

    if (loginStreak > bestLoginStreak) bestLoginStreak = loginStreak;
    if (playStreak > bestPlayStreak) bestPlayStreak = playStreak;

    // Check login milestones
    const lm = MILESTONES.find((m) => m.days === loginStreak);
    if (lm) {
      rewards.push({
        playerId: PLAYER_ID,
        rewardId: crypto.randomUUID(),
        type: 'login_milestone',
        milestone: lm.days,
        points: lm.loginReward,
        streakCount: loginStreak,
        createdAt: ts,
      });
    }

    // Check play milestones
    if (played) {
      const pm = MILESTONES.find((m) => m.days === playStreak);
      if (pm) {
        rewards.push({
          playerId: PLAYER_ID,
          rewardId: crypto.randomUUID(),
          type: 'play_milestone',
          milestone: pm.days,
          points: pm.playReward,
          streakCount: playStreak,
          createdAt: ts,
        });
      }
    }

    activities.push({
      playerId: PLAYER_ID,
      date,
      loggedIn: true,
      played,
      freezeUsed: false,
      streakBroken: false,
      loginStreakAtDay: loginStreak,
      playStreakAtDay: playStreak,
      checkedIn: true,
      timestamp: ts,
    });
  }

  const player = {
    playerId: PLAYER_ID,
    loginStreak,
    playStreak,
    bestLoginStreak,
    bestPlayStreak,
    lastLoginDate,
    lastPlayDate,
    freezesAvailable,
    freezesUsedThisMonth,
    lastFreezeGrantDate,
    updatedAt: new Date().toISOString(),
  };

  return { player, activities, rewards, freezeHistory };
}

// ---------------------------------------------------------------------------
// DynamoDB writes
// ---------------------------------------------------------------------------

function createDocClient() {
  const ddbClient = new DynamoDBClient({
    region: REGION,
    endpoint: ENDPOINT,
    credentials: {
      accessKeyId: process.env.AWS_ACCESS_KEY_ID || 'local',
      secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY || 'local',
    },
  });
  return DynamoDBDocumentClient.from(ddbClient, {
    marshallOptions: { removeUndefinedValues: true },
  });
}

/** Write items in batches of 25 (DynamoDB BatchWriteItem limit). */
async function batchWrite(client, tableName, items) {
  const BATCH_SIZE = 25;
  for (let i = 0; i < items.length; i += BATCH_SIZE) {
    const batch = items.slice(i, i + BATCH_SIZE);
    await client.send(
      new BatchWriteCommand({
        RequestItems: {
          [tableName]: batch.map((item) => ({ PutRequest: { Item: item } })),
        },
      })
    );
  }
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function seed() {
  console.log(`Seeding streak data for ${PLAYER_ID}...`);
  console.log(`DynamoDB endpoint: ${ENDPOINT}\n`);

  const { player, activities, rewards, freezeHistory } = generateSeedData();

  console.log(`  Activities : ${activities.length} records`);
  console.log(`  Rewards    : ${rewards.length} milestones`);
  console.log(`  Freezes    : ${freezeHistory.length} entries`);
  console.log(`  Login streak : ${player.loginStreak} (best ${player.bestLoginStreak})`);
  console.log(`  Play streak  : ${player.playStreak} (best ${player.bestPlayStreak})\n`);

  const client = createDocClient();

  await batchWrite(client, PLAYERS_TABLE, [player]);
  console.log('  -> Player record written');

  await batchWrite(client, ACTIVITY_TABLE, activities);
  console.log('  -> Activity records written');

  if (rewards.length > 0) {
    await batchWrite(client, REWARDS_TABLE, rewards);
    console.log('  -> Reward records written');
  }

  if (freezeHistory.length > 0) {
    await batchWrite(client, FREEZE_TABLE, freezeHistory);
    console.log('  -> Freeze history written');
  }

  console.log('\nSeed complete!');
}

// Allow running directly or importing for tests
if (require.main === module) {
  seed().catch((err) => {
    console.error('Seed failed:', err.message);
    process.exit(1);
  });
}

module.exports = { generateSeedData, PATTERN, PLAYER_ID };
