import api from './api';

export const auditService = {
  async getAuditLogs(params = {}) {
    return await api.get('/audit', { params });
  },
};
