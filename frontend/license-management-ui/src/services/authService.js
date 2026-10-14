import api from './api';

export const authService = {
  async login(email, password) {
    const res = await api.post('/auth/login', { email, password });
    if (res.success && res.data) {
      localStorage.setItem('token', res.data.token);
      localStorage.setItem('user', JSON.stringify(res.data.user));
    }
    return res;
  },

  async getMe() {
    return await api.get('/auth/me');
  },

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },

  getCurrentUser() {
    const userStr = localStorage.getItem('user');
    try {
      return userStr ? JSON.parse(userStr) : null;
    } catch {
      return null;
    }
  },

  isAuthenticated() {
    return !!localStorage.getItem('token');
  },

  async updateProfile(data) {
    const res = await api.put('/profile', data);
    if (res.success && res.data) {
      const user = authService.getCurrentUser();
      const updated = { ...user, ...res.data };
      localStorage.setItem('user', JSON.stringify(updated));
    }
    return res;
  },
};
