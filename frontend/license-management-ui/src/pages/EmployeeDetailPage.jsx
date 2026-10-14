import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  User,
  Mail,
  Phone,
  Building2,
  Briefcase,
  Calendar,
  Shield,
  KeyRound,
  History,
  AlertCircle,
  RotateCcw,
  Sparkles,
  BrainCircuit,
  Plus,
} from 'lucide-react';
import { employeeService } from '../services/employeeService';
import { assignmentService } from '../services/assignmentService';
import { aiService } from '../services/aiService';
import { StatusBadge } from '../components/StatusBadge';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Modal } from '../components/Modal';
import { FormField } from '../components/FormField';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { useToast } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';

export const EmployeeDetailPage = () => {
  const { id } = useParams();
  const [employee, setEmployee] = useState(null);
  const [loading, setLoading] = useState(true);

  // AI Recommendation State
  const [aiRecommendations, setAiRecommendations] = useState(null);
  const [loadingAiRecs, setLoadingAiRecs] = useState(false);

  // Revoke state
  const [isRevokeOpen, setIsRevokeOpen] = useState(false);
  const [selectedAssignment, setSelectedAssignment] = useState(null);
  const [revokeReason, setRevokeReason] = useState('No Longer Required');
  const [revokeRemarks, setRevokeRemarks] = useState('');
  const [revoking, setRevoking] = useState(false);

  const toast = useToast();
  const navigate = useNavigate();
  const { role } = useAuth();
  const canManage = role === 'ADMIN' || role === 'IT_MANAGER';

  const fetchDetails = async () => {
    try {
      setLoading(true);
      const res = await employeeService.getEmployeeById(id);
      if (res.success && res.data) {
        setEmployee(res.data);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load employee details.');
      navigate('/employees');
    } finally {
      setLoading(false);
    }

    try {
      setLoadingAiRecs(true);
      const aiRes = await aiService.getEmployeeRecommendations(id);
      if (aiRes.success && aiRes.data) {
        setAiRecommendations(aiRes.data);
      }
    } catch {
      // ignore
    } finally {
      setLoadingAiRecs(false);
    }
  };

  useEffect(() => {
    fetchDetails();
  }, [id]);

  const handleOpenRevoke = (asgn) => {
    setSelectedAssignment(asgn);
    setRevokeReason('No Longer Required');
    setRevokeRemarks('');
    setIsRevokeOpen(true);
  };

  const handleConfirmRevoke = async (e) => {
    e.preventDefault();
    if (!selectedAssignment) return;

    setRevoking(true);
    try {
      const res = await assignmentService.revokeLicense(selectedAssignment.id, {
        revocationReason: revokeReason,
        remarks: revokeRemarks,
      });

      if (res.success) {
        toast.success(`License ${selectedAssignment.softwareName} successfully revoked.`);
        setIsRevokeOpen(false);
        fetchDetails();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to revoke license.');
    } finally {
      setRevoking(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <LoadingSkeleton count={1} type="table" />
      </div>
    );
  }

  if (!employee) return null;

  return (
    <div className="space-y-6">
      {/* Back Button */}
      <button
        onClick={() => navigate('/employees')}
        className="inline-flex items-center gap-2 text-xs font-semibold text-slate-500 hover:text-slate-900 transition"
      >
        <ArrowLeft className="w-4 h-4" />
        Back to Employee Directory
      </button>

      {/* Header Profile Card */}
      <div className="bg-white rounded-3xl p-6 sm:p-8 border border-slate-200/80 shadow-xs">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
          <div className="flex items-center gap-5">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-indigo-600 to-purple-600 text-white font-black text-2xl flex items-center justify-center shadow-md">
              {employee.employeeName?.charAt(0) || 'E'}
            </div>
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-xl sm:text-2xl font-bold text-slate-900">{employee.employeeName}</h1>
                <span className="font-mono text-xs font-bold bg-slate-100 text-slate-700 px-2.5 py-1 rounded-md">
                  {employee.employeeCode}
                </span>
                <StatusBadge status={employee.status} size="xs" />
              </div>
              <p className="text-xs sm:text-sm text-slate-500 mt-1 font-medium">
                {employee.designation} &bull; <span className="text-slate-800 font-semibold">{employee.department}</span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <StatusBadge status={employee.role} size="md" />
          </div>
        </div>

        {/* Details Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mt-6 pt-6 border-t border-slate-100 text-xs">
          <div className="flex items-center gap-2.5 text-slate-600">
            <Mail className="w-4 h-4 text-slate-400" />
            <div>
              <span className="text-[10px] text-slate-400 block uppercase font-bold">Email</span>
              <span className="font-semibold text-slate-800">{employee.email}</span>
            </div>
          </div>

          <div className="flex items-center gap-2.5 text-slate-600">
            <Phone className="w-4 h-4 text-slate-400" />
            <div>
              <span className="text-[10px] text-slate-400 block uppercase font-bold">Phone</span>
              <span className="font-semibold text-slate-800">{employee.phone || '—'}</span>
            </div>
          </div>

          <div className="flex items-center gap-2.5 text-slate-600">
            <Calendar className="w-4 h-4 text-slate-400" />
            <div>
              <span className="text-[10px] text-slate-400 block uppercase font-bold">Joining Date</span>
              <span className="font-semibold text-slate-800">
                {employee.joiningDate ? new Date(employee.joiningDate).toLocaleDateString() : '—'}
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2.5 text-slate-600">
            <User className="w-4 h-4 text-slate-400" />
            <div>
              <span className="text-[10px] text-slate-400 block uppercase font-bold">Reporting Manager</span>
              <span className="font-semibold text-slate-800">{employee.managerName || 'None (Direct Report)'}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Active Software Licenses Section */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-indigo-50 text-indigo-600">
              <KeyRound className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Assigned Software Licenses</h3>
              <p className="text-xs text-slate-500">Active and past software allocations for this employee</p>
            </div>
          </div>

          {canManage && employee.status === 'Active' && (
            <button
              onClick={() => navigate('/assignments')}
              className="px-3.5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
            >
              + Assign New License
            </button>
          )}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50/60 text-slate-500 font-semibold uppercase">
                <th className="p-3">Software</th>
                <th className="p-3">License Code</th>
                <th className="p-3">Vendor</th>
                <th className="p-3">Type</th>
                <th className="p-3">Assign Date</th>
                <th className="p-3">Expiry Date</th>
                <th className="p-3">Status</th>
                {canManage && <th className="p-3 text-right">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {employee.assignedLicenses && employee.assignedLicenses.length > 0 ? (
                employee.assignedLicenses.map((asgn) => (
                  <tr key={asgn.id} className="hover:bg-slate-50/70 transition">
                    <td className="p-3 font-bold text-slate-900">{asgn.softwareName}</td>
                    <td className="p-3 font-mono text-slate-600">{asgn.licenseCode}</td>
                    <td className="p-3 text-slate-600">{asgn.vendor}</td>
                    <td className="p-3 text-slate-600">{asgn.licenseType}</td>
                    <td className="p-3 text-slate-600">{new Date(asgn.assignDate).toLocaleDateString()}</td>
                    <td className="p-3 font-mono text-slate-700">{new Date(asgn.expiryDate).toLocaleDateString()}</td>
                    <td className="p-3"><StatusBadge status={asgn.status} size="xs" /></td>
                    {canManage && (
                      <td className="p-3 text-right">
                        {asgn.status === 'Active' && (
                          <button
                            onClick={() => handleOpenRevoke(asgn)}
                            className="px-2.5 py-1 text-[11px] font-semibold text-rose-600 hover:text-white hover:bg-rose-600 border border-rose-200 rounded-lg transition"
                          >
                            Revoke
                          </button>
                        )}
                      </td>
                    )}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={8} className="p-8 text-center text-slate-400 text-xs">
                    No software licenses assigned to this employee yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* AI Recommended Software Section */}
      {aiRecommendations && (
        <div className="bg-gradient-to-br from-purple-950/5 via-indigo-950/5 to-slate-50 rounded-3xl p-6 border border-purple-200/80 shadow-xs space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-xl bg-purple-100 text-purple-700">
                <Sparkles className="w-5 h-5 fill-purple-600" />
              </div>
              <div>
                <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
                  <span>AI Recommended Software</span>
                  <span className="text-[10px] font-bold bg-purple-100 text-purple-800 px-2 py-0.5 rounded-full uppercase tracking-wider">
                    Role-Matched
                  </span>
                </h3>
                <p className="text-xs text-slate-500">
                  Tailored software stack based on {employee.designation} in {employee.department}
                </p>
              </div>
            </div>
            {canManage && (
              <button
                onClick={() => navigate('/assignments')}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-purple-600 hover:bg-purple-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
              >
                <Plus className="w-3.5 h-3.5" />
                Assign License
              </button>
            )}
          </div>

          <p className="text-xs text-slate-600 leading-relaxed bg-white/80 p-3 rounded-2xl border border-purple-100/60">
            {aiRecommendations.summary}
          </p>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {aiRecommendations.recommendations?.slice(0, 3).map((rec) => (
              <div
                key={rec.licenseId}
                className={`p-4 rounded-2xl border transition flex flex-col justify-between ${
                  rec.isCurrentlyAssigned
                    ? 'bg-slate-50 border-slate-200 opacity-75'
                    : 'bg-white border-purple-200 hover:border-purple-400 hover:shadow-xs'
                }`}
              >
                <div className="space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <div className="font-bold text-slate-900 text-xs">{rec.softwareName}</div>
                      <div className="text-[10px] text-slate-400 font-mono">{rec.vendor} &bull; {rec.licenseCode}</div>
                    </div>
                    <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold shrink-0 ${
                      rec.isCurrentlyAssigned
                        ? 'bg-emerald-100 text-emerald-800'
                        : 'bg-purple-100 text-purple-800'
                    }`}>
                      {rec.isCurrentlyAssigned ? 'Active' : `${rec.matchScore}% Match`}
                    </span>
                  </div>

                  <p className="text-[11px] text-slate-600 leading-snug">
                    {rec.recommendationReason}
                  </p>
                </div>

                <div className="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
                  <span className="text-[10px] text-slate-500 font-semibold">
                    {rec.availableSeats} / {rec.totalSeats} seats available
                  </span>
                  {!rec.isCurrentlyAssigned && canManage && (
                    <button
                      onClick={() => navigate('/assignments')}
                      className="text-xs font-bold text-purple-700 hover:text-purple-900 hover:underline flex items-center gap-1"
                    >
                      <span>Assign</span>
                      <span>&rarr;</span>
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Activity Timeline */}
      {employee.activityLogs && employee.activityLogs.length > 0 && (
        <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center gap-2.5 mb-4">
            <div className="p-2 rounded-xl bg-purple-50 text-purple-600">
              <History className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Activity History</h3>
              <p className="text-xs text-slate-500">Audit logs relating to this employee</p>
            </div>
          </div>

          <div className="space-y-3 max-h-64 overflow-y-auto pr-2">
            {employee.activityLogs.map((log) => (
              <div key={log.id} className="p-3 rounded-xl bg-slate-50 border border-slate-100 flex items-start justify-between gap-4 text-xs">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-bold text-slate-800">{log.action}</span>
                    <span className="px-2 py-0.2 text-[10px] font-mono bg-indigo-100 text-indigo-700 rounded-md">
                      {log.module}
                    </span>
                  </div>
                  <p className="text-slate-600 leading-relaxed">{log.remarks}</p>
                </div>
                <span className="text-[10px] text-slate-400 whitespace-nowrap">
                  {new Date(log.dateTime).toLocaleString()}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Revocation Modal */}
      <Modal
        isOpen={isRevokeOpen}
        onClose={() => setIsRevokeOpen(false)}
        title="Revoke Software License"
        subtitle={`Revoking ${selectedAssignment?.softwareName} from ${employee.employeeName}`}
        maxWidth="max-w-md"
      >
        <form onSubmit={handleConfirmRevoke} className="space-y-4">
          <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-xs text-amber-900">
            Revoking will restore <strong>1 seat</strong> to the available inventory pool immediately.
          </div>

          <FormField
            label="Revocation Reason"
            type="select"
            required
            options={[
              'No Longer Required',
              'Employee Left',
              'License Reallocation',
              'Role Change',
              'Other',
            ]}
            value={revokeReason}
            onChange={(e) => setRevokeReason(e.target.value)}
          />

          <FormField
            label="Remarks / Audit Note"
            type="textarea"
            placeholder="Provide context for this revocation..."
            value={revokeRemarks}
            onChange={(e) => setRevokeRemarks(e.target.value)}
          />

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsRevokeOpen(false)}
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
