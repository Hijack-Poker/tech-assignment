'use strict';

const fs = require('fs');
const path = require('path');

/**
 * Validate that docker-compose.yml defines all four streaks tables
 * with the correct key schemas.
 */
describe('Streaks — DynamoDB Table Schemas', () => {
  let composeContent;

  beforeAll(() => {
    const composePath = path.resolve(__dirname, '../../../../docker-compose.yml');
    composeContent = fs.readFileSync(composePath, 'utf-8');
  });

  const tables = [
    {
      name: 'streaks-players',
      pk: 'playerId',
      sk: null,
    },
    {
      name: 'streaks-activity',
      pk: 'playerId',
      sk: 'date',
    },
    {
      name: 'streaks-rewards',
      pk: 'playerId',
      sk: 'rewardId',
    },
    {
      name: 'streaks-freeze-history',
      pk: 'playerId',
      sk: 'date',
    },
  ];

  it('should define all four streaks tables in docker-compose', () => {
    for (const table of tables) {
      expect(composeContent).toContain(`--table-name ${table.name}`);
    }
  });

  it.each(tables)('$name should have playerId as partition key', (table) => {
    // Find the create-table block for this table
    const tableIdx = composeContent.indexOf(`--table-name ${table.name}`);
    expect(tableIdx).toBeGreaterThan(-1);

    // The block extends to the next create-table or end of command
    const nextTableIdx = composeContent.indexOf('--table-name', tableIdx + 1);
    const block = nextTableIdx > -1
      ? composeContent.slice(tableIdx, nextTableIdx)
      : composeContent.slice(tableIdx);

    expect(block).toContain('AttributeName=playerId,AttributeType=S');
    expect(block).toContain('AttributeName=playerId,KeyType=HASH');
  });

  it.each(tables.filter((t) => t.sk !== null))(
    '$name should have $sk as sort key',
    (table) => {
      const tableIdx = composeContent.indexOf(`--table-name ${table.name}`);
      const nextTableIdx = composeContent.indexOf('--table-name', tableIdx + 1);
      const block = nextTableIdx > -1
        ? composeContent.slice(tableIdx, nextTableIdx)
        : composeContent.slice(tableIdx);

      expect(block).toContain(`AttributeName=${table.sk},KeyType=RANGE`);
    }
  );

  it('streaks-players should have no sort key (hash-only)', () => {
    const tableIdx = composeContent.indexOf('--table-name streaks-players');
    const nextTableIdx = composeContent.indexOf('--table-name', tableIdx + 1);
    const block = composeContent.slice(tableIdx, nextTableIdx);

    expect(block).not.toContain('KeyType=RANGE');
  });
});
