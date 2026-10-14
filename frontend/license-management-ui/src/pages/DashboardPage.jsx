import React, { useState, useEffect } from 'react';
import {
  Users,
  KeyRound,
  UserCheck,
  CheckCircle2,
  AlertTriangle,
  AlertCircle,
  RefreshCw,
  DollarSign,
  TrendingUp,
  Sparkles,
  ArrowRight,
  Shield,
  Layers,
  Building2,
} from 'lucide-react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  PieChart,
  Pie,
  Cell,
  AreaChart,
  Area,
  CartesianGrid,
} from 'recharts';
import { dashboardService } from '../services/dashboardService';
import { KpiCard } from '../components/KpiCard';
import { ChartCard } from '../components/ChartCard';
import { StatusBadge } from '../components/StatusBadge';
import { AiInsightCard } from '../components/AiInsightCard';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const DashboardPage = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const { role } = useAuth();

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        const res = await dashboardService.getFullDashboard();
        if (res.success && res.data) {
          setData(res.data);
        }
      } catch {
        // error handled via toast or fallback
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <LoadingSkeleton count={4} />
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <LoadingSkeleton count={1} type="table" />
          <LoadingSkeleton count={1} type="table" />
        </div>
      </div>
    );
  }

  const summary = data?.summary || {};
  const utilization = data?.utilization || [];
  const statusDistribution = data?.statusDistribution || [];
  const departmentUsage = data?.departmentUsage || [];
  const monthlyCostTrend = data?.monthlyCostTrend || [];
  const expiryTimeline = data?.expiryTimeline || {};
  const topExpiring = data?.topExpiringLicenses || [];
  const underutilized = data?.underutilizedLicenses || [];
  const upcomingRenewals = data?.upcomingRenewals || [];
  const recentActivities = data?.recentActivities || [];
  const aiInsights = data?.aiInsights || [];

  return (
    <div className="space-y-8">
      {/* 1. Top Executive KPI Cards */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-500">
            Key Performance Metrics
          </h2>
          <span className="text-xs text-slate-400 font-medium">Real-time synchronized</span>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-5">
          <KpiCard
            title="Total Employees"
            value={summary.totalEmployees}
            subtitle={`${summary.activeEmployees} active workforce`}
            icon={Users}
            color="indigo"
            onClick={() => (role === 'ADMIN' || role === 'IT_MANAGER') && navigate('/employees')}
          />
          <KpiCard
            title="Total Software Seats"
            value={summary.totalLicenses}
            subtitle={`${summary.assignedLicenses} assigned (${summary.overallUtilizationPercentage}% utilized)`}
            icon={KeyRound}
            color="purple"
            onClick={() => navigate('/licenses')}
          />
          <KpiCard
            title="Available Seats"
            value={summary.availableLicenses}
            subtitle="Ready for provisioning"
            icon={CheckCircle2}
            color="emerald"
            onClick={() => (role === 'ADMIN' || role === 'IT_MANAGER') && navigate('/assignments')}
          />
          <KpiCard
            title="Expiring / Action Needed"
            value={summary.expiringSoonLicenses}
            subtitle={`${summary.expiredLicenses} already expired`}
            icon={AlertTriangle}
            color={summary.expiringSoonLicenses > 0 || summary.expiredLicenses > 0 ? 'amber' : 'emerald'}
            onClick={() => (role === 'ADMIN' || role === 'IT_MANAGER') && navigate('/renewals')}
          />
        </div>

        {/* Secondary Row of Cost & Capacity Metrics */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 sm:gap-5 mt-4">
          <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex items-center justify-between">
            <div>
              <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Total Annual Spend</span>
              <div className="text-xl font-bold text-slate-900 mt-1">
                ${summary.totalLicenseCost?.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </div>
            </div>
            <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center font-bold text-sm">
              <DollarSign className="w-5 h-5" />
            </div>
          </div>

          <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex items-center justify-between">
            <div>
              <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">Projected Renewal Cost</span>
              <div className="text-xl font-bold text-slate-900 mt-1">
                ${summary.totalRenewalCost?.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </div>
            </div>
            <div className="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center font-bold text-sm">
              <RefreshCw className="w-5 h-5" />
            </div>
          </div>

          <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex items-center justify-between">
            <div>
              <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">System License Capacity</span>
              <div className="text-xl font-bold text-indigo-600 mt-1">
                {summary.overallUtilizationPercentage}% Allocated
              </div>
            </div>
            <div className="w-10 h-10 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center font-bold text-sm">
              <Layers className="w-5 h-5" />
            </div>
          </div>
        </div>
      </section>

      {/* 2. AI Insights Section */}
      {aiInsights.length > 0 && (
        <section>
          <div className="flex items-center gap-2 mb-3">
            <div className="p-1 rounded-lg bg-indigo-100 text-indigo-700">
              <Sparkles className="w-4 h-4" />
            </div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700">
              AI Insights & Actionable Recommendations
            </h3>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {aiInsights.map((insight) => (
              <AiInsightCard key={insight.id} insight={insight} />
            ))}
          </div>
        </section>
      )}

      {/* 3. Analytics Charts Grid */}
      <section className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Software Utilization Bar Chart */}
        <div className="lg:col-span-8">
          <ChartCard
            title="Software Seat Utilization"
            subtitle="Comparing allocated vs available seats per software title"
          >
            <div className="h-72 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={utilization} margin={{ top: 10, right: 10, left: -20, bottom: 20 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                  <XAxis
                    dataKey="softwareName"
                    tick={{ fontSize: 11, fill: '#64748b' }}
                    interval={0}
                    angle={-20}
                    textAnchor="end"
                  />
                  <YAxis tick={{ fontSize: 11, fill: '#64748b' }} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1e293b',
                      borderRadius: '12px',
                      color: '#fff',
                      border: 'none',
                      fontSize: '12px',
                    }}
                  />
                  <Legend wrapperStyle={{ fontSize: '12px', paddingTop: '10px' }} />
                  <Bar dataKey="assigned" name="Assigned Seats" fill="#6366f1" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="available" name="Available Seats" fill="#10b981" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </ChartCard>
        </div>

        {/* License Status Distribution Donut */}
        <div className="lg:col-span-4">
          <ChartCard
            title="License Health & Status"
            subtitle="Current status breakdown across inventory"
          >
            <div className="h-72 w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={statusDistribution}
                    cx="50%"
                    cy="50%"
                    innerRadius={55}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {statusDistribution.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1e293b',
                      borderRadius: '12px',
                      color: '#fff',
                      border: 'none',
                      fontSize: '12px',
                    }}
                  />
                  <Legend wrapperStyle={{ fontSize: '11px' }} />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </ChartCard>
        </div>

        {/* Monthly Spend & Renewal Projection */}
        <div className="lg:col-span-6">
          <ChartCard
            title="Monthly License Expenditure Trend"
            subtitle="Historical and projected annual licensing cost ($)"
          >
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={monthlyCostTrend} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                  <defs>
                    <linearGradient id="costGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#6366f1" stopOpacity={0.4} />
                      <stop offset="95%" stopColor="#6366f1" stopOpacity={0.0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                  <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis tick={{ fontSize: 11, fill: '#64748b' }} />
                  <Tooltip
                    formatter={(val) => [`$${Number(val).toLocaleString()}`, 'Cost']}
                    contentStyle={{
                      backgroundColor: '#1e293b',
                      borderRadius: '12px',
                      color: '#fff',
                      border: 'none',
                      fontSize: '12px',
                    }}
                  />
                  <Area type="monotone" dataKey="cost" name="Licensing Cost" stroke="#6366f1" strokeWidth={2} fillOpacity={1} fill="url(#costGrad)" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </ChartCard>
        </div>

        {/* Department Usage Distribution */}
        <div className="lg:col-span-6">
          <ChartCard
            title="Department License Density"
            subtitle="Active licenses assigned by organizational department"
          >
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={departmentUsage} layout="vertical" margin={{ top: 5, right: 10, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#f1f5f9" />
                  <XAxis type="number" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis type="category" dataKey="department" tick={{ fontSize: 11, fill: '#64748b' }} width={90} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1e293b',
                      borderRadius: '12px',
                      color: '#fff',
                      border: 'none',
                      fontSize: '12px',
                    }}
                  />
                  <Bar dataKey="licenseCount" name="Assigned Licenses" fill="#8b5cf6" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </ChartCard>
        </div>
      </section>

      {/* 4. Expiry Timeline Breakdown & Tables */}
      <section className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Expiry Pipeline Categories */}
        <div className="lg:col-span-12">
          <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-4">
              Expiry & Renewal Timeline Horizon
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
              <div className="p-3.5 rounded-xl bg-rose-50 border border-rose-100 text-center">
                <span className="text-xs font-semibold text-rose-800">Expired</span>
                <div className="text-2xl font-black text-rose-600 mt-1">{expiryTimeline.expired || 0}</div>
                <span className="text-[10px] text-rose-500 font-medium">Needs Attention</span>
              </div>
              <div className="p-3.5 rounded-xl bg-amber-50 border border-amber-100 text-center">
                <span className="text-xs font-semibold text-amber-800">Critical (&lt; 7 Days)</span>
                <div className="text-2xl font-black text-amber-600 mt-1">{expiryTimeline.criticalWithin7Days || 0}</div>
                <span className="text-[10px] text-amber-600 font-medium">Imminent Expiry</span>
              </div>
              <div className="p-3.5 rounded-xl bg-yellow-50 border border-yellow-100 text-center">
                <span className="text-xs font-semibold text-yellow-800">Soon (&lt; 30 Days)</span>
                <div className="text-2xl font-black text-yellow-600 mt-1">{expiryTimeline.expiringWithin30Days || 0}</div>
                <span className="text-[10px] text-yellow-600 font-medium">Renewal Window</span>
              </div>
              <div className="p-3.5 rounded-xl bg-sky-50 border border-sky-100 text-center">
                <span className="text-xs font-semibold text-sky-800">Upcoming (&lt; 90 Days)</span>
                <div className="text-2xl font-black text-sky-600 mt-1">{expiryTimeline.upcomingWithin90Days || 0}</div>
                <span className="text-[10px] text-sky-600 font-medium">Planned Pipeline</span>
              </div>
              <div className="p-3.5 rounded-xl bg-emerald-50 border border-emerald-100 text-center">
                <span className="text-xs font-semibold text-emerald-800">Active (&gt; 90 Days)</span>
                <div className="text-2xl font-black text-emerald-600 mt-1">{expiryTimeline.activeBeyond90Days || 0}</div>
                <span className="text-[10px] text-emerald-600 font-medium">Healthy Coverage</span>
              </div>
            </div>
          </div>
        </div>

        {/* Top Expiring Licenses Table */}
        <div className="lg:col-span-6">
          <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs h-full flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-bold text-slate-900">Upcoming Expiring Licenses</h3>
                {(role === 'ADMIN' || role === 'IT_MANAGER') && (
                  <button
                    onClick={() => navigate('/renewals')}
                    className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 inline-flex items-center gap-1"
                  >
                    Manage Renewals <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left">
                  <thead>
                    <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase">
                      <th className="py-2.5">Software</th>
                      <th className="py-2.5">Vendor</th>
                      <th className="py-2.5 text-center">Assigned</th>
                      <th className="py-2.5">Expiry Date</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                    {topExpiring.length > 0 ? (
                      topExpiring.map((l) => (
                        <tr key={l.id} className="hover:bg-slate-50/80 transition">
                          <td className="py-2.5 font-bold text-slate-800">{l.softwareName}</td>
                          <td className="py-2.5 text-slate-500">{l.vendor}</td>
                          <td className="py-2.5 text-center font-semibold text-indigo-600">{l.assignedCount} / {l.totalLicenses}</td>
                          <td className="py-2.5 font-mono text-slate-600">
                            {new Date(l.expiryDate).toLocaleDateString()}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={4} className="py-6 text-center text-slate-400">
                          No expiring licenses found.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        {/* Recent Audit Activities Feed */}
        <div className="lg:col-span-6">
          <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs h-full flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-bold text-slate-900">Recent Audit Activities</h3>
                {role === 'ADMIN' && (
                  <button
                    onClick={() => navigate('/audit')}
                    className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 inline-flex items-center gap-1"
                  >
                    View All Logs <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>
              <div className="space-y-2.5 max-h-60 overflow-y-auto pr-1">
                {recentActivities.length > 0 ? (
                  recentActivities.map((act) => (
                    <div key={act.id} className="p-2.5 rounded-xl bg-slate-50/70 border border-slate-100 flex items-start justify-between gap-3 text-xs">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-1.5 mb-0.5">
                          <span className="font-bold text-slate-800">{act.userName}</span>
                          <span className="px-1.5 py-0.2 text-[9px] font-mono font-bold bg-indigo-100 text-indigo-700 rounded">
                            {act.action}
                          </span>
                        </div>
                        <p className="text-slate-600 text-[11px] truncate">{act.remarks}</p>
                      </div>
                      <span className="text-[10px] text-slate-400 flex-shrink-0">
                        {new Date(act.dateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                  ))
                ) : (
                  <div className="py-6 text-center text-slate-400 text-xs">
                    No recent activity logs.
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};
