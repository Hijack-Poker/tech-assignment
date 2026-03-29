import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import NotificationBell from '../components/NotificationBell';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    patch: vi.fn(),
  },
}));

import apiClient from '../api/client';

const mockNotifications = {
  notifications: [
    {
      notificationId: 'n1',
      type: 'tier_upgrade',
      title: 'Upgraded to Silver!',
      description: 'Congratulations!',
      dismissed: false,
      createdAt: new Date().toISOString(),
    },
    {
      notificationId: 'n2',
      type: 'milestone',
      title: 'First Hand!',
      description: 'Welcome to the tables!',
      dismissed: false,
      createdAt: new Date().toISOString(),
    },
  ],
  unreadCount: 2,
};

function renderBell() {
  return render(
    <ThemeProvider theme={theme}>
      <NotificationBell />
    </ThemeProvider>,
  );
}

describe('NotificationBell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('renders the bell icon', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: { notifications: [], unreadCount: 0 } });
    renderBell();
    expect(screen.getAllByLabelText('notifications').length).toBeGreaterThanOrEqual(1);
  });

  it('shows badge with unread count', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockNotifications });
    renderBell();

    await waitFor(() => {
      expect(screen.getByText('2')).toBeInTheDocument();
    });
  });

  it('opens popover on click and shows notification titles', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockNotifications });
    renderBell();

    await waitFor(() => {
      expect(screen.getByText('2')).toBeInTheDocument();
    });

    const bellButtons = screen.getAllByLabelText('notifications');
    await user.click(bellButtons[0]);

    expect(screen.getByText('Upgraded to Silver!')).toBeInTheDocument();
    expect(screen.getByText('First Hand!')).toBeInTheDocument();
  });

  it('dismiss button calls API and removes item', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockNotifications });
    vi.mocked(apiClient.patch).mockResolvedValue({ data: { success: true } });
    renderBell();

    await waitFor(() => {
      expect(screen.getByText('2')).toBeInTheDocument();
    });

    const bellButtons = screen.getAllByLabelText('notifications');
    await user.click(bellButtons[0]);
    const dismissButtons = screen.getAllByLabelText('dismiss');
    await user.click(dismissButtons[0]);

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalledWith('/player/notifications/n1/dismiss');
    });

    await waitFor(() => {
      expect(screen.queryByText('Upgraded to Silver!')).not.toBeInTheDocument();
    });
  });

  it('shows empty state text when no notifications', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.get).mockResolvedValue({ data: { notifications: [], unreadCount: 0 } });
    renderBell();

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalled();
    });

    const bellButtons = screen.getAllByLabelText('notifications');
    await user.click(bellButtons[0]);
    expect(screen.getByText('No notifications')).toBeInTheDocument();
  });
});
