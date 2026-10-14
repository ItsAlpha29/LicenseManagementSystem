import React, { useState, useEffect } from 'react';
import {
  RefreshCw,
  Calendar,
  DollarSign,
  AlertTriangle,
  CheckCircle2,
  Clock,
  ArrowRight,
  Shield,
  Plus,
} from 'lucide-react';
import { renewalService } from '../services/renewalService';
import { licenseService } from '../services/licenseService';
import { DataTable } from '../components/DataTable';
import { SearchBar } from '../components/SearchBar';
import { StatusBadge } from '../components/StatusBadge';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { FormField } from '../components/FormField';
import { useToast } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';

export const RenewalsPage = () => {
  const [renewals, setRenewals] = useState([]);
  const [licenses, setLicenses] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Filters & Search
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Process Renewal Modal
  const [isRenewModalOpen, setIsRenewModalOpen] = useState(false);
  const [selectedLicense, setSelectedLicense] = useState(null);
  const [renewalAmount, setRenewalAmount] = useState(1000);
  const [newExpiryDate, setNewExpiryDate] = useState('');
  const [renewalRemarks, setRenewalRemarks] = useState('');
  const [renewing, setRenewing] = useState(false);

  const toast = useToast();
  const { role } = useAuth();
  const canManage = role === 'ADMIN' || role === 'IT_MANAGER';

  const fetchRenewals = async () => {
    try {
      setLoading(true);
      const [renRes, licRes] = await Promise.all([
        renewalService.getRenewals({ search, pageNumber, pageSize }),
        licenseService.getLicenses({ pageSize: 100 }),
      ]);

      if (renRes.success && renRes.data) {
        setRenewals(renRes.data.items || []);
        setTotalCount(renRes.data.totalCount || 0);
      }
      if (licRes.success && licRes.data) {
        setLicenses(licRes.data.items || []);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load renewals.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRenewals();
  }, [search, pageNumber, pageSize]);

  const handleOpenRenew = (lic) => {
    setSelectedLicense(lic);
    setRenewalAmount(lic.renewalCost || lic.cost || 1000);

    // Default new expiry: +1 year from current expiry date
    const currentExp = new Date(lic.expiryDate);
    const nextYear = new Date(currentExp);
    nextYear.setFullYear(nextYear.getFullYear() + 1);
    setNewExpiryDate(nextYear.toISOString().split('T')[0]);

    setRenewalRemarks(`Annual renewal for ${lic.softwareName}`);
    setIsRenewModalOpen(true);
  };

  const handleExecuteRenewal = async (e) => {
    e.preventDefault();
    if (!selectedLicense) return;

    if (new Date(newExpiryDate) <= new Date(selectedLicense.expiryDate)) {
      toast.error('New expiry date must be after current license expiry date (Rule 10).');
      return;
    }

    setRenewing(true);
    try {
      const res = await renewalService.processRenewal({
        licenseId: selectedLicense.id,
        renewalAmount: Number(renewalAmount),
        newExpiryDate: new Date(newExpiryDate),
        remarks: renewalRemarks,
      });

      if (res.success) {
        toast.success(`Successfully renewed ${selectedLicense.softwareName} until ${new Date(newExpiryDate).toLocaleDateString()}!`);
        setIsRenewModalOpen(false);
        fetchRenewals();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to process renewal.');
    } finally {
      setRenewing(false);
    }
  };

  // Pipeline of expiring software
  const expiringPipeline = licenses
    .filter((l) => l.daysRemaining <= 60)
    .sort((a, b) => a.daysRemaining - b.daysRemaining);

  const totalRenewalSpend = renewals.reduce((acc, r) => acc + (r.renewalAmount || 0), 0);

  const columns = [
    {
      header: 'Renewal ID',
      key: 'id',
      render: (row) => <span className="font-mono text-xs font-bold text-slate-700">#{row.id}</span>,
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
      header: 'Renewal Date',
      key: 'renewalDate',
      render: (row) => (
        <span className="text-xs text-slate-600 font-medium">
          {new Date(row.renewalDate).toLocaleDateString()}
        </span>
      ),
    },
    {
      header: 'Renewal Amount',
      key: 'renewalAmount',
      render: (row) => (
        <span className="font-bold text-emerald-700 text-xs">
          ${row.renewalAmount?.toLocaleString('en-US', { minimumFractionDigits: 2 })}
        </span>
      ),
    },
    {
      header: 'Validity Extension',
      key: 'newExpiryDate',
      render: (row) => (
        <div className="text-xs font-mono">
          <span className="text-slate-400 line-through mr-1">
            {new Date(row.previousExpiryDate).toLocaleDateString()}
          </span>
          &rarr;{' '}
          <span className="text-slate-900 font-bold">
            {new Date(row.newExpiryDate).toLocaleDateString()}
          </span>
        </div>
      ),
    },
    {
      header: 'Renewed By',
      key: 'renewedByUserName',
      render: (row) => <span className="text-xs text-slate-600">{row.renewedByUserName || 'Admin'}</span>,
    },
    {
      header: 'Status',
      key: 'status',
      render: (row) => <StatusBadge status={row.status} size="xs" />,
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-slate-900 tracking-tight">License Renewal Management</h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Monitor upcoming license expirations, process contract extensions, and review historical renewals
        </p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          title="Total Renewals Processed"
          value={totalCount}
          subtitle="Lifetime renewal transactions"
          icon={RefreshCw}
          color="indigo"
        />
        <KpiCard
          title="Upcoming Pipeline (< 60d)"
          value={expiringPipeline.length}
          subtitle="Software contracts expiring"
          icon={AlertTriangle}
          color={expiringPipeline.length > 0 ? 'amber' : 'emerald'}
        />
        <KpiCard
          title="Total Renewal Spend"
          value={`$${totalRenewalSpend.toLocaleString('en-US', { minimumFractionDigits: 2 })}`}
          subtitle="Processed renewals budget"
          icon={DollarSign}
          color="emerald"
        />
        <KpiCard
          title="Average Renewal Cycle"
          value="12 Months"
          subtitle="Annual subscription coverage"
          icon={Calendar}
          color="purple"
        />
      </div>

      {/* Upcoming Expiry Pipeline Section */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-amber-50 text-amber-600">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Upcoming Expiry & Action Pipeline</h3>
              <p className="text-xs text-slate-500">Software assets requiring renewal review within the next 60 days</p>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {expiringPipeline.length > 0 ? (
            expiringPipeline.map((lic) => {
              const days = lic.daysRemaining;
              const isExpired = days <= 0;

              return (
                <div
                  key={lic.id}
                  className={`p-5 rounded-2xl border transition flex flex-col justify-between ${
                    isExpired
                      ? 'bg-rose-50/60 border-rose-200'
                      : days <= 15
                      ? 'bg-amber-50/60 border-amber-200'
                      : 'bg-slate-50/70 border-slate-200'
                  }`}
                >
                  <div>
                    <div className="flex items-start justify-between gap-2 mb-2">
                      <span className="font-mono text-[10px] font-bold text-slate-500 bg-white/80 px-2 py-0.5 rounded">
                        {lic.licenseCode}
                      </span>
                      <StatusBadge status={lic.status} size="xs" />
                    </div>

                    <h4 className="text-sm font-bold text-slate-900">{lic.softwareName}</h4>
                    <p className="text-xs text-slate-500">{lic.vendor}</p>

                    <div className="mt-3 space-y-1.5 text-xs text-slate-600">
                      <div className="flex justify-between">
                        <span className="text-slate-400">Current Expiry:</span>
                        <span className="font-mono font-semibold text-slate-800">
                          {new Date(lic.expiryDate).toLocaleDateString()}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-slate-400">Est. Renewal:</span>
                        <span className="font-semibold text-slate-800">
                          ${lic.renewalCost?.toLocaleString()}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-slate-400">Assigned Users:</span>
                        <span className="font-semibold text-indigo-700">{lic.assignedCount} active staff</span>
                      </div>
                    </div>
                  </div>

                  <div className="mt-4 pt-3 border-t border-slate-200/60 flex items-center justify-between">
                    <span className={`text-xs font-bold ${isExpired ? 'text-rose-600' : 'text-amber-700'}`}>
                      {isExpired ? 'Contract Expired' : `${days} days left`}
                    </span>

                    {canManage && (
                      <button
                        onClick={() => handleOpenRenew(lic)}
                        className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center gap-1"
                      >
                        <RefreshCw className="w-3.5 h-3.5" />
                        Renew
                      </button>
                    )}
                  </div>
                </div>
              );
            })
          ) : (
            <div className="col-span-3 py-6 text-center text-xs text-slate-400">
              ✅ No software licenses expiring in the next 60 days. All contracts are currently active.
            </div>
          )}
        </div>
      </div>

      {/* Historical Renewals Table */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-bold text-slate-900">Renewal History Log</h3>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search renewal records..."
            className="max-w-xs"
          />
        </div>

        <DataTable
          columns={columns}
          data={renewals}
          loading={loading}
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={setPageNumber}
          onPageSizeChange={setPageSize}
          emptyTitle="No renewal history records"
          emptyDescription="No renewals have been processed matching the search query."
        />
      </div>

      {/* Process Renewal Modal */}
      <Modal
        isOpen={isRenewModalOpen}
        onClose={() => setIsRenewModalOpen(false)}
        title="Process Software License Renewal"
        subtitle={`Extending subscription contract for ${selectedLicense?.softwareName}`}
        maxWidth="max-w-xl"
      >
        <form onSubmit={handleExecuteRenewal} className="space-y-4">
          <div className="p-3.5 bg-indigo-50 rounded-2xl border border-indigo-100 text-xs text-indigo-950 space-y-1">
            <div className="font-bold text-indigo-900">{selectedLicense?.softwareName}</div>
            <div>Current Contract Expiry: <strong>{selectedLicense?.expiryDate ? new Date(selectedLicense.expiryDate).toLocaleDateString() : '—'}</strong></div>
            <div>Vendor: {selectedLicense?.vendor} &bull; Total Assigned Seats: {selectedLicense?.assignedCount}</div>
          </div>

          <FormField
            label="Renewal Amount ($)"
            type="number"
            required
            value={renewalAmount}
            onChange={(e) => setRenewalAmount(e.target.value)}
          />

          <FormField
            label="New Contract Expiry Date"
            type="date"
            required
            value={newExpiryDate}
            onChange={(e) => setNewExpiryDate(e.target.value)}
            helperText="Must be after the current license expiry date."
          />

          <FormField
            label="Remarks / Procurement Order Reference"
            type="textarea"
            placeholder="e.g. PO-2026-8812 approved by IT Operations..."
            value={renewalRemarks}
            onChange={(e) => setRenewalRemarks(e.target.value)}
          />

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsRenewModalOpen(false)}
              className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={renewing}
              className="px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition disabled:opacity-50 flex items-center gap-1.5"
            >
              {renewing ? 'Processing Renewal...' : 'Confirm & Execute Renewal'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
