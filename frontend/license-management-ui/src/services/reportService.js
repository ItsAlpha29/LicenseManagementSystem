import api from './api';

export const reportService = {
  async getLicenseUtilization(params = {}) {
    return await api.get('/reports/license-utilization', { params });
  },

  async getExpiryReport(params = {}) {
    return await api.get('/reports/expiry', { params });
  },

  async getEmployeeLicensesReport(params = {}) {
    return await api.get('/reports/employee-licenses', { params });
  },

  async getCostReport(params = {}) {
    return await api.get('/reports/cost', { params });
  },

  async getRenewalsReport(params = {}) {
    return await api.get('/reports/renewals', { params });
  },

  async getDepartmentReport(params = {}) {
    return await api.get('/reports/department', { params });
  },

  async exportCsv(reportType, params = {}) {
    const token = localStorage.getItem('token');
    const query = new URLSearchParams({ ...params, reportType }).toString();
    const url = `${import.meta.env.VITE_API_URL || 'http://localhost:5000/api'}/reports/export/csv?${query}`;

    const response = await fetch(url, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to export CSV report.');
    }

    const blob = await response.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = downloadUrl;
    a.download = `${reportType.toLowerCase()}-report-${new Date().toISOString().split('T')[0]}.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(downloadUrl);
  },
};
