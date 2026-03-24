import { useRef, useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Chip,
  CircularProgress,
  Snackbar,
  Alert,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import LocalFireDepartmentIcon from '@mui/icons-material/LocalFireDepartment';
import StyleIcon from '@mui/icons-material/Style';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';
import html2canvas from 'html2canvas';
import { getShareData } from '../api/streaks.api';
import type { ShareData } from '../types/streaks.types';
import type { VipTier } from '../types/streaks.types';

const TIER_CONFIG: Record<VipTier, { label: string; color: string; bg: string }> = {
  bronze: { label: 'Bronze', color: '#CD7F32', bg: 'rgba(205,127,50,0.15)' },
  silver: { label: 'Silver', color: '#C0C0C0', bg: 'rgba(192,192,192,0.15)' },
  gold: { label: 'Gold', color: '#FFD700', bg: 'rgba(255,215,0,0.15)' },
  platinum: { label: 'Platinum', color: '#E5E4E2', bg: 'rgba(229,228,226,0.2)' },
};

interface ShareCardProps {
  open: boolean;
  onClose: () => void;
}

function ShareCard({ open, onClose }: ShareCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const [shareData, setShareData] = useState<ShareData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    setError(null);
    getShareData()
      .then((data) => setShareData(data))
      .catch(() => setError('Failed to load share data'))
      .finally(() => setLoading(false));
  }, [open]);

  const handleDownload = async () => {
    if (!cardRef.current) return;
    setDownloading(true);
    try {
      const canvas = await html2canvas(cardRef.current, {
        backgroundColor: '#0F1117',
        scale: 2,
      });
      const link = document.createElement('a');
      link.download = 'hijack-poker-streak.png';
      link.href = canvas.toDataURL('image/png');
      link.click();
    } catch {
      setError('Failed to generate image');
    } finally {
      setDownloading(false);
    }
  };

  const handleCopyLink = async () => {
    if (!shareData) return;
    try {
      await navigator.clipboard.writeText(shareData.shareText);
      setCopied(true);
    } catch {
      setError('Failed to copy to clipboard');
    }
  };

  const tierConfig = shareData ? TIER_CONFIG[shareData.tier] : TIER_CONFIG.bronze;

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            bgcolor: '#1A1D27',
            border: '1px solid #2A2D3A',
            borderRadius: 4,
          },
        }}
      >
        <DialogTitle
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            fontWeight: 700,
            fontSize: 18,
            color: '#fff',
          }}
        >
          Share Your Streak
          <IconButton onClick={onClose} size="small" sx={{ color: '#8B8FA3' }}>
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent sx={{ pb: 3 }}>
          {loading && (
            <Box display="flex" justifyContent="center" py={6}>
              <CircularProgress sx={{ color: '#FF6B35' }} />
            </Box>
          )}

          {error && !loading && (
            <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
              {error}
            </Alert>
          )}

          {shareData && !loading && (
            <>
              {/* Shareable card */}
              <Box
                ref={cardRef}
                sx={{
                  background: 'linear-gradient(160deg, #1A1D27 0%, #0F1117 50%, #1A1022 100%)',
                  border: '1px solid #2A2D3A',
                  borderRadius: 4,
                  p: 4,
                  mb: 3,
                  position: 'relative',
                  overflow: 'hidden',
                }}
              >
                {/* Decorative glow */}
                <Box
                  sx={{
                    position: 'absolute',
                    top: -40,
                    right: -40,
                    width: 160,
                    height: 160,
                    borderRadius: '50%',
                    background: 'radial-gradient(circle, rgba(255,107,53,0.15), transparent 70%)',
                    pointerEvents: 'none',
                  }}
                />
                <Box
                  sx={{
                    position: 'absolute',
                    bottom: -30,
                    left: -30,
                    width: 120,
                    height: 120,
                    borderRadius: '50%',
                    background: 'radial-gradient(circle, rgba(124,58,237,0.12), transparent 70%)',
                    pointerEvents: 'none',
                  }}
                />

                {/* Branding header */}
                <Box display="flex" alignItems="center" justifyContent="space-between" mb={3}>
                  <Box display="flex" alignItems="center" gap={1}>
                    <LocalFireDepartmentIcon sx={{ fontSize: 24, color: '#FF6B35' }} />
                    <Typography sx={{ fontSize: 16, fontWeight: 800, color: '#fff', letterSpacing: 0.5 }}>
                      HIJACK POKER
                    </Typography>
                  </Box>
                  <Chip
                    label={`${tierConfig.label} Tier`}
                    size="small"
                    sx={{
                      fontWeight: 700,
                      fontSize: 11,
                      color: tierConfig.color,
                      bgcolor: tierConfig.bg,
                      border: `1px solid ${tierConfig.color}`,
                      borderRadius: 2,
                    }}
                  />
                </Box>

                {/* Player name */}
                <Typography
                  sx={{
                    fontSize: 22,
                    fontWeight: 700,
                    color: '#fff',
                    mb: 3,
                  }}
                >
                  {shareData.playerName}
                </Typography>

                {/* Streak stats */}
                <Box display="flex" gap={3} mb={3}>
                  {/* Login Streak */}
                  <Box
                    sx={{
                      flex: 1,
                      background: 'rgba(255,107,53,0.08)',
                      borderRadius: 3,
                      p: 2.5,
                      border: '1px solid rgba(255,107,53,0.15)',
                    }}
                  >
                    <Box display="flex" alignItems="center" gap={1} mb={1}>
                      <LocalFireDepartmentIcon sx={{ fontSize: 20, color: '#FF6B35' }} />
                      <Typography sx={{ fontSize: 12, fontWeight: 600, color: '#8B8FA3' }}>
                        Login Streak
                      </Typography>
                    </Box>
                    <Typography sx={{ fontSize: 32, fontWeight: 800, color: '#fff', lineHeight: 1.1 }}>
                      {shareData.loginStreak}
                    </Typography>
                    <Typography sx={{ fontSize: 11, color: '#8B8FA3', mt: 0.5 }}>
                      Best: {shareData.bestLoginStreak} days
                    </Typography>
                  </Box>

                  {/* Play Streak */}
                  <Box
                    sx={{
                      flex: 1,
                      background: 'rgba(124,58,237,0.08)',
                      borderRadius: 3,
                      p: 2.5,
                      border: '1px solid rgba(124,58,237,0.15)',
                    }}
                  >
                    <Box display="flex" alignItems="center" gap={1} mb={1}>
                      <StyleIcon sx={{ fontSize: 20, color: '#A78BFA' }} />
                      <Typography sx={{ fontSize: 12, fontWeight: 600, color: '#8B8FA3' }}>
                        Play Streak
                      </Typography>
                    </Box>
                    <Typography sx={{ fontSize: 32, fontWeight: 800, color: '#fff', lineHeight: 1.1 }}>
                      {shareData.playStreak}
                    </Typography>
                    <Typography sx={{ fontSize: 11, color: '#8B8FA3', mt: 0.5 }}>
                      Best: {shareData.bestPlayStreak} days
                    </Typography>
                  </Box>
                </Box>

                {/* Rewards earned */}
                {shareData.totalRewards > 0 && (
                  <Box display="flex" alignItems="center" gap={1} mb={2}>
                    <EmojiEventsIcon sx={{ fontSize: 18, color: '#FFD700' }} />
                    <Typography sx={{ fontSize: 13, color: '#8B8FA3' }}>
                      <Box component="span" sx={{ color: '#FFD700', fontWeight: 700 }}>
                        {shareData.totalRewards}
                      </Box>{' '}
                      rewards earned
                    </Typography>
                  </Box>
                )}

                {/* Footer branding */}
                <Box
                  sx={{
                    borderTop: '1px solid #2A2D3A',
                    pt: 2,
                    mt: 1,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                  }}
                >
                  <Typography sx={{ fontSize: 11, color: '#555' }}>
                    hijackpoker.com
                  </Typography>
                  <Typography sx={{ fontSize: 11, color: '#555' }}>
                    Daily Streaks
                  </Typography>
                </Box>
              </Box>

              {/* Action buttons */}
              <Box display="flex" gap={2}>
                <Button
                  variant="contained"
                  startIcon={downloading ? <CircularProgress size={16} sx={{ color: '#fff' }} /> : <DownloadIcon />}
                  onClick={handleDownload}
                  disabled={downloading}
                  fullWidth
                  sx={{
                    background: 'linear-gradient(135deg, #FF6B35, #FF4444)',
                    borderRadius: 3,
                    py: 1.5,
                    fontWeight: 600,
                    textTransform: 'none',
                    '&:hover': {
                      background: 'linear-gradient(135deg, #FF5722, #E53935)',
                    },
                  }}
                >
                  {downloading ? 'Generating...' : 'Download'}
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<ContentCopyIcon />}
                  onClick={handleCopyLink}
                  fullWidth
                  sx={{
                    borderColor: copied ? '#2E7D32' : '#2A2D3A',
                    color: copied ? '#66BB6A' : '#fff',
                    borderRadius: 3,
                    py: 1.5,
                    fontWeight: 600,
                    textTransform: 'none',
                    '&:hover': {
                      borderColor: copied ? '#2E7D32' : '#FF6B35',
                      bgcolor: copied ? 'rgba(46,125,50,0.08)' : 'rgba(255,107,53,0.08)',
                    },
                  }}
                >
                  {copied ? 'Copied!' : 'Copy Text'}
                </Button>
              </Box>
            </>
          )}
        </DialogContent>
      </Dialog>

      <Snackbar
        open={copied}
        autoHideDuration={2000}
        onClose={() => setCopied(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" sx={{ borderRadius: 3 }}>
          Copied to clipboard!
        </Alert>
      </Snackbar>
    </>
  );
}

export default ShareCard;
