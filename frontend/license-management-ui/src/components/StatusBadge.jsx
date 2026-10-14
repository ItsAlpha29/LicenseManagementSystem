import React from 'react';

export const StatusBadge = ({ status, size = 'sm', className = '' }) => {
  if (!status) return null;

  const s = status.toString().trim();
  const lower = s.toLowerCase();

  let styles = 'bg-slate-100 text-slate-700 border-slate-200';
  let dotColor = 'bg-slate-400';
  let label = s;

  if (lower === 'active' || lower === 'completed' || lower === 'success') {
    styles = 'bg-emerald-50 text-emerald-700 border-emerald-200/80';
    dotColor = 'bg-emerald-500';
  } else if (lower === 'expiringsoon' || lower === 'expiring soon' || lower === 'warning' || lower === 'pending') {
    styles = 'bg-amber-50 text-amber-700 border-amber-200/80';
    dotColor = 'bg-amber-500';
    if (lower === 'expiringsoon') label = 'Expiring Soon';
  } else if (lower === 'expired' || lower === 'revoked' || lower === 'failed' || lower === 'critical' || lower === 'inactive') {
    styles = 'bg-rose-50 text-rose-700 border-rose-200/80';
    dotColor = 'bg-rose-500';
  } else if (lower === 'admin') {
    styles = 'bg-purple-50 text-purple-700 border-purple-200/80';
    dotColor = 'bg-purple-500';
  } else if (lower === 'it_manager' || lower === 'it manager') {
    styles = 'bg-indigo-50 text-indigo-700 border-indigo-200/80';
    dotColor = 'bg-indigo-500';
    label = 'IT Manager';
  } else if (lower === 'employee') {
    styles = 'bg-blue-50 text-blue-700 border-blue-200/80';
    dotColor = 'bg-blue-500';
  } else if (lower === 'subscription') {
    styles = 'bg-sky-50 text-sky-700 border-sky-200/80';
    dotColor = 'bg-sky-500';
  } else if (lower === 'perpetual') {
    styles = 'bg-teal-50 text-teal-700 border-teal-200/80';
    dotColor = 'bg-teal-500';
  }

  const sizeClasses = size === 'xs'
    ? 'px-2 py-0.5 text-[11px]'
    : size === 'md'
    ? 'px-3 py-1 text-sm font-semibold'
    : 'px-2.5 py-1 text-xs';

  return (
    <span
      className={`inline-flex items-center gap-1.5 font-medium rounded-full border shadow-xs ${styles} ${sizeClasses} ${className}`}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${dotColor} flex-shrink-0 animate-pulse-subtle`} />
      {label}
    </span>
  );
};
