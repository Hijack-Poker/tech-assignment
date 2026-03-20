import { waitForService } from './helpers/wait-for-services.js';
import { API_BASE_URL, HAND_VIEWER_URL, WEBGL_URL } from './helpers/constants.js';

export default async function globalSetup(): Promise<void> {
  console.log('\n[global-setup] Checking services...');

  const checks = [
    { name: 'holdem-processor', url: `${API_BASE_URL}/health` },
    { name: 'hand-viewer', url: HAND_VIEWER_URL },
    { name: 'webgl', url: WEBGL_URL },
  ];

  const results = await Promise.allSettled(
    checks.map(async ({ name, url }) => {
      try {
        await waitForService(url, { timeout: 10_000 });
        console.log(`  ✓ ${name} is ready`);
      } catch {
        console.log(`  ✗ ${name} not available (tests targeting it will fail)`);
      }
    }),
  );

  console.log('[global-setup] Done.\n');
}
