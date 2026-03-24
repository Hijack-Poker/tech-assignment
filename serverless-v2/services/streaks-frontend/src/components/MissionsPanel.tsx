import { useState, useEffect, useRef } from 'react';
import { Box, Typography, Card, CardContent, Button, Chip, Skeleton } from '@mui/material';
import TrackChangesIcon from '@mui/icons-material/TrackChanges';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import StarIcon from '@mui/icons-material/Star';
import { useMissions } from '../hooks/useMissions';
import gsap from 'gsap';

function MissionsPanel() {
  const { missions, pointsEarnedToday, loading, claimMission } = useMissions();
  const [claiming, setClaiming] = useState<string | null>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const barsAnimated = useRef(false);

  const handleClaim = async (missionId: string) => {
    setClaiming(missionId);
    try {
      await claimMission(missionId);
    } catch (err) {
      console.error('Failed to claim mission:', err);
    } finally {
      setClaiming(null);
    }
  };

  // Staggered card entrance + progress bar fill
  useEffect(() => {
    if (loading || !listRef.current || barsAnimated.current) return;
    barsAnimated.current = true;

    const cards = listRef.current.querySelectorAll<HTMLElement>('[data-mission]');
    const bars = listRef.current.querySelectorAll<HTMLElement>('[data-bar]');

    // Stagger cards in
    gsap.fromTo(cards,
      { y: 20, opacity: 0 },
      { y: 0, opacity: 1, duration: 0.5, stagger: 0.12, ease: 'power3.out' }
    );

    // Animate progress bars from 0 to their target width
    bars.forEach((bar) => {
      const target = bar.getAttribute('data-bar') || '0';
      gsap.fromTo(bar,
        { width: '0%' },
        { width: `${target}%`, duration: 1, delay: 0.4, ease: 'power2.out' }
      );
    });
  }, [loading, missions]);

  if (loading) {
    return (
      <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
        <CardContent sx={{ p: 3 }}>
          <Skeleton variant="rectangular" height={120} sx={{ borderRadius: 2 }} />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
      <CardContent sx={{ p: 3 }}>
        <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <TrackChangesIcon sx={{ color: '#FF6B35', fontSize: 24 }} />
            <Typography variant="h6" fontWeight={700} color="#fff">
              Daily Missions
            </Typography>
          </Box>
          <Chip
            icon={<StarIcon sx={{ fontSize: 14 }} />}
            label={`${pointsEarnedToday} pts today`}
            size="small"
            sx={{ bgcolor: '#2A2D3A', color: '#FFD700', fontWeight: 600, fontSize: 11 }}
          />
        </Box>

        <Box ref={listRef} display="flex" flexDirection="column" gap={2}>
          {(missions ?? []).map((mission) => {
            const pct = mission.target > 0 ? Math.min((mission.progress / mission.target) * 100, 100) : 0;
            const isCompleted = mission.status === 'completed';
            const isClaimed = mission.status === 'claimed';
            const barColor = isClaimed ? '#4ADE80' : isCompleted ? '#FFB300' : '#FF6B35';

            return (
              <Box
                key={mission.missionId}
                data-mission
                sx={{
                  p: 2,
                  borderRadius: 3,
                  bgcolor: isClaimed ? 'rgba(74,222,128,0.06)' : isCompleted ? 'rgba(255,179,0,0.08)' : '#141720',
                  border: isCompleted ? '1px solid rgba(255,179,0,0.3)' : '1px solid #2A2D3A',
                  opacity: 0,
                }}
              >
                <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                  <Typography sx={{ fontSize: 13, fontWeight: 700, color: isClaimed ? '#4ADE80' : isCompleted ? '#FFB300' : '#fff' }}>
                    {isClaimed && <CheckCircleIcon sx={{ fontSize: 14, mr: 0.5, verticalAlign: 'middle' }} />}
                    {mission.title}
                  </Typography>
                  <Chip
                    label={`+${mission.reward}`}
                    size="small"
                    sx={{
                      height: 20,
                      fontSize: 11,
                      fontWeight: 700,
                      bgcolor: isClaimed ? '#1B5E20' : '#2A2D3A',
                      color: isClaimed ? '#4ADE80' : '#FFD700',
                    }}
                  />
                </Box>
                <Typography sx={{ fontSize: 11, color: '#8B8FA3', mb: 1 }}>
                  {mission.description}
                </Typography>
                <Box display="flex" alignItems="center" gap={1}>
                  {/* Custom progress bar for GSAP animation */}
                  <Box sx={{ flex: 1, height: 6, borderRadius: 3, bgcolor: '#2A2D3A', overflow: 'hidden' }}>
                    <Box
                      data-bar={pct}
                      sx={{
                        height: '100%',
                        width: 0,
                        borderRadius: 3,
                        bgcolor: barColor,
                        boxShadow: isCompleted ? `0 0 8px ${barColor}40` : 'none',
                      }}
                    />
                  </Box>
                  <Typography sx={{ fontSize: 11, color: '#8B8FA3', minWidth: 35, textAlign: 'right' }}>
                    {mission.progress}/{mission.target}
                  </Typography>
                  {isCompleted && !isClaimed && (
                    <Button
                      size="small"
                      onClick={() => handleClaim(mission.missionId)}
                      disabled={claiming === mission.missionId}
                      sx={{
                        minWidth: 60,
                        fontSize: 11,
                        fontWeight: 700,
                        color: '#000',
                        bgcolor: '#FFB300',
                        borderRadius: 2,
                        textTransform: 'none',
                        py: 0.25,
                        '&:hover': { bgcolor: '#FFC107' },
                      }}
                    >
                      Claim
                    </Button>
                  )}
                </Box>
              </Box>
            );
          })}
        </Box>
      </CardContent>
    </Card>
  );
}

export default MissionsPanel;
