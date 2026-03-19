import { API_BASE_URL } from './constants.js';

async function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function waitForService(
  url: string,
  { interval = 2000, timeout = 60_000 } = {},
): Promise<void> {
  const deadline = Date.now() + timeout;

  while (Date.now() < deadline) {
    try {
      const res = await fetch(url);
      if (res.ok) return;
    } catch {
      // service not ready yet
    }
    await sleep(interval);
  }

  throw new Error(`Service at ${url} did not become healthy within ${timeout}ms`);
}

export async function waitForAllServices(): Promise<void> {
  console.log('Waiting for holdem-processor API...');
  await waitForService(`${API_BASE_URL}/health`);
  console.log('holdem-processor is ready.');
}
