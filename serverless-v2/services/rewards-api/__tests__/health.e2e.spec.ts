import { Test, TestingModule } from '@nestjs/testing';
import { INestApplication } from '@nestjs/common';
import * as http from 'http';
import { AppModule } from '../src/app.module';
import { NotFoundFilter } from '../src/filters/not-found.filter';

function request(
  server: http.Server,
  method: string,
  path: string,
  headers?: Record<string, string>,
): Promise<{ status: number; body: Record<string, unknown> }> {
  return new Promise((resolve, reject) => {
    const address = server.address() as { port: number };
    const req = http.request(
      { hostname: '127.0.0.1', port: address.port, path, method, headers },
      (res) => {
        let data = '';
        res.on('data', (chunk) => (data += chunk));
        res.on('end', () => resolve({ status: res.statusCode!, body: JSON.parse(data) }));
      },
    );
    req.on('error', reject);
    req.end();
  });
}

describe('Rewards API — Health (e2e)', () => {
  let app: INestApplication;

  beforeAll(async () => {
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleFixture.createNestApplication();
    app.setGlobalPrefix('api/v1');
    app.useGlobalFilters(new NotFoundFilter());
    await app.listen(0);
  });

  afterAll(async () => {
    await app.close();
  });

  it('GET /api/v1/health should return 200 with service info', async () => {
    const res = await request(app.getHttpServer(), 'GET', '/api/v1/health');
    expect(res.status).toBe(200);
    expect(res.body.service).toBe('rewards-api');
    expect(res.body.status).toBe('ok');
    expect(res.body.timestamp).toBeDefined();
  });

  it('should return 404 for unmatched routes', async () => {
    const res = await request(app.getHttpServer(), 'GET', '/api/v1/nonexistent');
    expect(res.status).toBe(404);
    expect(res.body.error).toBe('Not found');
  });

  it('should return 401 without X-Player-Id header', async () => {
    const res = await request(app.getHttpServer(), 'POST', '/api/v1/points/award');
    expect(res.status).toBe(401);
    expect(res.body.error).toBe('Unauthorized');
  });

  it('should return 501 for stub endpoints with auth', async () => {
    const res = await request(app.getHttpServer(), 'GET', '/api/v1/points/leaderboard', {
      'X-Player-Id': 'test-player',
    });
    expect(res.status).toBe(501);
    expect(res.body.error).toBe('Not implemented');
  });
});
