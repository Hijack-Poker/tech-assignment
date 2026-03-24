import { Component, type ReactNode, type ErrorInfo } from 'react';
import { Box, Typography, Button } from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';

interface Props {
  children: ReactNode;
  fallbackMessage?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            p: 3,
            borderRadius: 4,
            bgcolor: '#1A1D27',
            border: '1px solid #2A2D3A',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 1.5,
            textAlign: 'center',
          }}
        >
          <ErrorOutlineIcon sx={{ fontSize: 36, color: '#EF5350' }} />
          <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: 14 }}>
            {this.props.fallbackMessage || 'Something went wrong'}
          </Typography>
          <Typography sx={{ color: '#8B8FA3', fontSize: 12 }}>
            {this.state.error?.message}
          </Typography>
          <Button
            size="small"
            onClick={() => this.setState({ hasError: false, error: null })}
            sx={{ color: '#60A5FA', textTransform: 'none', fontSize: 12 }}
          >
            Try Again
          </Button>
        </Box>
      );
    }
    return this.props.children;
  }
}

export default ErrorBoundary;
