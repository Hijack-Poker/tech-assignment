import { Test, TestingModule } from '@nestjs/testing';
import { INestApplication, ValidationPipe } from '@nestjs/common';
import * as http from 'http';
import { AppModule } from '../src/app.module';
import { NotFoundFilter } from '../src/filters/not-found.filter';

/**
 * E2E test: Monthly reset tier downgrade flow.
 *
 * Runs against live DynamoDB-local and Redis — requires seeded data.
 * Verifies that Platinum players are downgraded exactly one tier (to Gold)
 * with tier floor protection, and that downgrade notifications are created.
 */

function request(
  server: http.Server,
  method: string,
  path: string,
  body?: unknown,
  headers?: Record<string, string>,
): Promise<{ status: number; body: Record<string, unknown> }> {
  return new Promise((resolve, reject) => {
    const address = server.address() as { port: number };
    const payload = body ? JSON.stringify(body) : undefined;
    const req = http.request(
      {
        hostname: '127.0.0.1',
        port: address.port,
        path,
        method,
        headers: {
          'Content-Type': 'application/json',
          ...headers,
          ...(payload && { 'Content-Length': Buffer.byteLength(payload).toString() }),
        },
      },
      (res) => {
        let data = '';
        res.on('data', (chunk) => (data += chunk));
        res.on('end', () => {
          try {
            resolve({ status: res.statusCode!, body: JSON.parse(data) });
          } catch {
            resolve({ status: res.statusCode!, body: { raw: data } as any });
          }
        });
      },
    );
    req.on('error', reject);
    if (payload) req.write(payload);
    req.end();
  });
}

interface PlayerRewards {
  playerId: string;
  tier: string;
  points: number;
  totalEarned: number;
  [key: string]: unknown;
}

interface Notification {
  type: string;
  title: string;
  description: string;
  [key: string]: unknown;
}

describe('Monthly Reset — Tier Downgrade (e2e)', () => {
  let app: INestApplication;

  beforeAll(async () => {
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleFixture.createNestApplication();
    app.setGlobalPrefix('api/v1');
    app.useGlobalFilters(new NotFoundFilter());
    app.useGlobalPipes(new ValidationPipe({ whitelist: true, forbidNonWhitelisted: true, transform: true }));
    await app.listen(0);
  });

  afterAll(async () => {
    await app.close();
  });

  it('downgrades Platinum players to Gold after monthly reset', async () => {
    const server = app.getHttpServer();

    // ── Step 1: Find all Platinum players before reset ──────────────

    // Use the admin leaderboard to get all players, then check each one
    const leaderboardRes = await request(server, 'GET', '/api/v1/admin/leaderboard?limit=100');
    expect(leaderboardRes.status).toBe(200);

    const allPlayers = leaderboardRes.body.leaderboard as Array<{
      playerId: string;
      tier: string;
      points: number;
    }>;
    expect(allPlayers.length).toBeGreaterThan(0);

    // Identify Platinum players before reset
    const platinumBefore = allPlayers.filter((p) => p.tier === 'Platinum');
    expect(platinumBefore.length).toBeGreaterThan(0);

    // Snapshot their profiles via the dev endpoint for detailed verification
    const profilesBefore: PlayerRewards[] = [];
    for (const p of platinumBefore) {
      const res = await request(server, 'GET', `/api/v1/dev/player/${p.playerId}`);
      expect(res.status).toBe(200);
      const profile = res.body as unknown as PlayerRewards;
      expect(profile.tier).toBe('Platinum');
      expect(profile.totalEarned).toBeGreaterThanOrEqual(10000); // Platinum threshold
      profilesBefore.push(profile);
    }

    // ── Step 2: Run monthly reset ───────────────────────────────────

    const resetRes = await request(server, 'POST', '/api/v1/dev/monthly-reset');
    expect(resetRes.status).toBe(201);
    expect(resetRes.body.processed).toBeGreaterThan(0);
    expect(resetRes.body.downgrades).toBeGreaterThan(0);

    // ── Step 3: Verify Platinum players downgraded exactly one tier ─

    for (const before of profilesBefore) {
      const res = await request(server, 'GET', `/api/v1/dev/player/${before.playerId}`);
      expect(res.status).toBe(200);
      const after = res.body as unknown as PlayerRewards;

      // Tier floor protection: Platinum (4) → Gold (3), exactly one tier drop
      expect(after.tier).toBe('Gold');

      // Points reset to 0 for the new month
      expect(after.points).toBe(0);

      // totalEarned is unchanged — lifetime progress is preserved
      expect(after.totalEarned).toBe(before.totalEarned);
    }

    // ── Step 4: Verify leaderboard reflects stored tier, not points ──

    const leaderboardAfter = await request(server, 'GET', '/api/v1/admin/leaderboard?limit=100');
    expect(leaderboardAfter.status).toBe(200);

    const afterEntries = leaderboardAfter.body.leaderboard as Array<{
      playerId: string;
      tier: string;
      points: number;
    }>;

    for (const before of profilesBefore) {
      const entry = afterEntries.find((e) => e.playerId === before.playerId);
      if (entry) {
        // Leaderboard must show Gold (stored tier), NOT Bronze (from 0 points)
        expect(entry.tier).toBe('Gold');
        expect(entry.points).toBe(0);
      }
    }

    // ── Step 5: Verify downgrade notifications were created ─────────

    // Check the first Platinum player's notifications
    const targetPlayer = profilesBefore[0].playerId;
    const notifRes = await request(
      server,
      'GET',
      '/api/v1/player/notifications?unread=true',
      undefined,
      { 'X-Player-Id': targetPlayer },
    );
    expect(notifRes.status).toBe(200);

    const notifications = (notifRes.body as any).notifications as Notification[];
    const downgradeNotif = notifications.find(
      (n) => n.type === 'tier_downgrade' && n.title.includes('Gold'),
    );
    expect(downgradeNotif).toBeDefined();
    expect(downgradeNotif!.description).toContain('Platinum');
    expect(downgradeNotif!.description).toContain('Gold');
  });
});
