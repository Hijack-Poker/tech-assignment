#!/usr/bin/env node

/**
 * Seed streaks DynamoDB tables with sample data.
 *
 * Creates 10 players with 60 days of activity history.
 * Run after `docker compose --profile streaks up`.
 *
 * Usage: node scripts/seed-streaks.js
 */

'use strict';

const { DynamoDBClient } = require('@aws-sdk/client-dynamodb');
const { DynamoDBDocumentClient, PutCommand } = require('@aws-sdk/lib-dynamodb');

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

const PLAYERS = [
  { id: 'streak-001', name: 'DailyGrinder', consistency: 0.9 },
  { id: 'streak-002', name: 'WeekendWarrior', consistency: 0.3 },
  { id: 'streak-003', name: 'IronWill', consistency: 0.95 },
  { id: 'streak-004', name: 'CasualPlayer', consistency: 0.2 },
  { id: 'streak-005', name: 'StreakHunter', consistency: 0.85 },
  { id: 'streak-006', name: 'Newcomer', consistency: 0.5 },
  { id: 'streak-007', name: 'Veteran', consistency: 0.75 },
  { id: 'streak-008', name: 'NightOwl', consistency: 0.6 },
  { id: 'streak-009', name: 'EarlyBird', consistency: 0.7 },
  { id: 'streak-010', name: 'Perfectionist', consistency: 1.0 },
];

function formatDate(date) {
  return date.toISOString().split('T')[0]; // YYYY-MM-DD
}

async function seed() {
  console.log(`Seeding streaks data to ${ENDPOINT}...`);
  let playerCount = 0;
  let activityCount = 0;

  for (const player of PLAYERS) {
    let currentStreak = 0;
    let longestStreak = 0;
    let totalCheckIns = 0;
    let tempStreak = 0;

    // Generate 60 days of activity
    const today = new Date();
    const activities = [];

    for (let d = 59; d >= 0; d--) {
      const date = new Date(today);
      date.setDate(date.getDate() - d);
      const dateStr = formatDate(date);

      const checkedIn = Math.random() < player.consistency;

      if (checkedIn) {
        tempStreak++;
        totalCheckIns++;
        if (tempStreak > longestStreak) longestStreak = tempStreak;

        activities.push({
          playerId: player.id,
          date: dateStr,
          checkedIn: true,
          timestamp: date.toISOString(),
        });
      } else {
        tempStreak = 0;
      }

      // Track current streak (from today backwards)
      if (d === 0) currentStreak = tempStreak;
    }

    // Insert player profile
    await docClient.send(
      new PutCommand({
        TableName: 'streaks-players',
        Item: {
          playerId: player.id,
          username: player.name,
          currentStreak,
          longestStreak,
          totalCheckIns,
          lastCheckIn: activities.length > 0
            ? activities[activities.length - 1].date
            : null,
          createdAt: new Date(today.getTime() - 60 * 86400000).toISOString(),
          updatedAt: new Date().toISOString(),
        },
      })
    );
    playerCount++;

    // Insert activity records
    for (const activity of activities) {
      await docClient.send(
        new PutCommand({
          TableName: 'streaks-activity',
          Item: activity,
        })
      );
      activityCount++;
    }

    console.log(
      `  ${player.name}: ${totalCheckIns} check-ins, ` +
      `current streak: ${currentStreak}, longest: ${longestStreak}`
    );
  }

  console.log(`\nSeeded ${playerCount} players and ${activityCount} activity records.`);
  console.log('Done!');
}

seed().catch((err) => {
  console.error('Seed failed:', err.message);
  process.exit(1);
});
