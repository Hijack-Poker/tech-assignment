import { test, expect } from '../../fixtures/api-fixture.js';
import { API_BASE_URL } from '../../helpers/constants.js';

test.describe('Error handling', () => {
  test('POST /process without tableId returns 400', async () => {
    const res = await fetch(`${API_BASE_URL}/process`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({}),
    });

    expect(res.status).toBe(400);
    const body = await res.json();
    expect(body.error).toContain('tableId');
  });

  test('POST /action without required fields returns 400', async () => {
    const res = await fetch(`${API_BASE_URL}/action`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tableId: 1 }),
    });

    expect(res.status).toBe(400);
    const body = await res.json();
    expect(body.error).toBeTruthy();
  });

  test('POST /join without tableId returns 400', async () => {
    const res = await fetch(`${API_BASE_URL}/join`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({}),
    });

    expect(res.status).toBe(400);
    const body = await res.json();
    expect(body.error).toContain('tableId');
  });

  test('POST /leave without tableId returns 400', async () => {
    const res = await fetch(`${API_BASE_URL}/leave`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({}),
    });

    expect(res.status).toBe(400);
    const body = await res.json();
    expect(body.error).toContain('tableId');
  });
});
