import api from './api';

export const assignmentService = {
  async getAssignments(params = {}) {
    return await api.get('/assignments', { params });
  },

  async getAssignmentById(id) {
    return await api.get(`/assignments/${id}`);
  },

  async assignLicense(data) {
    return await api.post('/assignments', data);
  },

  async revokeLicense(id, data) {
    return await api.post(`/assignments/${id}/revoke`, data);
  },

  async getMyLicenses() {
    return await api.get('/my/licenses');
  },
};
