import React from 'react';
import { Filter, RotateCcw } from 'lucide-react';

export const FilterBar = ({
  filters = [],
  values = {},
  onChange,
  onReset,
  className = '',
}) => {
  return (
    <div className={`flex flex-wrap items-center gap-3 ${className}`}>
      <div className="flex items-center text-xs font-semibold text-slate-500 uppercase tracking-wider gap-1.5 mr-1">
        <Filter className="w-3.5 h-3.5" />
        Filters:
      </div>

      {filters.map((f) => (
        <div key={f.key} className="flex items-center">
          <select
            value={values[f.key] || 'All'}
            onChange={(e) => onChange(f.key, e.target.value)}
            className="text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 shadow-2xs hover:border-slate-300 transition"
          >
            <option value="All">{f.label}: All</option>
            {f.options.map((opt) => {
              const val = typeof opt === 'object' ? opt.value : opt;
              const lbl = typeof opt === 'object' ? opt.label : opt;
              return (
                <option key={val} value={val}>
                  {f.label}: {lbl}
                </option>
              );
            })}
          </select>
        </div>
      ))}

      {onReset && (
        <button
          onClick={onReset}
          className="inline-flex items-center gap-1 text-xs text-slate-500 hover:text-indigo-600 font-medium px-2 py-1.5 rounded-lg hover:bg-slate-100 transition ml-auto"
          title="Reset all filters"
        >
          <RotateCcw className="w-3 h-3" />
          Reset
        </button>
      )}
    </div>
  );
};
