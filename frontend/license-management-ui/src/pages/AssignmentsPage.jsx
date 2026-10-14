import React, { useState, useEffect } from 'react';
import {
  UserCheck,
  UserPlus,
  RotateCcw,
  Search,
  Filter,
  CheckCircle2,
  AlertCircle,
  AlertTriangle,
  KeyRound,
  Users,
  Shield,
  Layers,
  ArrowRight,
  Sparkles,
  BrainCircuit,
  Zap,
} from 'lucide-react';
import { assignmentService } from '../services/assignmentService';
import { employeeService } from '../services/employeeService';
import { licenseService } from '../services/licenseService';
import { aiService } from '../services/aiService';
import { DataTable } from '../components/DataTable';
import { SearchBar } from '../components/SearchBar';
import { FilterBar } from '../components/FilterBar';
import { StatusBadge } from '../components/StatusBadge';
import { Modal } from '../components/Modal';
import { FormField } from '../components/FormField';
import { useToast } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';

export const AssignmentsPage = () => {
  const [assignments, setAssignments] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Search & Filters
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ department: 'All', status: 'All' });
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Assignment Modal & Multi-Step Wizard
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [assignStep, setAssignStep] = useState(1);
  const [employeesList, setEmployeesList] = useState([]);
  const [licensesList, setLicensesList] = useState([]);
  const [selectedEmployee, setSelectedEmployee] = useState(null);
  const [selectedLicense, setSelectedLicense] = useState(null);
  const [assignExpiryDate, setAssignExpiryDate] = useState('');
  const [assignRemarks, setAssignRemarks] = useState('');
  const [assigning, setAssigning] = useState(false);

  // AI Recommendation State
  const [aiRecommendations, setAiRecommendations] = useState(null);
  const [loadingAiRecommendations, setLoadingAiRecommendations] = useState(false);

  // Revoke Modal
  const [isRevokeModalOpen, setIsRevokeModalOpen] = useState(false);
  const [selectedAssignment, setSelectedAssignment] = useState(null);
  const [revokeReason, setRevokeReason] = useState('License Reallocation');
  const [revokeRemarks, setRevokeRemarks] = useState('');
  const [revoking, setRevoking] = useState(false);

  const toast = useToast();
  const { role } = useAuth();
  const canManage = role === 'ADMIN' || role === 'IT_MANAGER';

  const fetchAssignments = async () => {
    try {
      setLoading(true);
      const res = await assignmentService.getAssignments({
        search,
        department: filters.department,
        status: filters.status,
        pageNumber,
        pageSize,
      });

      if (res.success && res.data) {
        setAssignments(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load assignments.');
    } finally {
      setLoading(false);
    }
  };

  const loadWizardData = async () => {
    try {
      const [empRes, licRes] = await Promise.all([
        employeeService.getEmployees({ pageSize: 100 }),
        licenseService.getLicenses({ pageSize: 100 }),
      ]);
      if (empRes.success && empRes.data) setEmployeesList(empRes.data.items || []);
      if (licRes.success && licRes.data) setLicensesList(licRes.data.items || []);
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    fetchAssignments();
  }, [search, filters, pageNumber, pageSize]);

  const handleOpenAssignModal = () => {
    loadWizardData();
    setAssignStep(1);
    setSelectedEmployee(null);
    setSelectedLicense(null);
    setAiRecommendations(null);
    setAssignExpiryDate('');
    setAssignRemarks('');
    setIsAssignModalOpen(true);
  };

  const handleSelectEmployee = async (emp) => {
    if (emp.status !== 'Active') {
      toast.warning('Inactive employees cannot receive license allocations.');
      return;
    }
    setSelectedEmployee(emp);
    setAssignStep(2);
    setAiRecommendations(null);

    try {
      setLoadingAiRecommendations(true);
      const res = await aiService.getEmployeeRecommendations(emp.id);
      if (res.success && res.data) {
        setAiRecommendations(res.data);
      }
    } catch {
      // Fallback silently if AI call is unavailable
    } finally {
      setLoadingAiRecommendations(false);
    }
  };

  const handleSelectLicense = (lic) => {
    if (lic.availableCount <= 0) {
      toast.error(`No seats available for ${lic.softwareName}.`);
      return;
    }
    if (new Date(lic.expiryDate) < new Date()) {
      toast.error(`License ${lic.softwareName} has expired.`);
      return;
    }
    setSelectedLicense(lic);
    setAssignExpiryDate(lic.expiryDate.split('T')[0]);
    setAssignStep(3);
  };

  const handleExecuteAssignment = async () => {
    if (!selectedEmployee || !selectedLicense) return;

    setAssigning(true);
    try {
      const res = await assignmentService.assignLicense({
        employeeId: selectedEmployee.id,
        licenseId: selectedLicense.id,
        expiryDate: assignExpiryDate ? new Date(assignExpiryDate) : null,
        remarks: assignRemarks,
      });

      if (res.success) {
        toast.success(`Successfully assigned ${selectedLicense.softwareName} to ${selectedEmployee.employeeName}.`);
        setIsAssignModalOpen(false);
        fetchAssignments();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to complete assignment.');
    } finally {
      setAssigning(false);
    }
  };

  const handleOpenRevoke = (asgn) => {
    setSelectedAssignment(asgn);
    setRevokeReason('License Reallocation');
    setRevokeRemarks('');
    setIsRevokeModalOpen(true);
  };

  const handleExecuteRevoke = async (e) => {
    e.preventDefault();
    if (!selectedAssignment) return;

    setRevoking(true);
    try {
      const res = await assignmentService.revokeLicense(selectedAssignment.id, {
        revocationReason: revokeReason,
        remarks: revokeRemarks,
      });

      if (res.success) {
        toast.success(`Successfully revoked license from ${selectedAssignment.employeeName}.`);
        setIsRevokeModalOpen(false);
        fetchAssignments();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to revoke license.');
    } finally {
      setRevoking(false);
    }
  };

  const columns = [
    {
      header: 'Employee',
      key: 'employeeName',
      render: (row) => (
        <div>
          <div className="font-bold text-slate-900">{row.employeeName}</div>
          <div className="text-xs text-slate-400 font-mono">{row.employeeCode} &bull; {row.department}</div>
        </div>
      ),
    },
    {
      header: 'Software License',
      key: 'softwareName',
      render: (row) => (
        <div>
          <div className="font-bold text-slate-900">{row.softwareName}</div>
          <div className="text-xs text-slate-400 font-mono">{row.licenseCode} &bull; {row.vendor}</div>
        </div>
      ),
    },
    {
      header: 'Assigned Date',
      key: 'assignDate',
      render: (row) => (
        <span className="text-xs text-slate-600 font-medium">
          {new Date(row.assignDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      header: 'Expiry Date',
      key: 'expiryDate',
      render: (row) => {
        const days = row.daysRemaining;
        return (
          <div>
            <div className="font-mono text-xs font-semibold text-slate-800">
              {new Date(row.expiryDate).toLocaleDateString()}
            </div>
            {row.status === 'Active' && (
              <div className={`text-[10px] ${days < 0 ? 'text-rose-600' : days <= 30 ? 'text-amber-600' : 'text-slate-400'}`}>
                {days < 0 ? 'Expired' : `${days} days left`}
              </div>
            )}
          </div>
        );
      },
    },
    {
      header: 'Status',
      key: 'status',
      render: (row) => <StatusBadge status={row.status} size="xs" />,
    },
    {
      header: 'Revocation / Remarks',
      key: 'remarks',
      render: (row) => (
        <div className="text-xs text-slate-500 max-w-xs truncate">
          {row.revocationReason ? (
            <span className="text-rose-600 font-medium">{row.revocationReason}</span>
          ) : (
            row.remarks || '—'
          )}
        </div>
      ),
    },
    {
      header: 'Actions',
      key: 'actions',
      align: 'right',
      render: (row) => (
        <div className="flex items-center justify-end">
          {canManage && row.status === 'Active' ? (
            <button
              onClick={() => handleOpenRevoke(row)}
              className="px-3 py-1 text-xs font-semibold text-rose-600 hover:text-white hover:bg-rose-600 border border-rose-200 rounded-lg transition"
            >
              Revoke Seat
            </button>
          ) : (
            <span className="text-xs text-slate-400 italic">{row.status}</span>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight">License Allocations & Workflow</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Assign software seats to active employees, track allocations, and manage revocations
          </p>
        </div>

        {canManage && (
          <button
            onClick={handleOpenAssignModal}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
          >
            <UserCheck className="w-4 h-4" />
            Assign License to Employee
          </button>
        )}
      </div>

      {/* Search & Filter Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
          <div className="md:col-span-6">
            <SearchBar
              value={search}
              onChange={setSearch}
              placeholder="Search by employee name, code, software, or license code..."
            />
          </div>
          <div className="md:col-span-6">
            <FilterBar
              filters={[
                { key: 'department', label: 'Department', options: ['Engineering', 'Information Technology', 'Design & UX', 'Business Intelligence'] },
                { key: 'status', label: 'Status', options: ['Active', 'Revoked', 'Expired'] },
              ]}
              values={filters}
              onChange={(key, val) => {
                setFilters((prev) => ({ ...prev, [key]: val }));
                setPageNumber(1);
              }}
              onReset={() => {
                setSearch('');
                setFilters({ department: 'All', status: 'All' });
                setPageNumber(1);
              }}
            />
          </div>
        </div>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={assignments}
        loading={loading}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={setPageSize}
        emptyTitle="No license assignments found"
        emptyDescription="There are no license allocations matching the active filter criteria."
      />

      {/* Multi-Step Assignment Modal */}
      <Modal
        isOpen={isAssignModalOpen}
        onClose={() => setIsAssignModalOpen(false)}
        title="License Assignment Workflow"
        subtitle={`Step ${assignStep} of 3 — ${
          assignStep === 1 ? 'Select Employee' : assignStep === 2 ? 'Select Software License' : 'Confirm Assignment Details'
        }`}
        maxWidth="max-w-2xl"
      >
        {/* Step Indicator */}
        <div className="flex items-center justify-between mb-6 px-2">
          <div className={`flex items-center gap-2 text-xs font-bold ${assignStep >= 1 ? 'text-indigo-600' : 'text-slate-400'}`}>
            <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs ${assignStep >= 1 ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-500'}`}>1</span>
            <span>Employee</span>
          </div>
          <div className={`h-0.5 flex-1 mx-3 ${assignStep >= 2 ? 'bg-indigo-600' : 'bg-slate-200'}`} />
          <div className={`flex items-center gap-2 text-xs font-bold ${assignStep >= 2 ? 'text-indigo-600' : 'text-slate-400'}`}>
            <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs ${assignStep >= 2 ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-500'}`}>2</span>
            <span>Software License</span>
          </div>
          <div className={`h-0.5 flex-1 mx-3 ${assignStep >= 3 ? 'bg-indigo-600' : 'bg-slate-200'}`} />
          <div className={`flex items-center gap-2 text-xs font-bold ${assignStep >= 3 ? 'text-indigo-600' : 'text-slate-400'}`}>
            <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs ${assignStep >= 3 ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-500'}`}>3</span>
            <span>Confirm</span>
          </div>
        </div>

        {/* Step 1: Select Employee */}
        {assignStep === 1 && (
          <div className="space-y-3">
            <p className="text-xs text-slate-500">Choose an active employee from the organizational directory:</p>
            <div className="max-h-72 overflow-y-auto space-y-2 pr-1">
              {employeesList.filter(e => e.status === 'Active').map((emp) => (
                <div
                  key={emp.id}
                  onClick={() => handleSelectEmployee(emp)}
                  className="p-3 rounded-xl border border-slate-200 hover:border-indigo-500 hover:bg-indigo-50/40 cursor-pointer transition flex items-center justify-between group"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-xl bg-indigo-100 text-indigo-700 font-bold text-xs flex items-center justify-center">
                      {emp.employeeName?.charAt(0)}
                    </div>
                    <div>
                      <div className="text-xs font-bold text-slate-900 group-hover:text-indigo-600 transition">
                        {emp.employeeName}
                      </div>
                      <div className="text-[11px] text-slate-500 font-mono">
                        {emp.employeeCode} &bull; {emp.department}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-[10px] font-semibold bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full">
                      {emp.activeLicensesCount || 0} active licenses
                    </span>
                    <ArrowRight className="w-4 h-4 text-slate-400 group-hover:text-indigo-600 transition" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Step 2: Select Software License */}
        {assignStep === 2 && (
          <div className="space-y-3">
            <div className="p-3 bg-indigo-50 rounded-xl border border-indigo-100 text-xs text-indigo-950 flex items-center justify-between">
              <span>Selected Employee: <strong>{selectedEmployee?.employeeName}</strong> ({selectedEmployee?.department} &bull; {selectedEmployee?.designation})</span>
              <button
                onClick={() => setAssignStep(1)}
                className="text-indigo-600 hover:underline font-semibold"
              >
                Change
              </button>
            </div>

            {/* AI Recommendation Banner */}
            {loadingAiRecommendations && (
              <div className="p-3 bg-gradient-to-r from-purple-500/10 via-indigo-500/10 to-blue-500/10 rounded-2xl border border-purple-200/60 animate-pulse flex items-center gap-3">
                <BrainCircuit className="w-5 h-5 text-purple-600 animate-spin" />
                <span className="text-xs font-semibold text-purple-900">AI analyzing role requirements & software catalog...</span>
              </div>
            )}

            {aiRecommendations && aiRecommendations.recommendations && (
              <div className="p-3.5 bg-gradient-to-br from-purple-950/5 via-indigo-950/5 to-slate-50 rounded-2xl border border-purple-200 text-xs space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5 font-bold text-purple-900">
                    <Sparkles className="w-4 h-4 text-purple-600 fill-purple-500" />
                    <span>AI Recommended for {selectedEmployee?.designation}</span>
                  </div>
                  <span className="text-[10px] bg-purple-100 text-purple-800 font-bold px-2 py-0.5 rounded-full uppercase tracking-wider">
                    Role Match
                  </span>
                </div>
                <p className="text-[11px] text-slate-600 leading-relaxed">
                  {aiRecommendations.summary}
                </p>

                {/* Top Recommendation Quick Cards */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
                  {aiRecommendations.recommendations
                    .filter(r => !r.isCurrentlyAssigned && r.availableSeats > 0)
                    .slice(0, 2)
                    .map((rec) => {
                      const matchedLic = licensesList.find(l => l.id === rec.licenseId);
                      return (
                        <div
                          key={rec.licenseId}
                          onClick={() => matchedLic && handleSelectLicense(matchedLic)}
                          className="p-2.5 bg-white rounded-xl border border-purple-200/80 hover:border-purple-500 hover:shadow-sm cursor-pointer transition flex items-center justify-between group"
                        >
                          <div className="truncate pr-2">
                            <div className="font-bold text-slate-900 group-hover:text-purple-700 text-xs truncate">
                              {rec.softwareName}
                            </div>
                            <div className="text-[10px] text-purple-600 font-semibold flex items-center gap-1">
                              <span>{rec.matchScore}% Role Fit</span>
                              <span>&bull;</span>
                              <span>{rec.availableSeats} Available</span>
                            </div>
                          </div>
                          <span className="text-[10px] bg-purple-600 group-hover:bg-purple-700 text-white font-bold px-2 py-1 rounded-lg shrink-0">
                            Select
                          </span>
                        </div>
                      );
                    })}
                </div>
              </div>
            )}

            <p className="text-xs text-slate-500 font-medium">All software licenses with available seats:</p>
            <div className="max-h-64 overflow-y-auto space-y-2 pr-1">
              {licensesList.map((lic) => {
                const hasSeats = lic.availableCount > 0;
                const rec = aiRecommendations?.recommendations?.find(r => r.licenseId === lic.id);
                const isRecommended = rec && rec.matchScore >= 80;

                return (
                  <div
                    key={lic.id}
                    onClick={() => hasSeats && handleSelectLicense(lic)}
                    className={`p-3 rounded-xl border transition flex items-center justify-between ${
                      hasSeats
                        ? isRecommended
                          ? 'border-purple-200 bg-purple-50/20 hover:border-purple-500 hover:bg-purple-50/60 cursor-pointer group'
                          : 'border-slate-200 hover:border-indigo-500 hover:bg-indigo-50/40 cursor-pointer group'
                        : 'border-slate-100 bg-slate-50 opacity-60 cursor-not-allowed'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <div className={`w-9 h-9 rounded-xl flex items-center justify-center text-xs font-bold ${
                        isRecommended
                          ? 'bg-purple-600 text-white'
                          : hasSeats ? 'bg-indigo-100 text-indigo-700' : 'bg-slate-200 text-slate-400'
                      }`}>
                        {isRecommended ? <Sparkles className="w-4 h-4" /> : <KeyRound className="w-4 h-4" />}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-bold text-slate-900 group-hover:text-indigo-600 transition">
                            {lic.softwareName}
                          </span>
                          {isRecommended && (
                            <span className="text-[9px] font-bold bg-purple-100 text-purple-800 px-1.5 py-0.5 rounded-md flex items-center gap-0.5">
                              <Sparkles className="w-2.5 h-2.5 fill-purple-600" />
                              {rec.matchScore}% Fit
                            </span>
                          )}
                          {rec?.isCurrentlyAssigned && (
                            <span className="text-[9px] font-semibold bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded-md">
                              Already Assigned
                            </span>
                          )}
                        </div>
                        <div className="text-[11px] text-slate-500 font-mono">
                          {lic.licenseCode} &bull; {lic.vendor}
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-3">
                      <div className="text-right">
                        <div className={`text-xs font-bold ${hasSeats ? 'text-emerald-600' : 'text-rose-600'}`}>
                          {lic.availableCount} / {lic.totalLicenses} Seats
                        </div>
                        <div className="text-[10px] text-slate-400">
                          Expires: {new Date(lic.expiryDate).toLocaleDateString()}
                        </div>
                      </div>
                      {hasSeats && <ArrowRight className="w-4 h-4 text-slate-400 group-hover:text-indigo-600 transition" />}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* Step 3: Enter Details & Confirm */}
        {assignStep === 3 && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3 p-4 bg-slate-50 rounded-2xl border border-slate-100 text-xs">
              <div>
                <span className="text-[10px] text-slate-400 uppercase font-bold block">Target Employee</span>
                <span className="font-bold text-slate-900 text-sm">{selectedEmployee?.employeeName}</span>
                <span className="text-slate-500 block">{selectedEmployee?.department}</span>
              </div>
              <div>
                <span className="text-[10px] text-slate-400 uppercase font-bold block">Target Software</span>
                <span className="font-bold text-slate-900 text-sm">{selectedLicense?.softwareName}</span>
                <span className="text-emerald-600 font-semibold block">{selectedLicense?.availableCount} available seats remaining</span>
              </div>
            </div>

            <FormField
              label="License Expiry Date for this Employee"
              type="date"
              required
              value={assignExpiryDate}
              onChange={(e) => setAssignExpiryDate(e.target.value)}
              helperText={`Contract validity of software expires on ${selectedLicense?.expiryDate ? new Date(selectedLicense.expiryDate).toLocaleDateString() : '—'}`}
            />

            <FormField
              label="Remarks / Allotment Reason"
              type="textarea"
              placeholder="e.g. Standard workstation onboarding package..."
              value={assignRemarks}
              onChange={(e) => setAssignRemarks(e.target.value)}
            />

            <div className="flex items-center justify-between pt-4 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setAssignStep(2)}
                className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition"
              >
                Back
              </button>
              <button
                type="button"
                onClick={handleExecuteAssignment}
                disabled={assigning}
                className="px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition disabled:opacity-50"
              >
                {assigning ? 'Allocating Seat...' : 'Confirm & Assign License'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Revocation Modal */}
      <Modal
        isOpen={isRevokeModalOpen}
        onClose={() => setIsRevokeModalOpen(false)}
        title="Revoke License Allocation"
        subtitle={`Revoking ${selectedAssignment?.softwareName} from ${selectedAssignment?.employeeName}`}
        maxWidth="max-w-md"
      >
        <form onSubmit={handleExecuteRevoke} className="space-y-4">
          <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-xs text-amber-900">
            Revoking will restore <strong>1 seat</strong> to the available inventory pool immediately.
          </div>

          <FormField
            label="Revocation Reason"
            type="select"
            required
            options={[
              'License Reallocation',
              'Employee Left',
              'No Longer Required',
              'Role Change',
              'Other',
            ]}
            value={revokeReason}
            onChange={(e) => setRevokeReason(e.target.value)}
          />

          <FormField
            label="Remarks / Audit Note"
            type="textarea"
            placeholder="Audit remarks..."
            value={revokeRemarks}
            onChange={(e) => setRevokeRemarks(e.target.value)}
          />

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsRevokeModalOpen(false)}
              className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={revoking}
              className="px-4 py-2 text-xs font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-xl shadow-xs transition disabled:opacity-50"
            >
              {revoking ? 'Revoking...' : 'Confirm Revocation'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
