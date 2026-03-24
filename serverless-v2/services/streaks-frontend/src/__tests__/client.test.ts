import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import apiClient from '../api/client';

describe('apiClient interceptors', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('attaches Authorization header when token is in localStorage', async () => {
    localStorage.setItem('token', 'test-jwt-token');

    // Access the request interceptor by inspecting the config it produces
    const interceptors = (apiClient.interceptors.request as any).handlers;
    const interceptor = interceptors[interceptors.length - 1];
    const config = { headers: {} as Record<string, string> };
    const result = interceptor.fulfilled(config);

    expect(result.headers['Authorization']).toBe('Bearer test-jwt-token');
  });

  it('does not attach Authorization header when no token', async () => {
    const interceptors = (apiClient.interceptors.request as any).handlers;
    const interceptor = interceptors[interceptors.length - 1];
    const config = { headers: {} as Record<string, string> };
    const result = interceptor.fulfilled(config);

    expect(result.headers['Authorization']).toBeUndefined();
  });

  it('attaches X-Player-Id header when playerId is in localStorage', async () => {
    localStorage.setItem('playerId', 'player-123');

    const interceptors = (apiClient.interceptors.request as any).handlers;
    const interceptor = interceptors[interceptors.length - 1];
    const config = { headers: {} as Record<string, string> };
    const result = interceptor.fulfilled(config);

    expect(result.headers['X-Player-Id']).toBe('player-123');
  });

  it('attaches both headers when both are present', async () => {
    localStorage.setItem('token', 'my-jwt');
    localStorage.setItem('playerId', 'player-456');

    const interceptors = (apiClient.interceptors.request as any).handlers;
    const interceptor = interceptors[interceptors.length - 1];
    const config = { headers: {} as Record<string, string> };
    const result = interceptor.fulfilled(config);

    expect(result.headers['Authorization']).toBe('Bearer my-jwt');
    expect(result.headers['X-Player-Id']).toBe('player-456');
  });
});

describe('apiClient configuration', () => {
  it('has the correct baseURL', () => {
    expect(apiClient.defaults.baseURL).toContain('/api/v1');
  });

  it('sets Content-Type to application/json', () => {
    expect(apiClient.defaults.headers['Content-Type']).toBe('application/json');
  });
});
