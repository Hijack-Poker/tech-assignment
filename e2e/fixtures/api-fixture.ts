import { test as base } from '@playwright/test';
import { PokerApiClient } from '../helpers/api-client.js';
import { TABLE_ID } from '../helpers/constants.js';

type ApiFixtures = {
  pokerApi: PokerApiClient;
};

export const test = base.extend<ApiFixtures>({
  pokerApi: async ({}, use) => {
    const api = new PokerApiClient();

    // Clean human state before each test
    await api.leave(TABLE_ID);

    await use(api);

    // Clean up after test
    await api.leave(TABLE_ID);
  },
});

export { expect } from '@playwright/test';
