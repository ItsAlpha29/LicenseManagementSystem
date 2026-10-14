import api from './api';

export const dashboardService = {
  async getFullDashboard() {
    return await api.get('/dashboard/all');
  },

  async getSummary() {
    return await api.get('/dashboard/summary');
  },

  async getUtilization() {
    return await api.get('/dashboard/license-utilization');
  },

  async getCostTrend() {
    return await api.get('/dashboard/cost');
  },

  async getExpiryTimeline() {
    return await api.get('/dashboard/expiry');
  },

  async getRecentActivities(limit = 10) {
    return await api.get('/dashboard/recent-activities', { params: { limit } });
  },
};
