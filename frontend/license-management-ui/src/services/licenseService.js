import api from './api';

export const licenseService = {
  async getLicenses(params = {}) {
    return await api.get('/licenses', { params });
  },

  async getLicenseById(id) {
    return await api.get(`/licenses/${id}`);
  },

  async createLicense(data) {
    return await api.post('/licenses', data);
  },

  async updateLicense(id, data) {
    return await api.put(`/licenses/${id}`, data);
  },

  async deleteLicense(id) {
    return await api.delete(`/licenses/${id}`);
  },

  async getVendors() {
    return await api.get('/licenses/vendors');
  },

  async getLicenseTypes() {
    return await api.get('/licenses/types');
  },
};
