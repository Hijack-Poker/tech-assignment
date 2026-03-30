import { ExecutionContext, HttpException, HttpStatus } from '@nestjs/common';
import { AuthGuard } from '../src/auth/auth.guard';

function createMockContext(headers: Record<string, string> = {}): ExecutionContext {
  const request: Record<string, unknown> = { headers };
  return {
    switchToHttp: () => ({
      getRequest: () => request,
    }),
  } as unknown as ExecutionContext;
}

describe('AuthGuard', () => {
  let guard: AuthGuard;

  beforeEach(() => {
    guard = new AuthGuard();
  });

  it('returns true when X-Player-Id header is present', () => {
    const context = createMockContext({ 'x-player-id': 'player-001' });
    expect(guard.canActivate(context)).toBe(true);
  });

  it('sets playerId on the request object', () => {
    const request: Record<string, unknown> = { headers: { 'x-player-id': 'player-042' } };
    const context = {
      switchToHttp: () => ({ getRequest: () => request }),
    } as unknown as ExecutionContext;

    guard.canActivate(context);
    expect(request['playerId']).toBe('player-042');
  });

  it('throws UNAUTHORIZED when header is missing', () => {
    const context = createMockContext({});
    expect(() => guard.canActivate(context)).toThrow(HttpException);
  });

  it('throws with 401 status code', () => {
    const context = createMockContext({});
    try {
      guard.canActivate(context);
      fail('Expected HttpException');
    } catch (err) {
      expect((err as HttpException).getStatus()).toBe(HttpStatus.UNAUTHORIZED);
    }
  });

  it('includes descriptive error message', () => {
    const context = createMockContext({});
    try {
      guard.canActivate(context);
      fail('Expected HttpException');
    } catch (err) {
      const response = (err as HttpException).getResponse() as Record<string, string>;
      expect(response.error).toBe('Unauthorized');
      expect(response.message).toContain('X-Player-Id');
    }
  });
});
