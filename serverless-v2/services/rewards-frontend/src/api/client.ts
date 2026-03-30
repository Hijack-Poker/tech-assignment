import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || '';

const apiClient = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
});

// Add player ID header to all requests
apiClient.interceptors.request.use((config) => {
  if (!config.headers['X-Player-Id']) {
    const playerId = localStorage.getItem('playerId');
    if (playerId) {
      config.headers['X-Player-Id'] = playerId;
    }
  }
  return config;
});

export default apiClient;
