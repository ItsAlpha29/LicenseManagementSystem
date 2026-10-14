import React from 'react';
import { Sparkles, AlertTriangle, TrendingUp, AlertCircle, ArrowRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export const AiInsightCard = ({ insight }) => {
  const navigate = useNavigate();

  if (!insight) return null;

  const isCrit = insight.severity === 'CRITICAL';
  const isHigh = insight.severity === 'HIGH';

  return (
    <div
      className={`p-4 rounded-2xl border transition-all duration-200 shadow-xs hover:shadow-md ${
        isCrit
          ? 'bg-rose-50/60 border-rose-200/80'
          : isHigh
          ? 'bg-amber-50/60 border-amber-200/80'
          : 'bg-indigo-50/60 border-indigo-200/80'
      }`}
    >
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-2">
          <div
            className={`p-1.5 rounded-lg ${
              isCrit ? 'bg-rose-100 text-rose-600' : isHigh ? 'bg-amber-100 text-amber-600' : 'bg-indigo-100 text-indigo-600'
            }`}
          >
            {isCrit ? (
              <AlertCircle className="w-4 h-4" />
            ) : isHigh ? (
              <AlertTriangle className="w-4 h-4" />
            ) : (
              <Sparkles className="w-4 h-4" />
            )}
          </div>
          <h4 className="text-xs font-bold text-slate-900">{insight.title}</h4>
        </div>
        <span
          className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider ${
            isCrit ? 'bg-rose-200/70 text-rose-800' : isHigh ? 'bg-amber-200/70 text-amber-800' : 'bg-indigo-200/70 text-indigo-800'
          }`}
        >
          {insight.type}
        </span>
      </div>

      <p className="text-xs text-slate-600 leading-relaxed mb-2.5">{insight.description}</p>

      {insight.recommendation && (
        <div className="p-2.5 bg-white/80 rounded-xl border border-slate-200/60 text-xs text-slate-700 font-medium mb-3">
          <span className="font-semibold text-slate-900">Recommendation: </span>
          {insight.recommendation}
        </div>
      )}

      {insight.actionText && insight.actionUrl && (
        <button
          onClick={() => navigate(insight.actionUrl)}
          className="inline-flex items-center gap-1 text-xs font-semibold text-indigo-700 hover:text-indigo-900 transition"
        >
          <span>{insight.actionText}</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </button>
      )}
    </div>
  );
};
