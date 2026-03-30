import { forwardRef, useCallback, useImperativeHandle, useState } from 'react';
import {
  Badge,
  Box,
  IconButton,
  Popover,
  Typography,
} from '@mui/material';
import NotificationsIcon from '@mui/icons-material/Notifications';
import CloseIcon from '@mui/icons-material/Close';
import type { NotificationResponse, NotificationsResponse } from '@shared/types/rewards';
import apiClient from '../api/client';
import { usePolling } from '../hooks/usePolling';

export interface NotificationBellHandle {
  refresh: () => void;
}

function formatTimeAgo(createdAt: string): string {
  const diff = Date.now() - new Date(createdAt).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

const NotificationBell = forwardRef<NotificationBellHandle>((_props, ref) => {
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const fetchNotifications = useCallback(() => {
    apiClient
      .get<NotificationsResponse>('/player/notifications', { params: { unread: true } })
      .then(({ data }) => {
        setNotifications(data.notifications);
        setUnreadCount(data.unreadCount);
      })
      .catch(() => {});
  }, []);

  usePolling(fetchNotifications, 15000);

  useImperativeHandle(ref, () => ({ refresh: fetchNotifications }), [fetchNotifications]);

  const handleDismiss = (notificationId: string) => {
    apiClient
      .patch(`/player/notifications/${notificationId}/dismiss`)
      .then(() => {
        setNotifications((prev) => prev.filter((n) => n.notificationId !== notificationId));
        setUnreadCount((prev) => Math.max(0, prev - 1));
      })
      .catch(() => {});
  };

  const open = Boolean(anchorEl);

  return (
    <>
      <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="notifications">
        <Badge badgeContent={unreadCount} color="secondary">
          <NotificationsIcon />
        </Badge>
      </IconButton>

      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: 360, maxHeight: 400, overflow: 'auto', bgcolor: 'background.paper' } } }}
      >
        <Box sx={{ p: 2 }}>
          <Typography variant="subtitle1" fontWeight={700} sx={{ mb: 1 }}>
            Notifications
          </Typography>

          {notifications.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
              No notifications
            </Typography>
          ) : (
            notifications.map((n) => (
              <Box
                key={n.notificationId}
                sx={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  gap: 1,
                  py: 1.5,
                  borderBottom: '1px solid rgba(255,255,255,0.06)',
                  '&:last-child': { borderBottom: 'none' },
                }}
              >
                <Box sx={{ flex: 1 }}>
                  <Typography variant="body2" fontWeight={600}>
                    {n.title}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {n.description}
                  </Typography>
                  <Typography variant="caption" display="block" color="text.secondary" sx={{ mt: 0.5 }}>
                    {formatTimeAgo(n.createdAt)}
                  </Typography>
                </Box>
                <IconButton
                  size="small"
                  onClick={() => handleDismiss(n.notificationId)}
                  aria-label="dismiss"
                >
                  <CloseIcon fontSize="small" />
                </IconButton>
              </Box>
            ))
          )}
        </Box>
      </Popover>
    </>
  );
});

NotificationBell.displayName = 'NotificationBell';

export default NotificationBell;
