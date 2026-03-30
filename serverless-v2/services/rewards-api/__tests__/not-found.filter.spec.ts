import { ArgumentsHost, NotFoundException } from '@nestjs/common';
import { NotFoundFilter } from '../src/filters/not-found.filter';

function createMockHost() {
  const json = jest.fn();
  const status = jest.fn().mockReturnValue({ json });
  const response = { status, json };

  const host = {
    switchToHttp: () => ({
      getResponse: () => response,
    }),
  } as unknown as ArgumentsHost;

  return { host, status, json };
}

describe('NotFoundFilter', () => {
  let filter: NotFoundFilter;

  beforeEach(() => {
    filter = new NotFoundFilter();
  });

  it('passes through custom exception objects with 404 status', () => {
    const { host, status, json } = createMockHost();
    const customBody = { error: 'Not found', message: 'Player xyz not found' };
    const exception = new NotFoundException(customBody);

    filter.catch(exception, host);

    expect(status).toHaveBeenCalledWith(404);
    expect(json).toHaveBeenCalledWith(customBody);
  });

  it('normalizes NestJS-generated route 404s', () => {
    // NestJS route 404s include a statusCode property
    const { host, status, json } = createMockHost();
    const exception = new NotFoundException();

    filter.catch(exception, host);

    expect(status).toHaveBeenCalledWith(404);
    expect(json).toHaveBeenCalledWith({ error: 'Not found' });
  });
});
