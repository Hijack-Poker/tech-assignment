'use strict';

const { getPlayer, updatePlayer, addFreezeHistory } = require('./dynamo.service');

// Monthly free freezes granted
const MONTHLY_FREE_FREEZES = 1;

/**
 * Check whether a player is due for their monthly free freeze grant.
 * Uses lazy evaluation: compares lastFreezeGrantDate against the current month.
 *
 * @param {object} player - player record from DynamoDB
 * @param {string} currentMonth - YYYY-MM string
 * @returns {{ freezesAvailable: number, freezesUsedThisMonth: number, lastFreezeGrantDate: string, granted: boolean }}
 */
function checkMonthlyFreezeGrant(player, currentMonth) {
  let freezesAvailable = player.freezesAvailable || 0;
  let freezesUsedThisMonth = player.freezesUsedThisMonth || 0;
  const lastFreezeGrantDate = player.lastFreezeGrantDate || '';

  if (lastFreezeGrantDate !== currentMonth) {
    return {
      freezesAvailable: freezesAvailable + MONTHLY_FREE_FREEZES,
      freezesUsedThisMonth: 0,
      lastFreezeGrantDate: currentMonth,
      granted: true,
    };
  }

  return {
    freezesAvailable,
    freezesUsedThisMonth,
    lastFreezeGrantDate,
    granted: false,
  };
}

/**
 * Consume a freeze for a player on a missed day.
 * Decrements freezesAvailable, increments freezesUsedThisMonth,
 * and writes a freeze history record.
 *
 * @param {string} playerId
 * @param {string} date - YYYY-MM-DD of the missed day
 * @param {'free_monthly' | 'purchased'} source - freeze source
 * @param {number} currentFreezes - current freezesAvailable
 * @param {number} currentUsedThisMonth - current freezesUsedThisMonth
 * @returns {Promise<{ freezesAvailable: number, freezesUsedThisMonth: number }>}
 */
async function consumeFreeze(playerId, date, source, currentFreezes, currentUsedThisMonth) {
  const freezesAvailable = currentFreezes - 1;
  const freezesUsedThisMonth = currentUsedThisMonth + 1;

  await addFreezeHistory(playerId, date, source);

  return { freezesAvailable, freezesUsedThisMonth };
}

/**
 * Grant additional freezes to a player (admin operation).
 * Increments freezesAvailable by the specified count.
 *
 * @param {string} playerId
 * @param {number} count - number of freezes to grant
 * @returns {Promise<{ playerId: string, freezesAvailable: number }>}
 * @throws {Error} if player not found
 */
async function grantFreezes(playerId, count) {
  const player = await getPlayer(playerId);
  if (!player) {
    const err = new Error('Player not found');
    err.code = 'PLAYER_NOT_FOUND';
    throw err;
  }

  const newFreezes = (player.freezesAvailable || 0) + count;
  await updatePlayer(playerId, {
    freezesAvailable: newFreezes,
    updatedAt: new Date().toISOString(),
  });

  return { playerId, freezesAvailable: newFreezes };
}

module.exports = {
  checkMonthlyFreezeGrant,
  consumeFreeze,
  grantFreezes,
  MONTHLY_FREE_FREEZES,
};
