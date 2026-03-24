'use strict';

const { v4: uuidv4 } = require('uuid');
const { PutCommand } = require('@aws-sdk/lib-dynamodb');
const { docClient } = require('../../shared/config/dynamo');
const { getMilestone } = require('../config/constants');

const REWARDS_TABLE = process.env.STREAKS_REWARDS_TABLE || 'streaks-rewards';

/**
 * Check if a streak count hits a milestone and award the reward.
 *
 * Only awards on exact match (streakCount === milestone.days) so that
 * rewards are given once per milestone per streak instance.
 *
 * @param {string} playerId
 * @param {number} streakCount - current streak length
 * @param {'login' | 'play'} streakType - which streak was updated
 * @returns {Promise<object|null>} the reward record if awarded, null otherwise
 */
async function checkAndAwardMilestone(playerId, streakCount, streakType) {
  const milestone = getMilestone(streakCount);
  if (!milestone) return null;

  const type = streakType === 'login' ? 'login_milestone' : 'play_milestone';
  const points = streakType === 'login' ? milestone.loginReward : milestone.playReward;

  const reward = {
    playerId,
    rewardId: uuidv4(),
    type,
    milestone: milestone.days,
    points,
    streakCount,
    createdAt: new Date().toISOString(),
    notification: {
      title: `${milestone.days}-Day Streak!`,
      body: `You earned ${points} bonus points for your ${streakType} streak!`,
      type: 'streak_milestone',
    },
  };

  await docClient.send(
    new PutCommand({
      TableName: REWARDS_TABLE,
      Item: reward,
    })
  );

  // Notification flag — log for now; a dedicated notification service can consume this later
  console.log(
    `[NOTIFICATION] Milestone reward: player=${playerId} type=${type} milestone=${milestone.days} points=${points}`
  );

  return reward;
}

/**
 * Build a "streak at risk" notification payload.
 *
 * @param {'login' | 'play'} streakType
 * @param {number} currentStreak
 * @returns {{ title: string, body: string, type: string }}
 */
function buildStreakAtRiskNotification(streakType, currentStreak) {
  return {
    title: 'Streak at Risk!',
    body: `Your ${currentStreak}-day ${streakType} streak will reset if you don't check in before midnight UTC!`,
    type: 'streak_at_risk',
  };
}

module.exports = { checkAndAwardMilestone, buildStreakAtRiskNotification, REWARDS_TABLE };
