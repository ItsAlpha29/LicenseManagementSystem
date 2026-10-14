import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  KeyRound,
  Building,
  Calendar,
  DollarSign,
  Users,
  RefreshCw,
  Clock,
  Shield,
  Layers,
} from 'lucide-react';
import { licenseService } from '../services/licenseService';
import { assignmentService } from '../services/assignmentService';
import { StatusBadge } from '../components/StatusBadge';
import { Modal } from '../components/Modal';
import { FormField } from '../components/FormField';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { useToast } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';

export const LicenseDetailPage = () => {
  const { id } = useParams();
  const [license, setLicense] = useState(null);
  const [loading, setLoading] = useState(true);

  // Revoke modal state
  const [isRevokeOpen, setIsRevokeOpen] = useState(false);
  const [selectedAssignment, setSelectedAssignment] = useState(null);
  const [revokeReason, setRevokeReason] = useState('License Reallocation');
  const [revokeRemarks, setRevokeRemarks] = useState('');
  const [revoking, setRevoking] = useState(false);

  const toast = useToast();
  const navigate = useNavigate();
  const { role } = useAuth();
  const canManage = role === 'ADMIN' || role === 'IT_MANAGER';

  const fetchLicense = async () => {
    try {
      setLoading(true);
      const res = await licenseService.getLicenseById(id);
      if (res.success && res.data) {
        setLicense(res.data);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load license details.');
      navigate('/licenses');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLicense();
  }, [id]);

  const handleOpenRevoke = (asgn) => {
    setSelectedAssignment(asgn);
    setRevokeReason('License Reallocation');
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
        toast.success(`License successfully revoked from ${selectedAssignment.employeeName}.`);
        setIsRevokeOpen(false);
        fetchLicense();
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

  if (!license) return null;

  return (
    <div className="space-y-6">
      {/* Back Button */}
      <button
        onClick={() => navigate('/licenses')}
        className="inline-flex items-center gap-2 text-xs font-semibold text-slate-500 hover:text-slate-900 transition"
      >
        <ArrowLeft className="w-4 h-4" />
        Back to License Inventory
      </button>

      {/* Header Banner */}
      <div className="bg-white rounded-3xl p-6 sm:p-8 border border-slate-200/80 shadow-xs">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
          <div className="flex items-center gap-5">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-purple-600 to-indigo-600 text-white font-black text-2xl flex items-center justify-center shadow-md">
              <KeyRound className="w-8 h-8" />
            </div>
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-xl sm:text-2xl font-bold text-slate-900">{license.softwareName}</h1>
                <span className="font-mono text-xs font-bold bg-slate-100 text-slate-700 px-2.5 py-1 rounded-md">
                  {license.licenseCode}
                </span>
                <StatusBadge status={license.status} size="xs" />
              </div>
              <p className="text-xs sm:text-sm text-slate-500 mt-1 font-medium">
                {license.vendor} &bull; <span className="text-slate-800 font-semibold">{license.licenseType}</span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {canManage && (
              <button
                onClick={() => navigate('/renewals')}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center gap-1.5"
              >
                <RefreshCw className="w-3.5 h-3.5" />
                Manage Renewal
              </button>
            )}
          </div>
        </div>

        {/* 4 Stats Cards */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-6 pt-6 border-t border-slate-100">
          <div className="p-4 rounded-2xl bg-slate-50 border border-slate-100 text-center">
            <span className="text-[11px] font-semibold text-slate-400 uppercase">Total Seats</span>
            <div className="text-2xl font-black text-slate-900 mt-0.5">{license.totalLicenses}</div>
          </div>
          <div className="p-4 rounded-2xl bg-indigo-50/70 border border-indigo-100 text-center">
            <span className="text-[11px] font-semibold text-indigo-600 uppercase">Assigned Seats</span>
            <div className="text-2xl font-black text-indigo-700 mt-0.5">{license.assignedCount}</div>
          </div>
          <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-100 text-center">
            <span className="text-[11px] font-semibold text-emerald-600 uppercase">Available Seats</span>
            <div className="text-2xl font-black text-emerald-700 mt-0.5">{license.availableCount}</div>
          </div>
          <div className="p-4 rounded-2xl bg-purple-50/70 border border-purple-100 text-center">
            <span className="text-[11px] font-semibold text-purple-600 uppercase">Seat Utilization</span>
            <div className="text-2xl font-black text-purple-700 mt-0.5">{license.utilizationPercentage}%</div>
          </div>
        </div>

        {/* Details Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mt-6 text-xs border-t border-slate-100 pt-6">
          <div>
            <span className="text-[10px] text-slate-400 uppercase font-bold block">Purchase Date</span>
            <span className="font-semibold text-slate-800">
              {license.purchaseDate ? new Date(license.purchaseDate).toLocaleDateString() : '—'}
            </span>
          </div>
          <div>
            <span className="text-[10px] text-slate-400 uppercase font-bold block">Contract Expiry Date</span>
            <span className="font-semibold text-slate-800">
              {license.expiryDate ? new Date(license.expiryDate).toLocaleDateString() : '—'}
            </span>
          </div>
          <div>
            <span className="text-[10px] text-slate-400 uppercase font-bold block">Annual Contract Cost</span>
            <span className="font-semibold text-slate-800">${license.cost?.toLocaleString()}</span>
          </div>
          <div>
            <span className="text-[10px] text-slate-400 uppercase font-bold block">Key / Contract Reference</span>
            <span className="font-mono text-slate-700 font-semibold">{license.licenseReference || '—'}</span>
          </div>
        </div>
      </div>

      {/* Current Active Allocations Table */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-indigo-50 text-indigo-600">
              <Users className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Current Assigned Employees</h3>
              <p className="text-xs text-slate-500">Active seat allocations for this software</p>
            </div>
          </div>

          {canManage && license.availableCount > 0 && (
            <button
              onClick={() => navigate('/assignments')}
              className="px-3.5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
            >
              + Assign Seat to Employee
            </button>
          )}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50/60 text-slate-500 font-semibold uppercase">
                <th className="p-3">Employee</th>
                <th className="p-3">Employee Code</th>
                <th className="p-3">Department</th>
                <th className="p-3">Assigned Date</th>
                <th className="p-3">Expiry Date</th>
                <th className="p-3">Status</th>
                {canManage && <th className="p-3 text-right">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {license.currentAssignments && license.currentAssignments.length > 0 ? (
                license.currentAssignments.map((asgn) => (
                  <tr key={asgn.id} className="hover:bg-slate-50/70 transition">
                    <td className="p-3">
                      <div className="font-bold text-slate-900">{asgn.employeeName}</div>
                      <div className="text-slate-400 text-[11px]">{asgn.employeeEmail}</div>
                    </td>
                    <td className="p-3 font-mono text-slate-700 font-semibold">{asgn.employeeCode}</td>
                    <td className="p-3 text-slate-700">{asgn.department}</td>
                    <td className="p-3 text-slate-600">{new Date(asgn.assignDate).toLocaleDateString()}</td>
                    <td className="p-3 font-mono text-slate-700">{new Date(asgn.expiryDate).toLocaleDateString()}</td>
                    <td className="p-3"><StatusBadge status={asgn.status} size="xs" /></td>
                    {canManage && (
                      <td className="p-3 text-right">
                        <button
                          onClick={() => handleOpenRevoke(asgn)}
                          className="px-2.5 py-1 text-[11px] font-semibold text-rose-600 hover:text-white hover:bg-rose-600 border border-rose-200 rounded-lg transition"
                        >
                          Revoke Seat
                        </button>
                      </td>
                    )}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="p-8 text-center text-slate-400 text-xs">
                    No active allocations currently recorded for this software.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Renewal History Table */}
      {license.renewalHistory && license.renewalHistory.length > 0 && (
        <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center gap-2.5 mb-4">
            <div className="p-2 rounded-xl bg-emerald-50 text-emerald-600">
              <RefreshCw className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Renewal History</h3>
              <p className="text-xs text-slate-500">Past renewals recorded for this software asset</p>
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-xs text-left">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/60 text-slate-500 font-semibold uppercase">
                  <th className="p-3">Renewal Date</th>
                  <th className="p-3">Renewal Amount</th>
                  <th className="p-3">Previous Expiry</th>
                  <th className="p-3">New Expiry</th>
                  <th className="p-3">Renewed By</th>
                  <th className="p-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {license.renewalHistory.map((ren) => (
                  <tr key={ren.id} className="hover:bg-slate-50/70 transition">
                    <td className="p-3 font-semibold text-slate-800">
                      {new Date(ren.renewalDate).toLocaleDateString()}
                    </td>
                    <td className="p-3 font-bold text-emerald-700">${ren.renewalAmount?.toLocaleString()}</td>
                    <td className="p-3 text-slate-500">{new Date(ren.previousExpiryDate).toLocaleDateString()}</td>
                    <td className="p-3 font-mono font-semibold text-slate-800">
                      {new Date(ren.newExpiryDate).toLocaleDateString()}
                    </td>
                    <td className="p-3 text-slate-600">{ren.renewedByUserName || 'System Administrator'}</td>
                    <td className="p-3"><StatusBadge status={ren.status} size="xs" /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Revocation Modal */}
      <Modal
        isOpen={isRevokeOpen}
        onClose={() => setIsRevokeOpen(false)}
        title="Revoke Software License"
        subtitle={`Revoking ${license.softwareName} from ${selectedAssignment?.employeeName}`}
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
            placeholder="Reason or authorization reference..."
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
