import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Container, Typography, Grid, Card, CardContent,
  CircularProgress, IconButton, Chip, Button,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PeopleIcon from '@mui/icons-material/People';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';
import apiClient from '../api/client';

interface Analytics {
  totalPlayers: number;
  engagement: { dau: number; wau: number; mau: number };
  streaks: { activeLoginStreaks: number; activePlayStreaks: number; avgLoginStreak: number; avgPlayStreak: number };
  retention: { d1: number; d7: number; d30: number };
  tiers: { bronze: number; silver: number; gold: number; platinum: number };
  topPlayers: Array<{ displayName: string; loginStreak: number; playStreak: number; score: number }>;
  churnRisk: number;
}

const TIER_COLORS = { bronze: '#CD7F32', silver: '#C0C0C0', gold: '#FFD700', platinum: '#E5E4E2' };

function StatCard({ title, value, subtitle, icon, color }: { title: string; value: string | number; subtitle?: string; icon: React.ReactNode; color: string }) {
  return (
    <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
      <CardContent sx={{ p: 3 }}>
        <Box display="flex" alignItems="center" gap={1.5}>
          <Box sx={{ p: 1, borderRadius: 2, bgcolor: `${color}15` }}>
            {icon}
          </Box>
          <Box>
            <Typography sx={{ fontSize: 11, color: '#8B8FA3', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
              {title}
            </Typography>
            <Typography sx={{ fontSize: 24, fontWeight: 800, color: '#fff' }}>
              {value}
            </Typography>
            {subtitle && (
              <Typography sx={{ fontSize: 11, color: '#8B8FA3' }}>{subtitle}</Typography>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

function AdminDashboard() {
  const navigate = useNavigate();
  const [data, setData] = useState<Analytics | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient.get('/admin/analytics')
      .then((res) => setData(res.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <Box display="flex" alignItems="center" justifyContent="center" minHeight="100vh" bgcolor="#0F1117">
        <CircularProgress sx={{ color: '#FF6B35' }} />
      </Box>
    );
  }

  if (!data) return null;

  const retentionD1Pct = data.totalPlayers > 0 ? ((data.retention.d1 / data.totalPlayers) * 100).toFixed(0) : 0;
  const retentionD7Pct = data.totalPlayers > 0 ? ((data.retention.d7 / data.totalPlayers) * 100).toFixed(0) : 0;
  const retentionD30Pct = data.totalPlayers > 0 ? ((data.retention.d30 / data.totalPlayers) * 100).toFixed(0) : 0;

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#0F1117', py: 4, px: { xs: 2, md: 6 } }}>
      <Container maxWidth="xl" disableGutters>
        {/* Header */}
        <Box display="flex" alignItems="center" gap={2} mb={4}>
          <IconButton onClick={() => navigate('/')} sx={{ color: '#90CAF9' }}>
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h4" fontWeight={700} color="#fff">
            Admin Analytics
          </Typography>
          <Chip label="Live" size="small" sx={{ bgcolor: '#1B5E20', color: '#4ADE80', fontWeight: 700, fontSize: 11 }} />
          <Button
            onClick={() => navigate('/admin/players')}
            variant="outlined"
            size="small"
            sx={{ ml: 'auto', borderColor: '#2A2D3A', color: '#90CAF9', textTransform: 'none', fontWeight: 600, borderRadius: 2 }}
          >
            Player History
          </Button>
        </Box>

        {/* KPI Cards */}
        <Grid container spacing={3} mb={3}>
          <Grid item xs={6} md={3}>
            <StatCard title="Total Players" value={data.totalPlayers} icon={<PeopleIcon sx={{ color: '#60A5FA' }} />} color="#60A5FA" />
          </Grid>
          <Grid item xs={6} md={3}>
            <StatCard title="DAU" value={data.engagement.dau} subtitle={`WAU: ${data.engagement.wau} \u00B7 MAU: ${data.engagement.mau}`} icon={<TrendingUpIcon sx={{ color: '#4ADE80' }} />} color="#4ADE80" />
          </Grid>
          <Grid item xs={6} md={3}>
            <StatCard title="Avg Login Streak" value={data.streaks.avgLoginStreak} subtitle={`${data.streaks.activeLoginStreaks} active`} icon={<TrendingUpIcon sx={{ color: '#FFD700' }} />} color="#FFD700" />
          </Grid>
          <Grid item xs={6} md={3}>
            <StatCard title="Churn Risk" value={data.churnRisk} subtitle="Players at risk of losing streak" icon={<WarningAmberIcon sx={{ color: '#EF5350' }} />} color="#EF5350" />
          </Grid>
        </Grid>

        <Grid container spacing={3}>
          {/* Retention */}
          <Grid item xs={12} md={4}>
            <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} color="#fff" mb={2}>Retention</Typography>
                {[
                  { label: 'D1 (2+ day streak)', pct: retentionD1Pct, count: data.retention.d1, color: '#4ADE80' },
                  { label: 'D7 (7+ day best)', pct: retentionD7Pct, count: data.retention.d7, color: '#60A5FA' },
                  { label: 'D30 (30+ day best)', pct: retentionD30Pct, count: data.retention.d30, color: '#FFD700' },
                ].map((r) => (
                  <Box key={r.label} mb={2}>
                    <Box display="flex" justifyContent="space-between" mb={0.5}>
                      <Typography sx={{ fontSize: 12, color: '#8B8FA3' }}>{r.label}</Typography>
                      <Typography sx={{ fontSize: 12, fontWeight: 700, color: r.color }}>{r.pct}% ({r.count})</Typography>
                    </Box>
                    <Box sx={{ height: 6, borderRadius: 3, bgcolor: '#2A2D3A', overflow: 'hidden' }}>
                      <Box sx={{ height: '100%', width: `${r.pct}%`, bgcolor: r.color, borderRadius: 3, transition: 'width 0.5s ease' }} />
                    </Box>
                  </Box>
                ))}
              </CardContent>
            </Card>
          </Grid>

          {/* Tier Distribution */}
          <Grid item xs={12} md={4}>
            <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} color="#fff" mb={2}>Tier Distribution</Typography>
                {(Object.entries(data.tiers) as [string, number][]).reverse().map(([tier, count]) => (
                  <Box key={tier} display="flex" alignItems="center" justifyContent="space-between" mb={1.5}>
                    <Box display="flex" alignItems="center" gap={1}>
                      <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: TIER_COLORS[tier as keyof typeof TIER_COLORS] }} />
                      <Typography sx={{ fontSize: 13, fontWeight: 600, color: '#fff', textTransform: 'capitalize' }}>{tier}</Typography>
                    </Box>
                    <Chip label={count} size="small" sx={{ bgcolor: '#2A2D3A', color: '#fff', fontWeight: 700, fontSize: 12 }} />
                  </Box>
                ))}
              </CardContent>
            </Card>
          </Grid>

          {/* Top Players */}
          <Grid item xs={12} md={4}>
            <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
              <CardContent sx={{ p: 3 }}>
                <Box display="flex" alignItems="center" gap={1} mb={2}>
                  <EmojiEventsIcon sx={{ color: '#FFD700' }} />
                  <Typography variant="h6" fontWeight={700} color="#fff">Top Players</Typography>
                </Box>
                {data.topPlayers.map((p, i) => (
                  <Box key={i} display="flex" alignItems="center" justifyContent="space-between" mb={1}>
                    <Box display="flex" alignItems="center" gap={1}>
                      <Typography sx={{ fontSize: 14, fontWeight: 800, color: i < 3 ? '#FFD700' : '#8B8FA3', width: 20 }}>
                        {i + 1}.
                      </Typography>
                      <Typography sx={{ fontSize: 13, color: '#fff' }}>{p.displayName}</Typography>
                    </Box>
                    <Typography sx={{ fontSize: 12, fontWeight: 700, color: '#FFD700' }}>{p.score} pts</Typography>
                  </Box>
                ))}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Container>
    </Box>
  );
}

export default AdminDashboard;
