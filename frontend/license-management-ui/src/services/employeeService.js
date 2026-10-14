import api from './api';

export const employeeService = {
  async getEmployees(params = {}) {
    return await api.get('/employees', { params });
  },

  async getEmployeeById(id) {
    return await api.get(`/employees/${id}`);
  },

  async createEmployee(data) {
    return await api.post('/employees', data);
  },

  async updateEmployee(id, data) {
    return await api.put(`/employees/${id}`, data);
  },

  async updateStatus(id, status) {
    return await api.patch(`/employees/${id}/status`, { status });
  },

  async deleteEmployee(id) {
    return await api.delete(`/employees/${id}`);
  },

  async getDepartments() {
    return await api.get('/employees/departments');
  },
};
