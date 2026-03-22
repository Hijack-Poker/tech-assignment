import { test, expect } from '../../fixtures/api-fixture.js';
import { TABLE_ID } from '../../helpers/constants.js';

test.describe('GET /table/:tableId/seats', () => {
  test('returns 6 seats with availability', async ({ pokerApi }) => {
    // Ensure table exists
    await pokerApi.process(TABLE_ID);

    const { status, body } = await pokerApi.getSeats(TABLE_ID);

    expect(status).toBe(200);
    expect(body.seats).toHaveLength(6);

    for (const seat of body.seats) {
      expect(seat).toHaveProperty('seat');
      expect(seat).toHaveProperty('available');
      expect(seat).toHaveProperty('hasPlayer');
      expect(seat).toHaveProperty('username');
      expect(seat.seat).toBeGreaterThanOrEqual(1);
      expect(seat.seat).toBeLessThanOrEqual(6);
    }

    // All seats should be available when no human is joined
    expect(body.seats.every((s) => s.available)).toBe(true);
  });

  test('seat availability changes after human joins', async ({ pokerApi }) => {
    await pokerApi.process(TABLE_ID);

    // Join seat 3
    const joinRes = await pokerApi.join(TABLE_ID, 'SeatTester', 3);
    expect(joinRes.status).toBe(200);

    const { body } = await pokerApi.getSeats(TABLE_ID);

    // Seat 3 should no longer be available
    const seat3 = body.seats.find((s) => s.seat === 3);
    expect(seat3?.available).toBe(false);

    // Other seats should still be available
    const otherSeats = body.seats.filter((s) => s.seat !== 3);
    expect(otherSeats.every((s) => s.available)).toBe(true);
  });
});
