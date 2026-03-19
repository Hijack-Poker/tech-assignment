import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID } from '../../helpers/constants.js';

test.describe('POST /join and POST /leave', () => {
  test('join returns seat and playerId', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);

    const { status, body } = await pokerApi.join(TABLE_ID, 'JoinTest');

    expect(status).toBe(200);
    expect(body.seat).toBeDefined();
    expect(body.playerId).toBeDefined();
    expect(typeof body.seat).toBe('number');
  });

  test('leave succeeds after join', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);
    await pokerApi.join(TABLE_ID, 'LeaveTest');

    const { status, body } = await pokerApi.leave(TABLE_ID);

    expect(status).toBe(200);
    expect(body.success).toBe(true);
  });

  test('double join returns 409', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);

    // First join
    const first = await pokerApi.join(TABLE_ID, 'First');
    expect(first.status).toBe(200);

    // Second join should conflict
    const second = await pokerApi.join(TABLE_ID, 'Second');
    expect(second.status).toBe(409);
    expect(second.body.error).toBe('seat_taken');
  });

  test('can rejoin after leaving', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);

    await pokerApi.join(TABLE_ID, 'Rejoiner');
    await pokerApi.leave(TABLE_ID);

    const { status } = await pokerApi.join(TABLE_ID, 'Rejoiner');
    expect(status).toBe(200);
  });

  test('join with custom seat selection', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);

    const { status, body } = await pokerApi.join(TABLE_ID, 'SeatPicker', 4);

    expect(status).toBe(200);
    expect(body.seat).toBe(4);
  });
});
