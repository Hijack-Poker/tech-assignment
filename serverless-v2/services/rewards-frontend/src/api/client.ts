import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const apiClient = axios.create({
  baseURL: `${API_URL}/local/api/v1`,
  headers: { 'Content-Type': 'application/json' },
});

// Add player ID header to all requests
apiClient.interceptors.request.use((config) => {
  const playerId = localStorage.getItem('playerId');
  if (playerId) {
    config.headers['X-Player-Id'] = playerId;
  }
  return config;
});

export default apiClient;
