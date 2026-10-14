import React, { useState, useEffect } from 'react';
import {
  Users,
  UserPlus,
  Edit2,
  Trash2,
  Eye,
  Power,
  Shield,
  Building,
  Mail,
  Phone,
  Calendar,
} from 'lucide-react';
import { employeeService } from '../services/employeeService';
import { DataTable } from '../components/DataTable';
import { SearchBar } from '../components/SearchBar';
import { FilterBar } from '../components/FilterBar';
import { StatusBadge } from '../components/StatusBadge';
import { Modal } from '../components/Modal';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { FormField } from '../components/FormField';
import { useToast } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

export const EmployeesPage = () => {
  const [employees, setEmployees] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Filter & Pagination state
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ department: 'All', role: 'All', status: 'All' });
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('name');
  const [sortDesc, setSortDesc] = useState(false);

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isStatusDialogOpen, setIsStatusDialogOpen] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState(null);
  const [saving, setSaving] = useState(false);

  // Form State
  const initialForm = {
    employeeCode: '',
    employeeName: '',
    email: '',
    phone: '',
    department: 'Engineering',
    designation: '',
    role: 'EMPLOYEE',
    managerId: '',
    joiningDate: new Date().toISOString().split('T')[0],
    status: 'Active',
    initialPassword: 'Employee@123',
  };
  const [formData, setFormData] = useState(initialForm);

  const toast = useToast();
  const { role } = useAuth();
  const navigate = useNavigate();
  const isAdmin = role === 'ADMIN';

  const fetchEmployees = async () => {
    try {
      setLoading(true);
      const res = await employeeService.getEmployees({
        search,
        department: filters.department,
        role: filters.role,
        status: filters.status,
        pageNumber,
        pageSize,
        sortBy,
        sortDesc,
      });

      if (res.success && res.data) {
        setEmployees(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load employees.');
    } finally {
      setLoading(false);
    }
  };

  const fetchDepartments = async () => {
    try {
      const res = await employeeService.getDepartments();
      if (res.success && res.data) {
        setDepartments(res.data);
      }
    } catch {
      // fallback default departments
      setDepartments(['Engineering', 'Information Technology', 'Design & UX', 'Business Intelligence', 'Marketing', 'Human Resources', 'Finance']);
    }
  };

  useEffect(() => {
    fetchDepartments();
  }, []);

  useEffect(() => {
    fetchEmployees();
  }, [search, filters, pageNumber, pageSize, sortBy, sortDesc]);

  const handleFilterChange = (key, value) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPageNumber(1);
  };

  const handleResetFilters = () => {
    setSearch('');
    setFilters({ department: 'All', role: 'All', status: 'All' });
    setPageNumber(1);
  };

  const handleOpenAdd = () => {
    setFormData(initialForm);
    setIsAddModalOpen(true);
  };

  const handleOpenEdit = (emp) => {
    setSelectedEmployee(emp);
    setFormData({
      employeeName: emp.employeeName,
      phone: emp.phone || '',
      department: emp.department,
      designation: emp.designation,
      role: emp.role,
      managerId: emp.managerId || '',
      joiningDate: emp.joiningDate ? emp.joiningDate.split('T')[0] : '',
      status: emp.status,
    });
    setIsEditModalOpen(true);
  };

  const handleSaveAdd = async (e) => {
    e.preventDefault();
    if (!formData.employeeCode || !formData.employeeName || !formData.email) {
      toast.error('Please fill in required fields.');
      return;
    }

    setSaving(true);
    try {
      const payload = {
        ...formData,
        managerId: formData.managerId ? Number(formData.managerId) : null,
      };
      const res = await employeeService.createEmployee(payload);
      if (res.success) {
        toast.success(`Employee ${formData.employeeName} created successfully.`);
        setIsAddModalOpen(false);
        fetchEmployees();
        fetchDepartments();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to create employee.');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveEdit = async (e) => {
    e.preventDefault();
    if (!formData.employeeName) {
      toast.error('Employee name is required.');
      return;
    }

    setSaving(true);
    try {
      const payload = {
        ...formData,
        managerId: formData.managerId ? Number(formData.managerId) : null,
      };
      const res = await employeeService.updateEmployee(selectedEmployee.id, payload);
      if (res.success) {
        toast.success(`Employee updated successfully.`);
        setIsEditModalOpen(false);
        fetchEmployees();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to update employee.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleStatus = async () => {
    if (!selectedEmployee) return;
    const newStatus = selectedEmployee.status === 'Active' ? 'Inactive' : 'Active';
    setSaving(true);
    try {
      const res = await employeeService.updateStatus(selectedEmployee.id, newStatus);
      if (res.success) {
        toast.success(`Employee status set to ${newStatus}.`);
        setIsStatusDialogOpen(false);
        fetchEmployees();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to change employee status.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedEmployee) return;
    setSaving(true);
    try {
      const res = await employeeService.deleteEmployee(selectedEmployee.id);
      if (res.success) {
        toast.success('Employee deleted successfully.');
        setIsDeleteDialogOpen(false);
        fetchEmployees();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to delete employee.');
    } finally {
      setSaving(false);
    }
  };

  const columns = [
    {
      header: 'Employee Code',
      key: 'employeeCode',
      sortable: true,
      render: (row) => (
        <span className="font-mono font-bold text-slate-900 bg-slate-100 px-2 py-0.5 rounded-md text-xs">
          {row.employeeCode}
        </span>
      ),
    },
    {
      header: 'Name & Contact',
      key: 'employeeName',
      sortable: true,
      render: (row) => (
        <div>
          <div className="font-bold text-slate-900">{row.employeeName}</div>
          <div className="text-xs text-slate-400">{row.email}</div>
        </div>
      ),
    },
    {
      header: 'Department',
      key: 'department',
      sortable: true,
      render: (row) => (
        <div>
          <div className="font-medium text-slate-800">{row.department}</div>
          <div className="text-[11px] text-slate-400">{row.designation}</div>
        </div>
      ),
    },
    {
      header: 'System Role',
      key: 'role',
      sortable: true,
      render: (row) => <StatusBadge status={row.role} size="xs" />,
    },
    {
      header: 'Active Licenses',
      key: 'activeLicensesCount',
      align: 'center',
      render: (row) => (
        <span className={`inline-flex items-center justify-center px-2.5 py-0.5 rounded-full text-xs font-bold ${
          row.activeLicensesCount > 0 ? 'bg-indigo-50 text-indigo-700' : 'bg-slate-100 text-slate-500'
        }`}>
          {row.activeLicensesCount}
        </span>
      ),
    },
    {
      header: 'Status',
      key: 'status',
      sortable: true,
      render: (row) => <StatusBadge status={row.status} size="xs" />,
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (row) => (
        <div className="flex items-center justify-end gap-1.5">
          <button
            onClick={() => navigate(`/employees/${row.id}`)}
            className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-slate-100 rounded-lg transition"
            title="View Details & Licenses"
          >
            <Eye className="w-4 h-4" />
          </button>

          {isAdmin && (
            <>
              <button
                onClick={() => handleOpenEdit(row)}
                className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-slate-100 rounded-lg transition"
                title="Edit Employee"
              >
                <Edit2 className="w-4 h-4" />
              </button>

              <button
                onClick={() => {
                  setSelectedEmployee(row);
                  setIsStatusDialogOpen(true);
                }}
                className={`p-1.5 rounded-lg transition ${
                  row.status === 'Active'
                    ? 'text-slate-400 hover:text-amber-600 hover:bg-amber-50'
                    : 'text-slate-400 hover:text-emerald-600 hover:bg-emerald-50'
                }`}
                title={row.status === 'Active' ? 'Deactivate Employee' : 'Activate Employee'}
              >
                <Power className="w-4 h-4" />
              </button>

              <button
                onClick={() => {
                  setSelectedEmployee(row);
                  setIsDeleteDialogOpen(true);
                }}
                className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
                title="Delete Employee"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header & Add Button */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight">Employee Directory</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Manage organizational staff, license entitlements, and role authorizations
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={handleOpenAdd}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
          >
            <UserPlus className="w-4 h-4" />
            Add Employee
          </button>
        )}
      </div>

      {/* Search & Filter Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
          <div className="md:col-span-5">
            <SearchBar
              value={search}
              onChange={setSearch}
              placeholder="Search by name, code, email, designation..."
            />
          </div>
          <div className="md:col-span-7">
            <FilterBar
              filters={[
                { key: 'department', label: 'Department', options: departments },
                { key: 'role', label: 'Role', options: ['ADMIN', 'IT_MANAGER', 'EMPLOYEE'] },
                { key: 'status', label: 'Status', options: ['Active', 'Inactive', 'OnLeave'] },
              ]}
              values={filters}
              onChange={handleFilterChange}
              onReset={handleResetFilters}
            />
          </div>
        </div>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={employees}
        loading={loading}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={setPageSize}
        sortBy={sortBy}
        sortDesc={sortDesc}
        onSort={(col, desc) => {
          setSortBy(col);
          setSortDesc(desc);
        }}
        emptyTitle="No employees found"
        emptyDescription="Try adjusting search terms or clear filters to view staff members."
      />

      {/* Add Employee Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Add New Employee"
        subtitle="Create an employee record and corresponding system login credentials"
        maxWidth="max-w-2xl"
      >
        <form onSubmit={handleSaveAdd} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              label="Employee Code"
              required
              placeholder="e.g. EMP007"
              value={formData.employeeCode}
              onChange={(e) => setFormData({ ...formData, employeeCode: e.target.value })}
            />
            <FormField
              label="Full Name"
              required
              placeholder="e.g. John Doe"
              value={formData.employeeName}
              onChange={(e) => setFormData({ ...formData, employeeName: e.target.value })}
            />
            <FormField
              label="Work Email"
              type="email"
              required
              placeholder="john.doe@enterprise.com"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            />
            <FormField
              label="Phone Number"
              placeholder="+1 (555) 000-0000"
              value={formData.phone}
              onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
            />
            <FormField
              label="Department"
              type="select"
              required
              options={[
                'Engineering',
                'Information Technology',
                'Design & UX',
                'Business Intelligence',
                'Marketing',
                'Human Resources',
                'Finance',
              ]}
              value={formData.department}
              onChange={(e) => setFormData({ ...formData, department: e.target.value })}
            />
            <FormField
              label="Designation / Title"
              required
              placeholder="e.g. Senior Software Engineer"
              value={formData.designation}
              onChange={(e) => setFormData({ ...formData, designation: e.target.value })}
            />
            <FormField
              label="System Role"
              type="select"
              required
              options={[
                { value: 'EMPLOYEE', label: 'Employee (Read-Only Portal)' },
                { value: 'IT_MANAGER', label: 'IT Manager (License Operations)' },
                { value: 'ADMIN', label: 'Admin (Full System Administration)' },
              ]}
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value })}
            />
            <FormField
              label="Joining Date"
              type="date"
              value={formData.joiningDate}
              onChange={(e) => setFormData({ ...formData, joiningDate: e.target.value })}
            />
            <FormField
              label="Initial Password"
              placeholder="Defaults to Employee@123"
              value={formData.initialPassword}
              onChange={(e) => setFormData({ ...formData, initialPassword: e.target.value })}
            />
            <FormField
              label="Status"
              type="select"
              options={['Active', 'Inactive', 'OnLeave']}
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: e.target.value })}
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsAddModalOpen(false)}
              className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition disabled:opacity-50"
            >
              {saving ? 'Creating...' : 'Create Employee'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Edit Employee Modal */}
      <Modal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        title="Edit Employee Information"
        subtitle={`Updating profile for ${selectedEmployee?.employeeName} (${selectedEmployee?.employeeCode})`}
        maxWidth="max-w-2xl"
      >
        <form onSubmit={handleSaveEdit} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              label="Full Name"
              required
              value={formData.employeeName}
              onChange={(e) => setFormData({ ...formData, employeeName: e.target.value })}
            />
            <FormField
              label="Phone Number"
              value={formData.phone}
              onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
            />
            <FormField
              label="Department"
              type="select"
              required
              options={[
                'Engineering',
                'Information Technology',
                'Design & UX',
                'Business Intelligence',
                'Marketing',
                'Human Resources',
                'Finance',
              ]}
              value={formData.department}
              onChange={(e) => setFormData({ ...formData, department: e.target.value })}
            />
            <FormField
              label="Designation"
              required
              value={formData.designation}
              onChange={(e) => setFormData({ ...formData, designation: e.target.value })}
            />
            <FormField
              label="System Role"
              type="select"
              required
              options={[
                { value: 'EMPLOYEE', label: 'Employee' },
                { value: 'IT_MANAGER', label: 'IT Manager' },
                { value: 'ADMIN', label: 'Admin' },
              ]}
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value })}
            />
            <FormField
              label="Status"
              type="select"
              options={['Active', 'Inactive', 'OnLeave']}
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: e.target.value })}
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsEditModalOpen(false)}
              className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition disabled:opacity-50"
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Toggle Status Dialog */}
      <ConfirmDialog
        isOpen={isStatusDialogOpen}
        onClose={() => setIsStatusDialogOpen(false)}
        onConfirm={handleToggleStatus}
        title="Confirm Status Change"
        type="warning"
        message={`Are you sure you want to change status of ${selectedEmployee?.employeeName} to '${
          selectedEmployee?.status === 'Active' ? 'Inactive' : 'Active'
        }'? ${
          selectedEmployee?.status === 'Active'
            ? 'Inactive employees cannot receive new license assignments and cannot log in.'
            : 'Employee will regain system access.'
        }`}
        confirmText={selectedEmployee?.status === 'Active' ? 'Deactivate' : 'Activate'}
        loading={saving}
      />

      {/* Delete Dialog */}
      <ConfirmDialog
        isOpen={isDeleteDialogOpen}
        onClose={() => setIsDeleteDialogOpen(false)}
        onConfirm={handleDelete}
        title="Delete Employee Record"
        type="danger"
        message={`Are you sure you want to permanently delete employee ${selectedEmployee?.employeeName} (${selectedEmployee?.employeeCode})? This action will remove their user credentials. Employees with active licenses cannot be deleted.`}
        confirmText="Delete Employee"
        loading={saving}
      />
    </div>
  );
};
