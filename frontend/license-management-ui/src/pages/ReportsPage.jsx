import React, { useState, useEffect } from 'react';
import {
  BarChart3,
  Download,
  Printer,
  Calendar,
  Filter,
  Layers,
  Clock,
  Users,
  DollarSign,
  RefreshCw,
  Building2,
  Search,
} from 'lucide-react';
import { reportService } from '../services/reportService';
import { licenseService } from '../services/licenseService';
import { employeeService } from '../services/employeeService';
import { StatusBadge } from '../components/StatusBadge';
import { SearchBar } from '../components/SearchBar';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { useToast } from '../context/ToastContext';

export const ReportsPage = () => {
  const [activeReport, setActiveReport] = useState('utilization');
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);

  // Filters
  const [vendor, setVendor] = useState('All');
  const [department, setDepartment] = useState('All');
  const [status, setStatus] = useState('All');
  const [searchTerm, setSearchTerm] = useState('');
  const [vendorsList, setVendorsList] = useState([]);
  const [departmentsList, setDepartmentsList] = useState([]);

  const toast = useToast();

  const reportTabs = [
    { id: 'utilization', label: 'License Utilization', icon: Layers, desc: 'Seat allocations vs total capacity & unit costs' },
    { id: 'expiry', label: 'License Expiry', icon: Clock, desc: 'Upcoming renewal milestones & urgency levels' },
    { id: 'employee', label: 'Employee Licenses', icon: Users, desc: 'All software assigned by staff member' },
    { id: 'cost', label: 'Cost Analysis', icon: DollarSign, desc: 'Annual spend & billing cycles per software' },
    { id: 'renewal', label: 'Renewal History', icon: RefreshCw, desc: 'Historical contract renewals & extensions' },
    { id: 'department', label: 'Department Breakdown', icon: Building2, desc: 'License consumption grouped by department' },
  ];

  const fetchFilters = async () => {
    try {
      const [vRes, dRes] = await Promise.all([
        licenseService.getVendors(),
        employeeService.getDepartments(),
      ]);
      if (vRes.success && vRes.data) setVendorsList(vRes.data);
      if (dRes.success && dRes.data) setDepartmentsList(dRes.data);
    } catch {
      // ignore
    }
  };

  const fetchReportData = async () => {
    try {
      setLoading(true);
      const filterParams = {
        vendor,
        department,
        status,
        searchTerm,
      };

      let res;
      switch (activeReport) {
        case 'utilization':
          res = await reportService.getLicenseUtilization(filterParams);
          break;
        case 'expiry':
          res = await reportService.getExpiryReport(filterParams);
          break;
        case 'employee':
          res = await reportService.getEmployeeLicensesReport(filterParams);
          break;
        case 'cost':
          res = await reportService.getCostReport(filterParams);
          break;
        case 'renewal':
          res = await reportService.getRenewalsReport(filterParams);
          break;
        case 'department':
          res = await reportService.getDepartmentReport(filterParams);
          break;
        default:
          res = await reportService.getLicenseUtilization(filterParams);
      }

      if (res.success && res.data) {
        setData(res.data || []);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load report data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFilters();
  }, []);

  useEffect(() => {
    fetchReportData();
  }, [activeReport, vendor, department, status, searchTerm]);

  const handleExportCsv = async () => {
    try {
      setExporting(true);
      toast.info('Generating CSV export...');
      await reportService.exportCsv(activeReport, {
        vendor: vendor !== 'All' ? vendor : undefined,
        department: department !== 'All' ? department : undefined,
        status: status !== 'All' ? status : undefined,
        searchTerm: searchTerm || undefined,
      });
      toast.success('Report successfully exported.');
    } catch (err) {
      toast.error(err.message || 'Failed to export report.');
    } finally {
      setExporting(false);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 no-print">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight">Enterprise Reports & Analytics</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Generate audit-ready reports, analyze utilization efficiency, and export filtered datasets
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handlePrint}
            className="px-3.5 py-2 bg-white hover:bg-slate-50 text-slate-700 rounded-xl text-xs font-semibold border border-slate-200 shadow-2xs transition flex items-center gap-1.5"
          >
            <Printer className="w-4 h-4" />
            Print Report
          </button>
          <button
            onClick={handleExportCsv}
            disabled={exporting || loading || data.length === 0}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center gap-1.5 disabled:opacity-50"
          >
            <Download className="w-4 h-4" />
            {exporting ? 'Exporting...' : 'Export CSV'}
          </button>
        </div>
      </div>

      {/* Report Types Navigation Tabs */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2.5 no-print">
        {reportTabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeReport === tab.id;

          return (
            <button
              key={tab.id}
              onClick={() => setActiveReport(tab.id)}
              className={`p-3 rounded-2xl border text-left transition flex flex-col justify-between ${
                isActive
                  ? 'bg-indigo-600 text-white border-indigo-600 shadow-md shadow-indigo-600/20'
                  : 'bg-white text-slate-700 border-slate-200/80 hover:bg-slate-50'
              }`}
            >
              <div className="flex items-center justify-between mb-2">
                <Icon className={`w-4 h-4 ${isActive ? 'text-white' : 'text-indigo-600'}`} />
                {isActive && <span className="w-1.5 h-1.5 rounded-full bg-white animate-ping" />}
              </div>
              <div>
                <div className={`text-xs font-bold ${isActive ? 'text-white' : 'text-slate-900'}`}>{tab.label}</div>
                <div className={`text-[10px] mt-0.5 line-clamp-1 ${isActive ? 'text-indigo-100' : 'text-slate-400'}`}>
                  {tab.desc}
                </div>
              </div>
            </button>
          );
        })}
      </div>

      {/* Filter Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs space-y-3 no-print">
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex-1 min-w-[200px]">
            <SearchBar
              value={searchTerm}
              onChange={setSearchTerm}
              placeholder="Search in report data..."
            />
          </div>

          {(activeReport === 'utilization' || activeReport === 'expiry' || activeReport === 'cost') && (
            <select
              value={vendor}
              onChange={(e) => setVendor(e.target.value)}
              className="text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-xl px-3 py-2 focus:ring-2 focus:ring-indigo-500/20"
            >
              <option value="All">All Vendors</option>
              {vendorsList.map((v) => (
                <option key={v} value={v}>{v}</option>
              ))}
            </select>
          )}

          {(activeReport === 'employee' || activeReport === 'department') && (
            <select
              value={department}
              onChange={(e) => setDepartment(e.target.value)}
              className="text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-xl px-3 py-2 focus:ring-2 focus:ring-indigo-500/20"
            >
              <option value="All">All Departments</option>
              {departmentsList.map((d) => (
                <option key={d} value={d}>{d}</option>
              ))}
            </select>
          )}

          {(activeReport === 'utilization' || activeReport === 'expiry' || activeReport === 'employee') && (
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-xl px-3 py-2 focus:ring-2 focus:ring-indigo-500/20"
            >
              <option value="All">All Statuses</option>
              <option value="Active">Active</option>
              <option value="ExpiringSoon">Expiring Soon</option>
              <option value="Expired">Expired</option>
            </select>
          )}
        </div>
      </div>

      {/* Report Data Preview Card */}
      <div className="bg-white rounded-3xl border border-slate-200/80 shadow-xs overflow-hidden">
        {/* Print Header */}
        <div className="hidden print-only p-6 border-b border-slate-200">
          <h2 className="text-xl font-bold text-slate-900">License Management System — Enterprise Report</h2>
          <p className="text-xs text-slate-500">Report Type: {reportTabs.find(t => t.id === activeReport)?.label} | Generated on: {new Date().toLocaleString()}</p>
        </div>

        <div className="p-4 sm:p-6 border-b border-slate-100 flex items-center justify-between no-print">
          <div>
            <h3 className="text-base font-bold text-slate-900">
              {reportTabs.find((t) => t.id === activeReport)?.label}
            </h3>
            <p className="text-xs text-slate-500">
              Displaying {data.length} record(s) based on current filter parameters
            </p>
          </div>
        </div>

        <div className="overflow-x-auto">
          {loading ? (
            <div className="p-8">
              <LoadingSkeleton count={1} type="table" />
            </div>
          ) : data.length > 0 ? (
            activeReport === 'utilization' ? (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">License Code</th>
                    <th className="p-3.5">Software Name</th>
                    <th className="p-3.5">Vendor</th>
                    <th className="p-3.5 text-center">Total Seats</th>
                    <th className="p-3.5 text-center">Assigned</th>
                    <th className="p-3.5 text-center">Available</th>
                    <th className="p-3.5 text-center">Utilization</th>
                    <th className="p-3.5">Unit Cost</th>
                    <th className="p-3.5">Total Cost</th>
                    <th className="p-3.5">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5 font-mono font-bold text-slate-800">{item.licenseCode}</td>
                      <td className="p-3.5 font-bold text-slate-900">{item.softwareName}</td>
                      <td className="p-3.5 text-slate-600">{item.vendor}</td>
                      <td className="p-3.5 text-center font-semibold">{item.totalLicenses}</td>
                      <td className="p-3.5 text-center font-bold text-indigo-600">{item.assignedCount}</td>
                      <td className="p-3.5 text-center font-bold text-emerald-600">{item.availableCount}</td>
                      <td className="p-3.5 text-center font-bold">{item.utilizationRate}%</td>
                      <td className="p-3.5 font-semibold text-slate-700">${item.unitCost}</td>
                      <td className="p-3.5 font-bold text-slate-900">${item.totalCost?.toLocaleString()}</td>
                      <td className="p-3.5"><StatusBadge status={item.status} size="xs" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : activeReport === 'expiry' ? (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">License Code</th>
                    <th className="p-3.5">Software</th>
                    <th className="p-3.5">Vendor</th>
                    <th className="p-3.5">Expiry Date</th>
                    <th className="p-3.5">Days Remaining</th>
                    <th className="p-3.5 text-center">Assigned Users</th>
                    <th className="p-3.5">Est. Renewal Cost</th>
                    <th className="p-3.5">Urgency Level</th>
                    <th className="p-3.5">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5 font-mono font-bold text-slate-800">{item.licenseCode}</td>
                      <td className="p-3.5 font-bold text-slate-900">{item.softwareName}</td>
                      <td className="p-3.5 text-slate-600">{item.vendor}</td>
                      <td className="p-3.5 font-mono">{new Date(item.expiryDate).toLocaleDateString()}</td>
                      <td className="p-3.5 font-bold">{item.daysRemaining} days</td>
                      <td className="p-3.5 text-center font-semibold text-indigo-600">{item.assignedCount}</td>
                      <td className="p-3.5 font-bold text-emerald-700">${item.renewalCost?.toLocaleString()}</td>
                      <td className="p-3.5 font-semibold text-amber-700">{item.urgencyLevel}</td>
                      <td className="p-3.5"><StatusBadge status={item.status} size="xs" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : activeReport === 'employee' ? (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">Code</th>
                    <th className="p-3.5">Employee Name</th>
                    <th className="p-3.5">Department</th>
                    <th className="p-3.5">Designation</th>
                    <th className="p-3.5">Software</th>
                    <th className="p-3.5">License Code</th>
                    <th className="p-3.5">Assigned Date</th>
                    <th className="p-3.5">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5 font-mono font-bold text-slate-700">{item.employeeCode}</td>
                      <td className="p-3.5 font-bold text-slate-900">{item.employeeName}</td>
                      <td className="p-3.5 text-slate-700">{item.department}</td>
                      <td className="p-3.5 text-slate-500">{item.designation}</td>
                      <td className="p-3.5 font-bold text-indigo-950">{item.softwareName}</td>
                      <td className="p-3.5 font-mono text-slate-600">{item.licenseCode}</td>
                      <td className="p-3.5">{new Date(item.assignDate).toLocaleDateString()}</td>
                      <td className="p-3.5"><StatusBadge status={item.status} size="xs" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : activeReport === 'cost' ? (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">Category</th>
                    <th className="p-3.5">Software</th>
                    <th className="p-3.5">Vendor</th>
                    <th className="p-3.5">Billing Cycle</th>
                    <th className="p-3.5 text-center">Seats</th>
                    <th className="p-3.5">Unit Cost</th>
                    <th className="p-3.5">Annual Spend</th>
                    <th className="p-3.5">Renewal Forecast</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5"><StatusBadge status={item.category} size="xs" /></td>
                      <td className="p-3.5 font-bold text-slate-900">{item.softwareName}</td>
                      <td className="p-3.5 text-slate-600">{item.vendor}</td>
                      <td className="p-3.5 text-slate-600 font-medium">{item.billingCycle}</td>
                      <td className="p-3.5 text-center font-bold">{item.totalLicenses}</td>
                      <td className="p-3.5 font-semibold text-slate-700">${item.unitCost}</td>
                      <td className="p-3.5 font-bold text-slate-900">${item.annualCost?.toLocaleString()}</td>
                      <td className="p-3.5 font-bold text-emerald-700">${item.renewalCost?.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : activeReport === 'renewal' ? (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">Renewal ID</th>
                    <th className="p-3.5">Software</th>
                    <th className="p-3.5">Vendor</th>
                    <th className="p-3.5">Renewal Date</th>
                    <th className="p-3.5">Renewal Amount</th>
                    <th className="p-3.5">New Expiry Date</th>
                    <th className="p-3.5">Renewed By</th>
                    <th className="p-3.5">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5 font-mono font-bold">#{item.renewalId}</td>
                      <td className="p-3.5 font-bold text-slate-900">{item.softwareName}</td>
                      <td className="p-3.5 text-slate-600">{item.vendor}</td>
                      <td className="p-3.5">{new Date(item.renewalDate).toLocaleDateString()}</td>
                      <td className="p-3.5 font-bold text-emerald-700">${item.renewalAmount?.toLocaleString()}</td>
                      <td className="p-3.5 font-mono font-bold">{new Date(item.newExpiryDate).toLocaleDateString()}</td>
                      <td className="p-3.5 text-slate-600">{item.renewedBy}</td>
                      <td className="p-3.5"><StatusBadge status={item.status} size="xs" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase">
                  <tr>
                    <th className="p-3.5">Department</th>
                    <th className="p-3.5 text-center">Employees</th>
                    <th className="p-3.5 text-center">Assigned Licenses</th>
                    <th className="p-3.5 text-center">Unique Toolsets</th>
                    <th className="p-3.5">Attributed Cost</th>
                    <th className="p-3.5">Top Allocated Tools</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.map((item, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/70">
                      <td className="p-3.5 font-bold text-slate-900">{item.department}</td>
                      <td className="p-3.5 text-center font-semibold text-slate-700">{item.totalEmployees}</td>
                      <td className="p-3.5 text-center font-bold text-indigo-600">{item.totalLicensesAssigned}</td>
                      <td className="p-3.5 text-center font-bold text-purple-600">{item.uniqueSoftwareCount}</td>
                      <td className="p-3.5 font-bold text-emerald-700">${item.totalCostAttributed?.toLocaleString()}</td>
                      <td className="p-3.5 text-slate-600">
                        {item.topSoftwareAssigned ? item.topSoftwareAssigned.join(', ') : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )
          ) : (
            <div className="p-12 text-center text-slate-400 text-xs">
              No report records found for the selected filter parameters.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
