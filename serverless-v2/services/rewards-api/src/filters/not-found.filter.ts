import { ArgumentsHost, Catch, ExceptionFilter, NotFoundException } from '@nestjs/common';
import { Response } from 'express';

@Catch(NotFoundException)
export class NotFoundFilter implements ExceptionFilter {
  catch(exception: NotFoundException, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const exceptionResponse = exception.getResponse();

    // Pass through custom exception objects (e.g. player not found).
    // NestJS-generated route 404s include a `statusCode` property — normalize those.
    if (
      typeof exceptionResponse === 'object' &&
      !('statusCode' in (exceptionResponse as Record<string, unknown>))
    ) {
      response.status(404).json(exceptionResponse);
    } else {
      response.status(404).json({ error: 'Not found' });
    }
  }
}
