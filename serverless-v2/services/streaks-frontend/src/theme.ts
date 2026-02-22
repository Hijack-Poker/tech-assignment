import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#FF9800' },
    secondary: { main: '#4CAF50' },
    background: {
      default: '#0D1117',
      paper: '#161B22',
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  },
});
