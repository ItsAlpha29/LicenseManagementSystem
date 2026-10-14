import React, { useState, useEffect } from 'react';
import {
  KeyRound,
  Plus,
  Edit2,
  Trash2,
  Eye,
  Building,
  DollarSign,
  Calendar,
  AlertTriangle,
  Layers,
  Sparkles,
  Info,
  CheckCircle2,
  ShieldAlert,
  SearchCheck,
  TrendingDown,
  BrainCircuit,
  Copy,
} from 'lucide-react';
import { licenseService } from '../services/licenseService';
import { aiService } from '../services/aiService';
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

export const LicensesPage = () => {
  const [licenses, setLicenses] = useState([]);
  const [vendors, setVendors] = useState([]);
  const [licenseTypes, setLicenseTypes] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Selected License for quick side preview
  const [selectedPreview, setSelectedPreview] = useState(null);

  // Filters & Pagination
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ vendor: 'All', licenseType: 'All', status: 'All' });
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('software');
  const [sortDesc, setSortDesc] = useState(false);

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [selectedLicense, setSelectedLicense] = useState(null);
  const [saving, setSaving] = useState(false);

  // AI Duplicate Detection inside Add Modal
  const [duplicateCheck, setDuplicateCheck] = useState(null);
  const [checkingDuplicates, setCheckingDuplicates] = useState(false);
  const [showDuplicateDetails, setShowDuplicateDetails] = useState(false);

  // AI Redundancy Scanner Modal
  const [isRedundancyModalOpen, setIsRedundancyModalOpen] = useState(false);
  const [redundancyData, setRedundancyData] = useState(null);
  const [scanningRedundancy, setScanningRedundancy] = useState(false);

  const initialForm = {
    licenseCode: '',
    softwareName: '',
    licenseType: 'Subscription',
    vendor: '',
    purchaseDate: new Date().toISOString().split('T')[0],
    expiryDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    totalLicenses: 10,
    cost: 1200,
    billingCycle: 'Annual',
    renewalCost: 1200,
    licenseReference: '',
    status: 'Active',
    notes: '',
  };
  const [formData, setFormData] = useState(initialForm);

  const toast = useToast();
  const { role } = useAuth();
  const navigate = useNavigate();
  const isAdmin = role === 'ADMIN';
  const canManage = role === 'ADMIN' || role === 'IT_MANAGER';

  // Debounced duplicate check when adding a license
  useEffect(() => {
    if (!isAddModalOpen || !formData.softwareName || formData.softwareName.trim().length < 2) {
      setDuplicateCheck(null);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        setCheckingDuplicates(true);
        const res = await aiService.checkDuplicateLicense({
          softwareName: formData.softwareName,
          vendor: formData.vendor || '',
          licenseType: formData.licenseType,
          licenseReference: formData.licenseReference,
        });
        if (res.success && res.data) {
          setDuplicateCheck(res.data);
        }
      } catch {
        // ignore
      } finally {
        setCheckingDuplicates(false);
      }
    }, 400);

    return () => clearTimeout(timer);
  }, [formData.softwareName, formData.vendor, isAddModalOpen]);

  const handleOpenRedundancyScanner = async () => {
    setIsRedundancyModalOpen(true);
    setScanningRedundancy(true);
    try {
      const res = await aiService.scanPortfolioRedundancy();
      if (res.success && res.data) {
        setRedundancyData(res.data);
      }
    } catch {
      toast.error('Failed to run portfolio redundancy scan.');
    } finally {
      setScanningRedundancy(false);
    }
  };

  const handleOpenAdd = () => {
    setFormData(initialForm);
    setDuplicateCheck(null);
    setShowDuplicateDetails(false);
    setIsAddModalOpen(true);
  };

  const fetchLicenses = async () => {
    try {
      setLoading(true);
      const res = await licenseService.getLicenses({
        search,
        vendor: filters.vendor,
        licenseType: filters.licenseType,
        status: filters.status,
        pageNumber,
        pageSize,
        sortBy,
        sortDesc,
      });

      if (res.success && res.data) {
        setLicenses(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
        if (res.data.items?.length > 0 && !selectedPreview) {
          setSelectedPreview(res.data.items[0]);
        }
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load licenses.');
    } finally {
      setLoading(false);
    }
  };

  const fetchFilters = async () => {
    try {
      const [vRes, tRes] = await Promise.all([
        licenseService.getVendors(),
        licenseService.getLicenseTypes(),
      ]);
      if (vRes.success && vRes.data) setVendors(vRes.data);
      if (tRes.success && tRes.data) setLicenseTypes(tRes.data);
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    fetchFilters();
  }, []);

  useEffect(() => {
    fetchLicenses();
  }, [search, filters, pageNumber, pageSize, sortBy, sortDesc]);

  const handleFilterChange = (key, value) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPageNumber(1);
  };

  const handleResetFilters = () => {
    setSearch('');
    setFilters({ vendor: 'All', licenseType: 'All', status: 'All' });
    setPageNumber(1);
  };

  const handleOpenEdit = (lic) => {
    setSelectedLicense(lic);
    setFormData({
      softwareName: lic.softwareName,
      licenseType: lic.licenseType,
      vendor: lic.vendor,
      purchaseDate: lic.purchaseDate ? lic.purchaseDate.split('T')[0] : '',
      expiryDate: lic.expiryDate ? lic.expiryDate.split('T')[0] : '',
      totalLicenses: lic.totalLicenses,
      cost: lic.cost,
      billingCycle: lic.billingCycle,
      renewalCost: lic.renewalCost,
      licenseReference: lic.licenseReference || '',
      status: lic.status,
      notes: lic.notes || '',
    });
    setIsEditModalOpen(true);
  };

  const handleSaveAdd = async (e) => {
    e.preventDefault();
    if (!formData.licenseCode || !formData.softwareName || !formData.vendor) {
      toast.error('Please complete required fields.');
      return;
    }

    setSaving(true);
    try {
      const res = await licenseService.createLicense(formData);
      if (res.success) {
        toast.success(`License ${formData.softwareName} created successfully.`);
        setIsAddModalOpen(false);
        fetchLicenses();
        fetchFilters();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to add software license.');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveEdit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      const res = await licenseService.updateLicense(selectedLicense.id, formData);
      if (res.success) {
        toast.success(`License updated successfully.`);
        setIsEditModalOpen(false);
        fetchLicenses();
        if (selectedPreview?.id === selectedLicense.id) {
          setSelectedPreview(res.data);
        }
      }
    } catch (err) {
      toast.error(err.message || 'Failed to update license.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedLicense) return;
    setSaving(true);
    try {
      const res = await licenseService.deleteLicense(selectedLicense.id);
      if (res.success) {
        toast.success('Software license deleted successfully.');
        setIsDeleteDialogOpen(false);
        setSelectedPreview(null);
        fetchLicenses();
      }
    } catch (err) {
      toast.error(err.message || 'Failed to delete license.');
    } finally {
      setSaving(false);
    }
  };

  const columns = [
    {
      header: 'Code',
      key: 'licenseCode',
      sortable: true,
      render: (row) => (
        <span className="font-mono font-bold text-slate-900 bg-slate-100 px-2 py-0.5 rounded-md text-xs">
          {row.licenseCode}
        </span>
      ),
    },
    {
      header: 'Software & Vendor',
      key: 'softwareName',
      sortable: true,
      render: (row) => (
        <div>
          <div className="font-bold text-slate-900">{row.softwareName}</div>
          <div className="text-xs text-slate-400">{row.vendor} &bull; {row.licenseType}</div>
        </div>
      ),
    },
    {
      header: 'Seats (Assigned / Total)',
      key: 'totalLicenses',
      sortable: true,
      render: (row) => (
        <div>
          <div className="flex items-center gap-2">
            <span className="font-bold text-slate-900">{row.assignedCount}</span>
            <span className="text-slate-400">/</span>
            <span className="font-semibold text-slate-600">{row.totalLicenses}</span>
            <span className="text-xs text-slate-400">({row.utilizationPercentage}%)</span>
          </div>
          {/* Seat Progress Bar */}
          <div className="w-28 h-1.5 bg-slate-100 rounded-full overflow-hidden mt-1">
            <div
              className={`h-full rounded-full ${
                row.utilizationPercentage >= 90
                  ? 'bg-rose-500'
                  : row.utilizationPercentage >= 70
                  ? 'bg-amber-500'
                  : 'bg-indigo-600'
              }`}
              style={{ width: `${Math.min(row.utilizationPercentage, 100)}%` }}
            />
          </div>
        </div>
      ),
    },
    {
      header: 'Available',
      key: 'availableCount',
      sortable: true,
      align: 'center',
      render: (row) => (
        <span
          className={`inline-flex items-center justify-center px-2.5 py-0.5 rounded-full text-xs font-bold ${
            row.availableCount > 5
              ? 'bg-emerald-50 text-emerald-700'
              : row.availableCount > 0
              ? 'bg-amber-50 text-amber-700'
              : 'bg-rose-50 text-rose-700'
          }`}
        >
          {row.availableCount}
        </span>
      ),
    },
    {
      header: 'Expiry Date',
      key: 'expiryDate',
      sortable: true,
      render: (row) => {
        const days = row.daysRemaining;
        return (
          <div>
            <div className="font-mono text-xs text-slate-800 font-semibold">
              {new Date(row.expiryDate).toLocaleDateString()}
            </div>
            <div className={`text-[10px] font-medium ${days < 0 ? 'text-rose-600' : days <= 30 ? 'text-amber-600' : 'text-slate-400'}`}>
              {days < 0 ? `Expired ${Math.abs(days)}d ago` : `${days} days remaining`}
            </div>
          </div>
        );
      },
    },
    {
      header: 'Annual Cost',
      key: 'cost',
      sortable: true,
      render: (row) => (
        <span className="font-semibold text-slate-900">
          ${row.cost?.toLocaleString('en-US', { minimumFractionDigits: 2 })}
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
            onClick={() => setSelectedPreview(row)}
            className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-slate-100 rounded-lg transition"
            title="Preview Details"
          >
            <Eye className="w-4 h-4" />
          </button>

          {canManage && (
            <button
              onClick={() => handleOpenEdit(row)}
              className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-slate-100 rounded-lg transition"
              title="Edit License"
            >
              <Edit2 className="w-4 h-4" />
            </button>
          )}

          {isAdmin && (
            <button
              onClick={() => {
                setSelectedLicense(row);
                setIsDeleteDialogOpen(true);
              }}
              className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
              title="Delete License"
            >
              <Trash2 className="w-4 h-4" />
            </button>
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
          <h1 className="text-xl font-bold text-slate-900 tracking-tight">License Master Inventory</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Central repository of software assets, seat allotments, and vendor contracts
          </p>
        </div>

        {canManage && (
          <div className="flex items-center gap-2">
            <button
              onClick={handleOpenRedundancyScanner}
              className="inline-flex items-center gap-2 px-3.5 py-2.5 bg-purple-50 hover:bg-purple-100 border border-purple-200 text-purple-800 rounded-xl text-xs font-semibold shadow-xs transition"
            >
              <Sparkles className="w-4 h-4 text-purple-600 fill-purple-500" />
              AI Redundancy Scan
            </button>
            <button
              onClick={handleOpenAdd}
              className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition"
            >
              <Plus className="w-4 h-4" />
              Add Software License
            </button>
          </div>
        )}
      </div>

      {/* Main Grid: Table (Left 8 cols) & Right-side License Summary (4 cols) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <div className="lg:col-span-8 space-y-4">
          {/* Search & Filter Bar */}
          <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs space-y-3">
            <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
              <div className="md:col-span-5">
                <SearchBar
                  value={search}
                  onChange={setSearch}
                  placeholder="Search by software name, vendor, license code..."
                />
              </div>
              <div className="md:col-span-7">
                <FilterBar
                  filters={[
                    { key: 'vendor', label: 'Vendor', options: vendors },
                    { key: 'licenseType', label: 'Type', options: licenseTypes },
                    { key: 'status', label: 'Status', options: ['Active', 'ExpiringSoon', 'Expired', 'Deprecated'] },
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
            data={licenses}
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
            emptyTitle="No licenses found"
            emptyDescription="No software licenses match the selected search or filter parameters."
          />
        </div>

        {/* Right-Side License Summary Card */}
        <div className="lg:col-span-4 sticky top-24">
          {selectedPreview ? (
            <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-5 animate-fade-in">
              <div className="flex items-start justify-between border-b border-slate-100 pb-4">
                <div>
                  <span className="text-[10px] font-bold uppercase tracking-wider text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded-md font-mono">
                    {selectedPreview.licenseCode}
                  </span>
                  <h3 className="text-lg font-bold text-slate-900 mt-1.5">{selectedPreview.softwareName}</h3>
                  <p className="text-xs text-slate-500 font-medium">{selectedPreview.vendor}</p>
                </div>
                <StatusBadge status={selectedPreview.status} size="xs" />
              </div>

              {/* Seats Metrics Grid */}
              <div className="grid grid-cols-3 gap-2 text-center p-3 bg-slate-50 rounded-2xl border border-slate-100">
                <div>
                  <span className="text-[10px] font-semibold text-slate-400 uppercase">Total</span>
                  <div className="text-lg font-black text-slate-800 mt-0.5">{selectedPreview.totalLicenses}</div>
                </div>
                <div>
                  <span className="text-[10px] font-semibold text-indigo-600 uppercase">Assigned</span>
                  <div className="text-lg font-black text-indigo-600 mt-0.5">{selectedPreview.assignedCount}</div>
                </div>
                <div>
                  <span className="text-[10px] font-semibold text-emerald-600 uppercase">Available</span>
                  <div className="text-lg font-black text-emerald-600 mt-0.5">{selectedPreview.availableCount}</div>
                </div>
              </div>

              {/* Progress Utilization */}
              <div>
                <div className="flex justify-between text-xs font-semibold mb-1.5">
                  <span className="text-slate-600">Seat Utilization</span>
                  <span className="text-indigo-600">{selectedPreview.utilizationPercentage}%</span>
                </div>
                <div className="w-full h-2 bg-slate-100 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-gradient-to-r from-indigo-500 to-purple-600 rounded-full"
                    style={{ width: `${Math.min(selectedPreview.utilizationPercentage, 100)}%` }}
                  />
                </div>
              </div>

              {/* Key Specs List */}
              <div className="space-y-2.5 text-xs text-slate-600 border-t border-slate-100 pt-4">
                <div className="flex justify-between">
                  <span className="text-slate-400">License Type:</span>
                  <span className="font-semibold text-slate-800">{selectedPreview.licenseType}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-400">Contract Expiry:</span>
                  <span className="font-semibold text-slate-800">
                    {new Date(selectedPreview.expiryDate).toLocaleDateString()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-400">Annual Cost:</span>
                  <span className="font-semibold text-slate-800">${selectedPreview.cost?.toLocaleString()}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-400">Renewal Amount:</span>
                  <span className="font-semibold text-slate-800">${selectedPreview.renewalCost?.toLocaleString()}</span>
                </div>
                {selectedPreview.licenseReference && (
                  <div className="flex justify-between">
                    <span className="text-slate-400">Key Reference:</span>
                    <span className="font-mono text-slate-700">{selectedPreview.licenseReference}</span>
                  </div>
                )}
              </div>

              {/* Notes */}
              {selectedPreview.notes && (
                <div className="p-3 bg-slate-50 rounded-xl text-xs text-slate-600 leading-relaxed border border-slate-100">
                  <span className="font-bold text-slate-800 block mb-0.5">Notes:</span>
                  {selectedPreview.notes}
                </div>
              )}

              {/* Action Buttons */}
              <div className="pt-2 flex gap-2">
                <button
                  onClick={() => navigate(`/licenses/${selectedPreview.id}`)}
                  className="flex-1 py-2.5 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-semibold transition text-center shadow-xs"
                >
                  Full License Details & History
                </button>
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-3xl p-8 border border-slate-200/80 text-center text-xs text-slate-400 shadow-xs">
              Select any license from the table to view real-time seat metrics and contract summary.
            </div>
          )}
        </div>
      </div>

      {/* Add License Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Add Software License"
        subtitle="Register a new software license asset into organizational inventory"
        maxWidth="max-w-2xl"
      >
        <form onSubmit={handleSaveAdd} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              label="License Code"
              required
              placeholder="e.g. LIC-SLK-ENT"
              value={formData.licenseCode}
              onChange={(e) => setFormData({ ...formData, licenseCode: e.target.value })}
            />
            <FormField
              label="Software Name"
              required
              placeholder="e.g. Slack Enterprise Grid"
              value={formData.softwareName}
              onChange={(e) => setFormData({ ...formData, softwareName: e.target.value })}
            />
            <FormField
              label="Vendor"
              required
              placeholder="e.g. Salesforce / Slack"
              value={formData.vendor}
              onChange={(e) => setFormData({ ...formData, vendor: e.target.value })}
            />
            <FormField
              label="License Type"
              type="select"
              required
              options={['Subscription', 'Perpetual', 'OpenSource', 'OEM']}
              value={formData.licenseType}
              onChange={(e) => setFormData({ ...formData, licenseType: e.target.value })}
            />
            <FormField
              label="Total Seats"
              type="number"
              required
              value={formData.totalLicenses}
              onChange={(e) => setFormData({ ...formData, totalLicenses: Number(e.target.value) })}
            />
            <FormField
              label="Purchase Date"
              type="date"
              value={formData.purchaseDate}
              onChange={(e) => setFormData({ ...formData, purchaseDate: e.target.value })}
            />
            <FormField
              label="Expiry Date"
              type="date"
              required
              value={formData.expiryDate}
              onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
            />
            <FormField
              label="Total Contract Cost ($)"
              type="number"
              value={formData.cost}
              onChange={(e) => setFormData({ ...formData, cost: Number(e.target.value) })}
            />
            <FormField
              label="Billing Cycle"
              type="select"
              options={['Annual', 'Monthly', 'Quarterly', 'OneTime']}
              value={formData.billingCycle}
              onChange={(e) => setFormData({ ...formData, billingCycle: e.target.value })}
            />
            <FormField
              label="Projected Renewal Amount ($)"
              type="number"
              value={formData.renewalCost}
              onChange={(e) => setFormData({ ...formData, renewalCost: Number(e.target.value) })}
            />
            <FormField
              label="License Reference (Masked)"
              placeholder="e.g. SLACK-CORP-****-5521"
              value={formData.licenseReference}
              onChange={(e) => setFormData({ ...formData, licenseReference: e.target.value })}
            />
            <FormField
              label="Status"
              type="select"
              options={['Active', 'ExpiringSoon', 'Expired', 'Deprecated']}
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: e.target.value })}
            />
          </div>

          {/* AI Duplicate Detection Live Indicator / Warning */}
          {checkingDuplicates && (
            <div className="p-2.5 bg-purple-50/70 rounded-xl border border-purple-200/60 flex items-center gap-2 text-xs text-purple-700 animate-pulse">
              <BrainCircuit className="w-4 h-4 animate-spin text-purple-600" />
              <span>AI checking for duplicate software or existing vendor contracts...</span>
            </div>
          )}

          {duplicateCheck && duplicateCheck.matches && duplicateCheck.matches.length > 0 && (
            <div className={`p-3.5 rounded-2xl border text-xs space-y-2.5 transition animate-fade-in ${
              duplicateCheck.highestSimilarityPercentage >= 75
                ? 'bg-rose-50/80 border-rose-200 text-rose-950'
                : 'bg-amber-50/80 border-amber-200 text-amber-950'
            }`}>
              <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-2 font-bold">
                  <ShieldAlert className={`w-4 h-4 shrink-0 ${
                    duplicateCheck.highestSimilarityPercentage >= 75 ? 'text-rose-600' : 'text-amber-600'
                  }`} />
                  <span>
                    {duplicateCheck.highestSimilarityPercentage >= 95
                      ? 'AI Duplicate Alert: Exact License Already Exists'
                      : `AI Warning: Potential Duplicate Detected (${duplicateCheck.highestSimilarityPercentage}% match)`}
                  </span>
                </div>
                <button
                  type="button"
                  onClick={() => setShowDuplicateDetails(!showDuplicateDetails)}
                  className="text-[11px] font-bold text-indigo-700 hover:underline shrink-0"
                >
                  {showDuplicateDetails ? 'Hide Matches' : 'View Matches'}
                </button>
              </div>

              <p className="text-[11px] leading-relaxed text-slate-700">
                {duplicateCheck.assessment.replace(/[*_#]/g, '')}
              </p>

              {showDuplicateDetails && (
                <div className="space-y-2 pt-2 border-t border-slate-200/70">
                  {duplicateCheck.matches.map((m) => (
                    <div key={m.existingLicenseId} className="p-2.5 bg-white rounded-xl border border-slate-200 flex items-center justify-between text-xs">
                      <div>
                        <div className="font-bold text-slate-900">{m.existingSoftwareName} ({m.licenseCode})</div>
                        <div className="text-[10px] text-slate-500">{m.vendor} &bull; {m.availableSeats}/{m.totalSeats} seats available &bull; Status: {m.status}</div>
                        <div className="text-[10px] text-indigo-600 font-medium mt-0.5">💡 {m.recommendedAction}</div>
                      </div>
                      <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                        m.similarityPercentage >= 75 ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700'
                      }`}>
                        {m.similarityPercentage}% match
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          <FormField
            label="Notes & Administration Guidance"
            type="textarea"
            placeholder="Usage terms, procurement contract ID, or contact person..."
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
          />

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
              {saving ? 'Creating...' : 'Create License'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Edit License Modal */}
      <Modal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        title="Edit Software License"
        subtitle={`Updating details for ${selectedLicense?.softwareName} (${selectedLicense?.licenseCode})`}
        maxWidth="max-w-2xl"
      >
        <form onSubmit={handleSaveEdit} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              label="Software Name"
              required
              value={formData.softwareName}
              onChange={(e) => setFormData({ ...formData, softwareName: e.target.value })}
            />
            <FormField
              label="Vendor"
              required
              value={formData.vendor}
              onChange={(e) => setFormData({ ...formData, vendor: e.target.value })}
            />
            <FormField
              label="License Type"
              type="select"
              required
              options={['Subscription', 'Perpetual', 'OpenSource', 'OEM']}
              value={formData.licenseType}
              onChange={(e) => setFormData({ ...formData, licenseType: e.target.value })}
            />
            <FormField
              label="Total Seats"
              type="number"
              required
              value={formData.totalLicenses}
              onChange={(e) => setFormData({ ...formData, totalLicenses: Number(e.target.value) })}
              helperText={`Currently assigned: ${selectedLicense?.assignedCount || 0} seats`}
            />
            <FormField
              label="Expiry Date"
              type="date"
              required
              value={formData.expiryDate}
              onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
            />
            <FormField
              label="Annual Cost ($)"
              type="number"
              value={formData.cost}
              onChange={(e) => setFormData({ ...formData, cost: Number(e.target.value) })}
            />
            <FormField
              label="Renewal Cost ($)"
              type="number"
              value={formData.renewalCost}
              onChange={(e) => setFormData({ ...formData, renewalCost: Number(e.target.value) })}
            />
            <FormField
              label="Status"
              type="select"
              options={['Active', 'ExpiringSoon', 'Expired', 'Deprecated']}
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: e.target.value })}
            />
          </div>

          <FormField
            label="Notes"
            type="textarea"
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
          />

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

      {/* Delete Dialog */}
      <ConfirmDialog
        isOpen={isDeleteDialogOpen}
        onClose={() => setIsDeleteDialogOpen(false)}
        onConfirm={handleDelete}
        title="Delete Software License"
        type="danger"
        message={`Are you sure you want to permanently delete '${selectedLicense?.softwareName}' (${selectedLicense?.licenseCode})? Licenses with active assignments cannot be deleted.`}
        confirmText="Delete License"
        loading={saving}
      />

      {/* AI Portfolio Redundancy Scanner Modal */}
      <Modal
        isOpen={isRedundancyModalOpen}
        onClose={() => setIsRedundancyModalOpen(false)}
        title="AI License Redundancy & Consolidation Audit"
        subtitle="Portfolio-wide scan detecting overlapping multi-vendor software and cost saving opportunities"
        maxWidth="max-w-3xl"
      >
        <div className="space-y-5">
          {scanningRedundancy && (
            <div className="p-8 text-center space-y-3">
              <BrainCircuit className="w-8 h-8 text-purple-600 animate-spin mx-auto" />
              <p className="text-sm font-semibold text-slate-700">Scanning all software assets across categories...</p>
              <p className="text-xs text-slate-400">Comparing vendors, application categories, and seat densities</p>
            </div>
          )}

          {redundancyData && (
            <>
              {/* Summary KPIs */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="p-4 bg-slate-50 rounded-2xl border border-slate-100">
                  <span className="text-[10px] uppercase font-bold text-slate-400 block">Licenses Scanned</span>
                  <span className="text-2xl font-bold text-slate-900">{redundancyData.totalLicensesScanned}</span>
                </div>
                <div className="p-4 bg-purple-50 rounded-2xl border border-purple-100">
                  <span className="text-[10px] uppercase font-bold text-purple-600 block">Overlap Groups</span>
                  <span className="text-2xl font-bold text-purple-900">{redundancyData.redundantGroupsFound}</span>
                </div>
                <div className="p-4 bg-emerald-50 rounded-2xl border border-emerald-100">
                  <span className="text-[10px] uppercase font-bold text-emerald-600 block">Est. Annual Savings</span>
                  <span className="text-2xl font-bold text-emerald-700">${redundancyData.estimatedAnnualSavings?.toLocaleString()}</span>
                </div>
              </div>

              {/* Redundancy Groups */}
              <div className="space-y-3 max-h-96 overflow-y-auto pr-1">
                {redundancyData.redundancyGroups?.length > 0 ? (
                  redundancyData.redundancyGroups.map((group, idx) => (
                    <div key={idx} className="p-4 bg-white rounded-2xl border border-slate-200 shadow-xs space-y-3">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <Layers className="w-4 h-4 text-purple-600" />
                            <h4 className="font-bold text-slate-900 text-sm">{group.categoryOrFunction}</h4>
                          </div>
                          <p className="text-xs text-slate-500 mt-0.5">{group.description}</p>
                        </div>
                        <div className="text-right shrink-0">
                          <span className="text-xs font-bold text-emerald-600 bg-emerald-50 px-2 py-1 rounded-lg">
                            Save ~${group.potentialSavings?.toLocaleString()}/yr
                          </span>
                        </div>
                      </div>

                      {/* Overlapping Licenses Pills */}
                      <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 space-y-1.5 text-xs">
                        <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block">
                          Overlapping Software in Category:
                        </span>
                        <div className="flex flex-wrap gap-2">
                          {group.overlappingLicenses.map((lic) => (
                            <span key={lic.existingLicenseId} className="px-2.5 py-1 bg-white border border-slate-200 rounded-lg text-slate-800 font-semibold flex items-center gap-1.5">
                              <span>{lic.existingSoftwareName}</span>
                              <span className="text-slate-400 font-mono text-[10px]">({lic.vendor})</span>
                            </span>
                          ))}
                        </div>
                      </div>

                      {/* Consolidation Advice */}
                      <div className="flex items-start gap-2 text-xs text-purple-900 bg-purple-50/60 p-3 rounded-xl border border-purple-100">
                        <Sparkles className="w-4 h-4 text-purple-600 shrink-0 mt-0.5 fill-purple-500" />
                        <div>
                          <strong className="block text-purple-950 font-bold">AI Consolidation Strategy:</strong>
                          <span className="text-purple-800">{group.consolidationAdvice}</span>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="p-6 bg-emerald-50 rounded-2xl border border-emerald-100 text-center text-xs text-emerald-800 font-medium">
                    ✅ Excellent software portfolio hygiene! No duplicate or overlapping vendor contracts detected.
                  </div>
                )}
              </div>

              <div className="flex justify-end pt-2">
                <button
                  type="button"
                  onClick={() => setIsRedundancyModalOpen(false)}
                  className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-semibold"
                >
                  Close Audit
                </button>
              </div>
            </>
          )}
        </div>
      </Modal>
    </div>
  );
};
