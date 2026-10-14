import api from './api';

export const aiService = {
  async chat(message, conversationHistory = []) {
    return await api.post('/ai/chat', { message, conversationHistory });
  },

  async confirmAction(confirmationToken, actionType, parameters) {
    return await api.post('/ai/action/confirm', {
      confirmationToken,
      actionType,
      parameters,
    });
  },

  async getEmployeeRecommendations(employeeId) {
    return await api.get(`/ai/recommendations/employee/${employeeId}`);
  },

  async getRoleRecommendations(department, designation, employeeId = null) {
    return await api.post('/ai/recommendations/role', {
      department,
      designation,
      employeeId,
    });
  },

  async checkDuplicateLicense(data) {
    return await api.post('/ai/duplicate-check', data);
  },

  async scanPortfolioRedundancy() {
    return await api.get('/ai/redundancy-scan');
  },
};
