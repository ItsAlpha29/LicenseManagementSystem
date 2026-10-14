import React, { useState, useEffect } from 'react';
import {
  Award,
  KeyRound,
  Calendar,
  CheckCircle2,
  AlertTriangle,
  AlertCircle,
  Clock,
  Shield,
  Search,
} from 'lucide-react';
import { assignmentService } from '../services/assignmentService';
import { StatusBadge } from '../components/StatusBadge';
import { KpiCard } from '../components/KpiCard';
import { SearchBar } from '../components/SearchBar';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { EmptyState } from '../components/EmptyState';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';

export const MyLicensesPage = () => {
  const [licenses, setLicenses] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  const { user } = useAuth();
  const toast = useToast();

  const fetchMyLicenses = async () => {
    try {
      setLoading(true);
      const res = await assignmentService.getMyLicenses();
      if (res.success && res.data) {
        setLicenses(res.data || []);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load assigned licenses.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMyLicenses();
  }, []);

  const filteredLicenses = licenses.filter(
    (l) =>
      l.softwareName.toLowerCase().includes(search.toLowerCase()) ||
      l.licenseCode.toLowerCase().includes(search.toLowerCase()) ||
      l.vendor.toLowerCase().includes(search.toLowerCase())
  );

  const totalAssigned = licenses.length;
  const activeCount = licenses.filter((l) => l.status === 'Active' && l.daysRemaining >= 30).length;
  const expiringSoonCount = licenses.filter((l) => l.status === 'Active' && l.daysRemaining > 0 && l.daysRemaining < 30).length;
  const expiredCount = licenses.filter((l) => l.daysRemaining <= 0).length;

  if (loading) {
    return (
      <div className="space-y-6">
        <LoadingSkeleton count={4} />
        <LoadingSkeleton count={1} type="table" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-slate-900 tracking-tight">My Software Licenses</h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Self-service portal of software tools and entitlements assigned to your workstation
        </p>
      </div>

      {/* KPI Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          title="Total Assigned"
          value={totalAssigned}
          subtitle="Software applications"
          icon={Award}
          color="indigo"
        />
        <KpiCard
          title="Active & Healthy"
          value={activeCount}
          subtitle="Full validity"
          icon={CheckCircle2}
          color="emerald"
        />
        <KpiCard
          title="Expiring Soon"
          value={expiringSoonCount}
          subtitle="Within 30 days"
          icon={AlertTriangle}
          color="amber"
        />
        <KpiCard
          title="Expired / Action Needed"
          value={expiredCount}
          subtitle="Contact IT for renewal"
          icon={AlertCircle}
          color="rose"
        />
      </div>

      {/* Search Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs">
        <SearchBar
          value={search}
          onChange={setSearch}
          placeholder="Filter my licenses by software name, vendor..."
          className="max-w-md"
        />
      </div>

      {/* Licenses Grid of Cards */}
      {filteredLicenses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredLicenses.map((lic) => {
            const days = lic.daysRemaining;
            const isExpiring = days > 0 && days <= 30;
            const isExpired = days <= 0;

            return (
              <div
                key={lic.id}
                className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs hover:shadow-md transition-all duration-200 flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-start justify-between gap-2 mb-3">
                    <div className="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center font-bold">
                      <KeyRound className="w-5 h-5" />
                    </div>
                    <StatusBadge status={lic.status} size="xs" />
                  </div>

                  <span className="font-mono text-[10px] font-bold text-slate-400 uppercase tracking-wider block mb-1">
                    {lic.licenseCode}
                  </span>
                  <h3 className="text-base font-bold text-slate-900 tracking-tight mb-1">
                    {lic.softwareName}
                  </h3>
                  <p className="text-xs text-slate-500 font-medium mb-4">
                    {lic.vendor} &bull; <span className="text-slate-700">{lic.licenseType}</span>
                  </p>
                </div>

                <div className="pt-4 border-t border-slate-100 space-y-2.5 text-xs text-slate-600">
                  <div className="flex justify-between items-center">
                    <span className="text-slate-400">Assigned Date:</span>
                    <span className="font-semibold text-slate-800">
                      {new Date(lic.assignDate).toLocaleDateString()}
                    </span>
                  </div>

                  <div className="flex justify-between items-center">
                    <span className="text-slate-400">Expiry Date:</span>
                    <span className="font-mono font-semibold text-slate-800">
                      {new Date(lic.expiryDate).toLocaleDateString()}
                    </span>
                  </div>

                  <div className="flex justify-between items-center">
                    <span className="text-slate-400">Days Remaining:</span>
                    <span
                      className={`font-bold ${
                        isExpired
                          ? 'text-rose-600'
                          : isExpiring
                          ? 'text-amber-600'
                          : 'text-emerald-600'
                      }`}
                    >
                      {isExpired ? `Expired` : `${days} Days Left`}
                    </span>
                  </div>

                  {lic.remarks && (
                    <div className="p-2.5 bg-slate-50 rounded-xl text-[11px] text-slate-500 italic mt-2">
                      &ldquo;{lic.remarks}&rdquo;
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="bg-white rounded-3xl p-12 border border-slate-200/80 shadow-xs text-center">
          <EmptyState
            title="No Assigned Licenses"
            description="You do not currently have any active software licenses assigned to your profile. Reach out to your IT administrator if you require tools."
          />
        </div>
      )}
    </div>
  );
};
