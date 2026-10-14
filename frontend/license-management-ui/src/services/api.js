import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach JWT Bearer Token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Format error responses uniformly & handle 401
api.interceptors.response.use(
  (response) => {
    // If backend wrapped in ApiResponse<T>, unwrap data where appropriate
    if (response.data && response.data.hasOwnProperty('success')) {
      return response.data;
    }
    return { success: true, data: response.data };
  },
  (error) => {
    if (error.response) {
      if (error.response.status === 401) {
        // Clear expired token if unauthorized
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
      }

      const backendMessage = error.response.data?.message || error.response.data?.title || 'An error occurred processing your request.';
      const errors = error.response.data?.errors || [];

      return Promise.reject({
        status: error.response.status,
        message: backendMessage,
        errors: Array.isArray(errors) ? errors : Object.values(errors).flat(),
      });
    }

    return Promise.reject({
      status: 0,
      message: 'Unable to connect to the server. Please ensure the backend is running.',
      errors: [],
    });
  }
);

export default api;
