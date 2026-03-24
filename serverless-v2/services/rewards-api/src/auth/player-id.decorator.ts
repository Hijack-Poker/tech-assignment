import { createParamDecorator, ExecutionContext } from '@nestjs/common';

export const PlayerId = createParamDecorator(
  (_data: unknown, ctx: ExecutionContext): string => {
    const request = ctx.switchToHttp().getRequest();
    return request.playerId as string;
  },
);
