import React, { useState, useEffect } from 'react';
import {
  History,
  Shield,
  Search,
  Filter,
  User,
  Calendar,
  Eye,
  Activity,
} from 'lucide-react';
import { auditService } from '../services/auditService';
import { DataTable } from '../components/DataTable';
import { SearchBar } from '../components/SearchBar';
import { FilterBar } from '../components/FilterBar';
import { Modal } from '../components/Modal';
import { useToast } from '../context/ToastContext';

export const AuditPage = () => {
  const [logs, setLogs] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Filters & Pagination
  const [filters, setFilters] = useState({ module: 'All', action: 'All' });
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(15);

  // Metadata Modal
  const [selectedLog, setSelectedLog] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const toast = useToast();

  const fetchLogs = async () => {
    try {
      setLoading(true);
      const res = await auditService.getAuditLogs({
        module: filters.module,
        action: filters.action,
        pageNumber,
        pageSize,
      });

      if (res.success && res.data) {
        setLogs(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      toast.error(err.message || 'Failed to load audit history.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, [filters, pageNumber, pageSize]);

  const handleOpenMetadata = (log) => {
    setSelectedLog(log);
    setIsModalOpen(true);
  };

  const getActionBadgeColor = (act) => {
    const a = act?.toUpperCase();
    if (a === 'CREATE' || a === 'ASSIGN' || a === 'RENEW') return 'bg-emerald-100 text-emerald-800 border-emerald-200';
    if (a === 'UPDATE' || a === 'STATUS_CHANGE') return 'bg-indigo-100 text-indigo-800 border-indigo-200';
    if (a === 'DELETE' || a === 'REVOKE') return 'bg-rose-100 text-rose-800 border-rose-200';
    return 'bg-slate-100 text-slate-700 border-slate-200';
  };

  const columns = [
    {
      header: 'Audit ID',
      key: 'id',
      render: (row) => <span className="font-mono font-bold text-slate-500 text-xs">#{row.id}</span>,
    },
    {
      header: 'User / Actor',
      key: 'userName',
      render: (row) => (
        <div>
          <div className="font-bold text-slate-900">{row.userName}</div>
          <div className="text-[11px] text-slate-400 font-mono">{row.userEmail} ({row.userRole})</div>
        </div>
      ),
    },
    {
      header: 'Action',
      key: 'action',
      render: (row) => (
        <span className={`px-2 py-0.5 rounded text-[11px] font-mono font-bold border ${getActionBadgeColor(row.action)}`}>
          {row.action}
        </span>
      ),
    },
    {
      header: 'Module & Entity',
      key: 'module',
      render: (row) => (
        <div>
          <span className="font-semibold text-slate-800">{row.module}</span>
          <span className="text-[11px] text-slate-400 block font-mono">{row.entityType} #{row.entityId || '—'}</span>
        </div>
      ),
    },
    {
      header: 'Date & Time',
      key: 'dateTime',
      render: (row) => (
        <span className="text-xs text-slate-600 font-mono">
          {new Date(row.dateTime).toLocaleString()}
        </span>
      ),
    },
    {
      header: 'Audit Remarks',
      key: 'remarks',
      render: (row) => (
        <p className="text-xs text-slate-700 max-w-sm truncate" title={row.remarks}>
          {row.remarks}
        </p>
      ),
    },
    {
      header: 'IP Address',
      key: 'ipAddress',
      render: (row) => (
        <span className="font-mono text-[11px] text-slate-500">
          {row.ipAddress || '127.0.0.1'}
        </span>
      ),
    },
    {
      header: 'Details',
      key: 'details',
      align: 'right',
      render: (row) => (
        <button
          onClick={() => handleOpenMetadata(row)}
          className="p-1 text-slate-400 hover:text-indigo-600 hover:bg-slate-100 rounded transition"
          title="View raw log details"
        >
          <Eye className="w-4 h-4" />
        </button>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-slate-900 tracking-tight">System Audit History Trail</h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Immutable compliance record of all administrative logins, mutations, assignments, and renewals
        </p>
      </div>

      {/* Filter Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs">
        <FilterBar
          filters={[
            {
              key: 'module',
              label: 'Module',
              options: ['AUTH', 'EMPLOYEES', 'LICENSES', 'ASSIGNMENTS', 'RENEWALS', 'AI'],
            },
            {
              key: 'action',
              label: 'Action',
              options: ['LOGIN', 'CREATE', 'UPDATE', 'DELETE', 'ASSIGN', 'REVOKE', 'RENEW', 'STATUS_CHANGE', 'PASSWORD_CHANGE'],
            },
          ]}
          values={filters}
          onChange={(key, val) => {
            setFilters((prev) => ({ ...prev, [key]: val }));
            setPageNumber(1);
          }}
          onReset={() => {
            setFilters({ module: 'All', action: 'All' });
            setPageNumber(1);
          }}
        />
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={logs}
        loading={loading}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={setPageSize}
        emptyTitle="No audit logs found"
        emptyDescription="No events match the specified filter criteria."
      />

      {/* Audit Detail Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Audit Entry Inspector"
        subtitle={`Log Record #${selectedLog?.id} — ${selectedLog?.action}`}
        maxWidth="max-w-xl"
      >
        <div className="space-y-4 text-xs">
          <div className="grid grid-cols-2 gap-3 p-3.5 bg-slate-50 rounded-xl border border-slate-100">
            <div>
              <span className="text-slate-400 uppercase font-bold text-[10px] block">Actor / User</span>
              <span className="font-bold text-slate-900">{selectedLog?.userName}</span>
              <span className="text-slate-500 block">{selectedLog?.userEmail}</span>
            </div>
            <div>
              <span className="text-slate-400 uppercase font-bold text-[10px] block">Timestamp</span>
              <span className="font-mono text-slate-900 font-semibold">
                {selectedLog?.dateTime ? new Date(selectedLog.dateTime).toLocaleString() : '—'}
              </span>
              <span className="text-slate-500 block">IP: {selectedLog?.ipAddress || '127.0.0.1'}</span>
            </div>
          </div>

          <div className="p-3 bg-white rounded-xl border border-slate-200">
            <span className="text-slate-400 uppercase font-bold text-[10px] block mb-1">Remarks / Explanation</span>
            <p className="text-slate-800 leading-relaxed font-medium">{selectedLog?.remarks}</p>
          </div>

          {selectedLog?.metadata && (
            <div>
              <span className="text-slate-400 uppercase font-bold text-[10px] block mb-1">JSON Metadata Payload</span>
              <pre className="p-3 bg-slate-950 text-emerald-400 rounded-xl font-mono text-[11px] overflow-x-auto max-h-48">
                {selectedLog.metadata}
              </pre>
            </div>
          )}

          <div className="flex justify-end pt-3 border-t border-slate-100">
            <button
              onClick={() => setIsModalOpen(false)}
              className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-xs font-semibold transition"
            >
              Close
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
