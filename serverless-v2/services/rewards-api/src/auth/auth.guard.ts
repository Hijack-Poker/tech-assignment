import { CanActivate, ExecutionContext, HttpException, HttpStatus, Injectable } from '@nestjs/common';
import { Request } from 'express';

/**
 * Stub auth guard.
 *
 * In production, this validates a JWT token. For the tech assignment,
 * it simply extracts playerId from the X-Player-Id header.
 *
 * Candidates may enhance this with real JWT validation if they choose.
 */
@Injectable()
export class AuthGuard implements CanActivate {
  canActivate(context: ExecutionContext): boolean {
    const request = context.switchToHttp().getRequest<Request>();
    const playerId = request.headers['x-player-id'] as string | undefined;

    if (!playerId) {
      throw new HttpException(
        { error: 'Unauthorized', message: 'X-Player-Id header is required' },
        HttpStatus.UNAUTHORIZED,
      );
    }

    (request as unknown as Record<string, unknown>)['playerId'] = playerId;
    return true;
  }
}
