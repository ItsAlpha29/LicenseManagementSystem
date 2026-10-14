import React from 'react';
import { ArrowUpRight, ArrowDownRight } from 'lucide-react';

export const KpiCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  trendLabel,
  color = 'indigo',
  loading = false,
  onClick,
}) => {
  const colorMap = {
    indigo: {
      bg: 'bg-indigo-50/80',
      border: 'border-indigo-100',
      text: 'text-indigo-600',
      accent: 'group-hover:border-indigo-300',
    },
    emerald: {
      bg: 'bg-emerald-50/80',
      border: 'border-emerald-100',
      text: 'text-emerald-600',
      accent: 'group-hover:border-emerald-300',
    },
    amber: {
      bg: 'bg-amber-50/80',
      border: 'border-amber-100',
      text: 'text-amber-600',
      accent: 'group-hover:border-amber-300',
    },
    rose: {
      bg: 'bg-rose-50/80',
      border: 'border-rose-100',
      text: 'text-rose-600',
      accent: 'group-hover:border-rose-300',
    },
    purple: {
      bg: 'bg-purple-50/80',
      border: 'border-purple-100',
      text: 'text-purple-600',
      accent: 'group-hover:border-purple-300',
    },
    sky: {
      bg: 'bg-sky-50/80',
      border: 'border-sky-100',
      text: 'text-sky-600',
      accent: 'group-hover:border-sky-300',
    },
  };

  const scheme = colorMap[color] || colorMap.indigo;

  if (loading) {
    return (
      <div className="bg-white rounded-2xl p-5 border border-slate-100 shadow-xs animate-pulse">
        <div className="flex justify-between items-start mb-3">
          <div className="h-4 bg-slate-200 rounded w-24" />
          <div className="w-10 h-10 bg-slate-100 rounded-xl" />
        </div>
        <div className="h-8 bg-slate-200 rounded w-20 mb-2" />
        <div className="h-3 bg-slate-100 rounded w-32" />
      </div>
    );
  }

  return (
    <div
      onClick={onClick}
      className={`group relative bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs hover:shadow-md transition-all duration-200 ${
        scheme.accent
      } ${onClick ? 'cursor-pointer' : ''}`}
    >
      <div className="flex items-start justify-between">
        <div>
          <span className="text-xs font-semibold uppercase tracking-wider text-slate-600">
            {title}
          </span>
          <div className="mt-2 text-2xl font-bold tracking-tight text-slate-900">
            {value !== undefined && value !== null ? value : '—'}
          </div>
        </div>

        {Icon && (
          <div
            className={`flex items-center justify-center w-11 h-11 rounded-xl ${scheme.bg} ${scheme.border} ${scheme.text} shadow-xs transition-transform group-hover:scale-105`}
          >
            <Icon className="w-5 h-5" />
          </div>
        )}
      </div>

      {(subtitle || trend) && (
        <div className="mt-3 flex items-center gap-2 text-xs">
          {trend && (
            <span
              className={`inline-flex items-center font-semibold ${
                trend > 0
                  ? 'text-emerald-600'
                  : trend < 0
                  ? 'text-rose-600'
                  : 'text-slate-500'
              }`}
            >
              {trend > 0 ? (
                <ArrowUpRight className="w-3.5 h-3.5 mr-0.5" />
              ) : (
                <ArrowDownRight className="w-3.5 h-3.5 mr-0.5" />
              )}
              {Math.abs(trend)}%
            </span>
          )}
          <span className="text-slate-600 font-medium truncate">
            {trendLabel || subtitle}
          </span>
        </div>
      )}
    </div>
  );
};
