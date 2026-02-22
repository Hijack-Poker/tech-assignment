'use strict';

/**
 * Streak milestone definitions.
 * Rewards awarded at specific streak lengths.
 */
const MILESTONES = [
  { days: 3, reward: 'Bronze Badge', bonusPoints: 10, description: '3-day streak' },
  { days: 7, reward: 'Silver Badge', bonusPoints: 25, description: '7-day streak' },
  { days: 14, reward: 'Gold Badge', bonusPoints: 50, description: '14-day streak' },
  { days: 30, reward: 'Platinum Badge', bonusPoints: 100, description: '30-day streak' },
  { days: 60, reward: 'Diamond Badge', bonusPoints: 250, description: '60-day streak' },
  { days: 100, reward: 'Legend Badge', bonusPoints: 500, description: '100-day streak' },
];

/**
 * Check if a streak length hits a milestone.
 */
function getMilestone(streakLength) {
  return MILESTONES.find((m) => m.days === streakLength) || null;
}

/**
 * Get all milestones achieved for a given streak length.
 */
function getAchievedMilestones(streakLength) {
  return MILESTONES.filter((m) => m.days <= streakLength);
}

module.exports = { MILESTONES, getMilestone, getAchievedMilestones };
