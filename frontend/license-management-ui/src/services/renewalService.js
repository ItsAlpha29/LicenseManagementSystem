import api from './api';

export const renewalService = {
  async getRenewals(params = {}) {
    return await api.get('/renewals', { params });
  },

  async getRenewalById(id) {
    return await api.get(`/renewals/${id}`);
  },

  async processRenewal(data) {
    return await api.post('/renewals', data);
  },
};
