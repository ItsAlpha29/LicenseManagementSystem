import api from './api';

export const notificationService = {
  async getNotifications() {
    return await api.get('/notifications');
  },

  async markAsRead(id) {
    return await api.patch(`/notifications/${id}/read`);
  },

  async markAllAsRead() {
    return await api.post('/notifications/read-all');
  },
};
