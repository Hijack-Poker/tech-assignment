import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Container, Typography, TextField, Button, Card, CardContent,
  CircularProgress, IconButton, Chip, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Alert, Grid,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import SearchIcon from '@mui/icons-material/Search';
import LocalFireDepartmentIcon from '@mui/icons-material/LocalFireDepartment';
import AcUnitIcon from '@mui/icons-material/AcUnit';
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents';
import { getPlayerHistory } from '../api/streaks.api';
import type { PlayerHistory } from '../types/streaks.types';

const ACTIVITY_COLORS: Record<string, string> = {
  played: '#2E7D32',
  login_only: '#A5D6A7',
  freeze: '#42A5F5',
  streak_broken: '#EF5350',
  none: '#2A2D3A',
};

function getActivityLabel(record: PlayerHistory['activity'][0]): string {
  if (record.freezeUsed) return 'freeze';
  if (record.streakBroken) return 'streak_broken';
  if (record.played) return 'played';
  if (record.loggedIn) return 'login_only';
  return 'none';
}

function AdminPlayerHistory() {
  const navigate = useNavigate();
  const [searchId, setSearchId] = useState('');
  const [data, setData] = useState<PlayerHistory | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSearch = async () => {
    const id = searchId.trim();
    if (!id) return;
    setLoading(true);
    setError('');
    setData(null);
    try {
      const result = await getPlayerHistory(id);
      setData(result);
    } catch (err: unknown) {
      const isNotFound = err instanceof Error && 'response' in err &&
        (err as { response?: { status?: number } }).response?.status === 404;
      setError(isNotFound ? 'Player not found' : 'Failed to fetch player data');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#0F1117', py: 4, px: { xs: 2, md: 6 } }}>
      <Container maxWidth="xl" disableGutters>
        {/* Header */}
        <Box display="flex" alignItems="center" gap={2} mb={4}>
          <IconButton onClick={() => navigate('/admin')} sx={{ color: '#90CAF9' }}>
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h4" fontWeight={700} color="#fff">
            Player History
          </Typography>
        </Box>

        {/* Search */}
        <Box display="flex" gap={2} mb={4}>
          <TextField
            placeholder="Enter Player ID (e.g. player-42)"
            value={searchId}
            onChange={(e) => setSearchId(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
            fullWidth
            sx={{
              '& .MuiOutlinedInput-root': {
                bgcolor: '#1A1D27', color: '#fff', borderRadius: 3,
                '& fieldset': { borderColor: '#2A2D3A' },
                '&:hover fieldset': { borderColor: '#FF6B35' },
              },
            }}
          />
          <Button
            variant="contained"
            onClick={handleSearch}
            disabled={loading}
            startIcon={loading ? <CircularProgress size={18} /> : <SearchIcon />}
            sx={{ bgcolor: '#FF6B35', px: 4, borderRadius: 3, textTransform: 'none', fontWeight: 700, '&:hover': { bgcolor: '#E55A2B' } }}
          >
            Search
          </Button>
        </Box>

        {error && <Alert severity="error" sx={{ mb: 3, bgcolor: '#2A1215', color: '#EF5350' }}>{error}</Alert>}

        {data && (
          <>
            {/* Player Profile Card */}
            <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4, mb: 3 }}>
              <CardContent sx={{ p: 3 }}>
                <Box display="flex" alignItems="center" gap={2} mb={2}>
                  <Typography variant="h5" fontWeight={700} color="#fff">
                    {data.player.displayName}
                  </Typography>
                  <Chip label={data.player.playerId} size="small" sx={{ bgcolor: '#2A2D3A', color: '#8B8FA3', fontFamily: 'monospace' }} />
                  {data.player.selfExcludedUntil && (
                    <Chip label={`Excluded until ${data.player.selfExcludedUntil}`} size="small" sx={{ bgcolor: '#2A1215', color: '#EF5350' }} />
                  )}
                </Box>

                <Grid container spacing={3}>
                  {[
                    { label: 'Login Streak', value: data.player.loginStreak, best: data.player.bestLoginStreak, icon: <LocalFireDepartmentIcon sx={{ color: '#FF6B35' }} />, color: '#FF6B35' },
                    { label: 'Play Streak', value: data.player.playStreak, best: data.player.bestPlayStreak, icon: <LocalFireDepartmentIcon sx={{ color: '#9C27B0' }} />, color: '#9C27B0' },
                    { label: 'Freezes', value: data.player.freezesAvailable, best: null, icon: <AcUnitIcon sx={{ color: '#42A5F5' }} />, color: '#42A5F5' },
                    { label: 'Rewards', value: data.rewards.length, best: null, icon: <EmojiEventsIcon sx={{ color: '#FFD700' }} />, color: '#FFD700' },
                  ].map((s) => (
                    <Grid item xs={6} md={3} key={s.label}>
                      <Box display="flex" alignItems="center" gap={1.5}>
                        {s.icon}
                        <Box>
                          <Typography sx={{ fontSize: 11, color: '#8B8FA3', fontWeight: 600, textTransform: 'uppercase' }}>{s.label}</Typography>
                          <Typography sx={{ fontSize: 22, fontWeight: 800, color: '#fff' }}>
                            {s.value}
                            {s.best !== null && <Typography component="span" sx={{ fontSize: 12, color: '#8B8FA3', ml: 1 }}>best: {s.best}</Typography>}
                          </Typography>
                        </Box>
                      </Box>
                    </Grid>
                  ))}
                </Grid>

                <Box mt={2} display="flex" gap={3}>
                  <Typography sx={{ fontSize: 12, color: '#8B8FA3' }}>
                    Last login: <span style={{ color: '#fff' }}>{data.player.lastLoginDate || 'never'}</span>
                  </Typography>
                  <Typography sx={{ fontSize: 12, color: '#8B8FA3' }}>
                    Last play: <span style={{ color: '#fff' }}>{data.player.lastPlayDate || 'never'}</span>
                  </Typography>
                </Box>
              </CardContent>
            </Card>

            {/* Activity Table */}
            <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4, mb: 3 }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} color="#fff" mb={2}>
                  Activity (Last 90 Days) — {data.activity.length} records
                </Typography>
                <TableContainer sx={{ maxHeight: 400 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        {['Date', 'Activity', 'Login Streak', 'Play Streak', 'Freeze', 'Broken'].map((h) => (
                          <TableCell key={h} sx={{ bgcolor: '#0F1117', color: '#8B8FA3', fontWeight: 700, borderBottom: '1px solid #2A2D3A' }}>{h}</TableCell>
                        ))}
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {data.activity.map((a) => {
                        const label = getActivityLabel(a);
                        return (
                          <TableRow key={a.date} sx={{ '&:hover': { bgcolor: '#1E2130' } }}>
                            <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27', fontFamily: 'monospace' }}>{a.date}</TableCell>
                            <TableCell sx={{ borderBottom: '1px solid #1A1D27' }}>
                              <Chip label={label} size="small" sx={{ bgcolor: `${ACTIVITY_COLORS[label]}25`, color: ACTIVITY_COLORS[label], fontWeight: 700, fontSize: 11 }} />
                            </TableCell>
                            <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27' }}>{a.loginStreakAtDay}</TableCell>
                            <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27' }}>{a.playStreakAtDay}</TableCell>
                            <TableCell sx={{ borderBottom: '1px solid #1A1D27' }}>
                              {a.freezeUsed && <AcUnitIcon sx={{ color: '#42A5F5', fontSize: 18 }} />}
                            </TableCell>
                            <TableCell sx={{ borderBottom: '1px solid #1A1D27' }}>
                              {a.streakBroken && <Chip label="RESET" size="small" sx={{ bgcolor: '#2A1215', color: '#EF5350', fontWeight: 700, fontSize: 10 }} />}
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>

            <Grid container spacing={3}>
              {/* Rewards */}
              <Grid item xs={12} md={6}>
                <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
                  <CardContent sx={{ p: 3 }}>
                    <Typography variant="h6" fontWeight={700} color="#fff" mb={2}>
                      Rewards ({data.rewards.length})
                    </Typography>
                    <TableContainer sx={{ maxHeight: 300 }}>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            {['Date', 'Type', 'Milestone', 'Points'].map((h) => (
                              <TableCell key={h} sx={{ color: '#8B8FA3', fontWeight: 700, borderBottom: '1px solid #2A2D3A' }}>{h}</TableCell>
                            ))}
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {data.rewards.map((r) => (
                            <TableRow key={r.rewardId}>
                              <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27', fontFamily: 'monospace', fontSize: 12 }}>{r.createdAt?.slice(0, 10)}</TableCell>
                              <TableCell sx={{ borderBottom: '1px solid #1A1D27' }}>
                                <Chip label={r.type} size="small" sx={{ bgcolor: r.type.includes('login') ? '#FF6B3525' : '#9C27B025', color: r.type.includes('login') ? '#FF6B35' : '#9C27B0', fontWeight: 700, fontSize: 10 }} />
                              </TableCell>
                              <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27' }}>{r.milestone}d</TableCell>
                              <TableCell sx={{ color: '#FFD700', fontWeight: 700, borderBottom: '1px solid #1A1D27' }}>+{r.points}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </CardContent>
                </Card>
              </Grid>

              {/* Freeze History */}
              <Grid item xs={12} md={6}>
                <Card sx={{ bgcolor: '#1A1D27', border: '1px solid #2A2D3A', borderRadius: 4 }}>
                  <CardContent sx={{ p: 3 }}>
                    <Typography variant="h6" fontWeight={700} color="#fff" mb={2}>
                      Freeze History ({data.freezeHistory.length})
                    </Typography>
                    <TableContainer sx={{ maxHeight: 300 }}>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            {['Date', 'Source'].map((h) => (
                              <TableCell key={h} sx={{ color: '#8B8FA3', fontWeight: 700, borderBottom: '1px solid #2A2D3A' }}>{h}</TableCell>
                            ))}
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {data.freezeHistory.map((f) => (
                            <TableRow key={f.date}>
                              <TableCell sx={{ color: '#fff', borderBottom: '1px solid #1A1D27', fontFamily: 'monospace' }}>{f.date}</TableCell>
                              <TableCell sx={{ borderBottom: '1px solid #1A1D27' }}>
                                <Chip label={f.source} size="small" sx={{ bgcolor: '#42A5F525', color: '#42A5F5', fontWeight: 700, fontSize: 11 }} />
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </>
        )}
      </Container>
    </Box>
  );
}

export default AdminPlayerHistory;
