import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  KeyRound,
  UserCheck,
  Award,
  RefreshCw,
  BarChart3,
  History,
  User,
  ShieldAlert,
  ChevronLeft,
  ChevronRight,
  ShieldCheck,
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const Sidebar = ({ isCollapsed, setIsCollapsed, isMobileOpen, setIsMobileOpen }) => {
  const { user, role, logout } = useAuth();

  const navigationItems = [
    {
      name: 'Dashboard',
      path: '/dashboard',
      icon: LayoutDashboard,
      roles: ['ADMIN', 'IT_MANAGER', 'EMPLOYEE'],
    },
    {
      name: 'Employees',
      path: '/employees',
      icon: Users,
      roles: ['ADMIN', 'IT_MANAGER'],
    },
    {
      name: 'License Master',
      path: '/licenses',
      icon: KeyRound,
      roles: ['ADMIN', 'IT_MANAGER', 'EMPLOYEE'],
    },
    {
      name: 'Assignments',
      path: '/assignments',
      icon: UserCheck,
      roles: ['ADMIN', 'IT_MANAGER'],
    },
    {
      name: 'My Licenses',
      path: '/my-licenses',
      icon: Award,
      roles: ['ADMIN', 'IT_MANAGER', 'EMPLOYEE'],
    },
    {
      name: 'Renewals',
      path: '/renewals',
      icon: RefreshCw,
      roles: ['ADMIN', 'IT_MANAGER'],
    },
    {
      name: 'Reports',
      path: '/reports',
      icon: BarChart3,
      roles: ['ADMIN', 'IT_MANAGER'],
    },
    {
      name: 'Audit History',
      path: '/audit',
      icon: History,
      roles: ['ADMIN'],
    },
    {
      name: 'My Profile',
      path: '/profile',
      icon: User,
      roles: ['ADMIN', 'IT_MANAGER', 'EMPLOYEE'],
    },
  ];

  const filteredItems = navigationItems.filter((item) =>
    item.roles.includes(role.toUpperCase())
  );

  return (
    <>
      {/* Mobile Backdrop */}
      {isMobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/60 backdrop-blur-xs lg:hidden"
          onClick={() => setIsMobileOpen(false)}
        />
      )}

      {/* Sidebar Container */}
      <aside
        className={`fixed top-0 left-0 z-40 h-screen bg-[#0f172a] text-slate-200 transition-all duration-300 ease-in-out border-r border-slate-800 flex flex-col ${
          isCollapsed ? 'w-20' : 'w-64'
        } ${
          isMobileOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        }`}
      >
        {/* Logo & Brand Header */}
        <div className="h-16 flex items-center justify-between px-4 border-b border-slate-800/80">
          <div className="flex items-center gap-3 overflow-hidden">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 to-purple-600 flex items-center justify-center text-white shadow-md flex-shrink-0">
              <ShieldCheck className="w-6 h-6" />
            </div>
            {!isCollapsed && (
              <div className="min-w-0">
                <h1 className="text-sm font-bold tracking-tight text-white truncate">
                  LicenceHub
                </h1>
                <p className="text-[10px] text-slate-400 font-medium tracking-wide uppercase truncate">
                  Enterprise Platform
                </p>
              </div>
            )}
          </div>

          {/* Desktop Collapse Toggle */}
          <button
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="hidden lg:flex p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition"
            title={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {isCollapsed ? <ChevronRight className="w-4 h-4" /> : <ChevronLeft className="w-4 h-4" />}
          </button>
        </div>

        {/* Navigation Items */}
        <nav className="flex-1 px-3 py-4 space-y-1.5 overflow-y-auto">
          {filteredItems.map((item) => {
            const Icon = item.icon;

            return (
              <NavLink
                key={item.path}
                to={item.path}
                onClick={() => setIsMobileOpen(false)}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all duration-150 group ${
                    isActive
                      ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/30'
                      : 'text-slate-400 hover:text-slate-100 hover:bg-slate-800/60'
                  } ${isCollapsed ? 'justify-center px-2' : ''}`
                }
                title={isCollapsed ? item.name : undefined}
              >
                <Icon className={`w-4 h-4 flex-shrink-0 transition-transform group-hover:scale-110`} />
                {!isCollapsed && <span className="truncate">{item.name}</span>}
              </NavLink>
            );
          })}
        </nav>

        {/* User Card & Role Info in Footer */}
        <div className="p-3 border-t border-slate-800/80 bg-slate-900/50">
          <div className={`flex items-center gap-3 p-2 rounded-xl bg-slate-800/60 border border-slate-700/50 ${isCollapsed ? 'justify-center p-1.5' : ''}`}>
            <div className="w-8 h-8 rounded-lg bg-indigo-500/20 text-indigo-400 border border-indigo-500/30 flex items-center justify-center font-bold text-xs flex-shrink-0">
              {user?.name ? user.name.charAt(0).toUpperCase() : user?.email?.charAt(0).toUpperCase() || 'U'}
            </div>

            {!isCollapsed && (
              <div className="flex-1 min-w-0">
                <p className="text-xs font-semibold text-white truncate">
                  {user?.name || user?.email}
                </p>
                <span className="inline-block text-[10px] font-mono font-medium text-indigo-400 uppercase">
                  {role}
                </span>
              </div>
            )}
          </div>
        </div>
      </aside>
    </>
  );
};
