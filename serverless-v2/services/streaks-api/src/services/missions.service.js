'use strict';

const { getMissions, putMissions, getPlayer, getActivity } = require('./dynamo.service');

/**
 * Mission templates — each day, players get 3 missions from this pool.
 * The selection rotates based on the day of year.
 */
const MISSION_POOL = [
  { id: 'check_in', title: 'Daily Check-In', description: 'Check in today to keep your streak alive', target: 1, reward: 25, type: 'check_in' },
  { id: 'play_hands_3', title: 'Card Shark', description: 'Play 3 poker hands', target: 3, reward: 50, type: 'play_hands' },
  { id: 'play_hands_5', title: 'Table Regular', description: 'Play 5 poker hands', target: 5, reward: 75, type: 'play_hands' },
  { id: 'play_hands_10', title: 'Grinder', description: 'Play 10 poker hands', target: 10, reward: 150, type: 'play_hands' },
  { id: 'maintain_login_3', title: 'Streak Guardian', description: 'Reach a 3-day login streak', target: 3, reward: 100, type: 'login_streak' },
  { id: 'maintain_login_7', title: 'Week Warrior', description: 'Reach a 7-day login streak', target: 7, reward: 250, type: 'login_streak' },
  { id: 'maintain_play_3', title: 'Dedicated Player', description: 'Reach a 3-day play streak', target: 3, reward: 150, type: 'play_streak' },
  { id: 'combo_streak', title: 'Double Threat', description: 'Have both login and play streaks active', target: 1, reward: 200, type: 'combo_streak' },
];

/**
 * Select 3 missions for a given date using deterministic rotation.
 */
function selectDailyMissions(date) {
  const dayOfYear = Math.floor((new Date(date + 'T00:00:00Z') - new Date(date.slice(0, 4) + '-01-01T00:00:00Z')) / 86400000);
  const selected = [];
  // Always include check-in
  selected.push({ ...MISSION_POOL[0] });
  // Rotate through the rest based on day
  const pool = MISSION_POOL.slice(1);
  const idx1 = dayOfYear % pool.length;
  const idx2 = (dayOfYear + 3) % pool.length;
  selected.push({ ...pool[idx1] });
  if (idx2 !== idx1) {
    selected.push({ ...pool[idx2] });
  } else {
    selected.push({ ...pool[(idx2 + 1) % pool.length] });
  }
  return selected;
}

/**
 * Get or generate today's missions for a player.
 */
async function getDailyMissions(playerId, today) {
  let record = await getMissions(playerId, today);

  if (!record) {
    const templates = selectDailyMissions(today);
    const missions = templates.map((t) => ({
      missionId: `${t.id}-${today}`,
      ...t,
      progress: 0,
      status: 'active', // active | completed | claimed
    }));

    record = {
      playerId,
      date: today,
      missions,
      pointsEarnedToday: 0,
      createdAt: new Date().toISOString(),
    };
    await putMissions(record);
  }

  // Recalculate progress from live data
  const player = await getPlayer(playerId);
  const activity = await getActivity(playerId, today, today);
  const todayActivity = activity.length > 0 ? activity[0] : null;

  for (const mission of record.missions) {
    let progress = 0;
    switch (mission.type) {
      case 'check_in':
        progress = (todayActivity && todayActivity.loggedIn) ? 1 : 0;
        break;
      case 'play_hands':
        // Count hands played today (each hand-completed call creates/updates activity)
        progress = player && player.handsPlayedToday ? player.handsPlayedToday : (todayActivity && todayActivity.played ? 1 : 0);
        break;
      case 'login_streak':
        progress = player ? (player.loginStreak || 0) : 0;
        break;
      case 'play_streak':
        progress = player ? (player.playStreak || 0) : 0;
        break;
      case 'combo_streak':
        progress = (player && player.loginStreak > 0 && player.playStreak > 0) ? 1 : 0;
        break;
    }

    mission.progress = Math.min(progress, mission.target);
    if (mission.progress >= mission.target && mission.status === 'active') {
      mission.status = 'completed';
    }
    // Keep claimed status — only skip status change, not progress
  }

  // Save updated progress
  await putMissions(record);
  return record;
}

/**
 * Claim a completed mission's reward.
 */
async function claimMission(playerId, today, missionId) {
  const record = await getMissions(playerId, today);
  if (!record) return { error: 'No missions found for today' };

  const mission = record.missions.find((m) => m.missionId === missionId);
  if (!mission) return { error: 'Mission not found' };
  if (mission.status === 'claimed') return { error: 'Already claimed' };
  if (mission.status !== 'completed') return { error: 'Mission not completed yet' };

  mission.status = 'claimed';
  record.pointsEarnedToday = (record.pointsEarnedToday || 0) + mission.reward;
  await putMissions(record);

  return { reward: mission.reward, pointsEarnedToday: record.pointsEarnedToday };
}

module.exports = { getDailyMissions, claimMission, selectDailyMissions, MISSION_POOL };
