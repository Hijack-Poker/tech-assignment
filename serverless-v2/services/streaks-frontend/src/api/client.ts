import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

const apiClient = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000,
});

// Attach JWT auth token and player ID header to all requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers['Authorization'] = `Bearer ${token}`;
  }

  const playerId = localStorage.getItem('playerId');
  if (playerId) {
    config.headers['X-Player-Id'] = playerId;
  }
  return config;
});

// On 401, clear stale auth and force re-login
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('playerId');
      localStorage.removeItem('displayName');
      window.location.reload();
    }
    return Promise.reject(error);
  }
);

export default apiClient;
