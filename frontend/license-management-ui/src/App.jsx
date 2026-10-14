import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ToastProvider } from './context/ToastContext';

// Layout & Guards
import { AppLayout } from './components/AppLayout';
import { ProtectedRoute, RoleGuard } from './components/ProtectedRoute';

// Pages
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { EmployeesPage } from './pages/EmployeesPage';
import { EmployeeDetailPage } from './pages/EmployeeDetailPage';
import { LicensesPage } from './pages/LicensesPage';
import { LicenseDetailPage } from './pages/LicenseDetailPage';
import { AssignmentsPage } from './pages/AssignmentsPage';
import { MyLicensesPage } from './pages/MyLicensesPage';
import { RenewalsPage } from './pages/RenewalsPage';
import { ReportsPage } from './pages/ReportsPage';
import { AuditPage } from './pages/AuditPage';
import { ProfilePage } from './pages/ProfilePage';
import { AccessDeniedPage } from './pages/AccessDeniedPage';
import { NotFoundPage } from './pages/NotFoundPage';

export const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <Routes>
            {/* Public Login Route */}
            <Route path="/login" element={<LoginPage />} />

            {/* Protected Routes */}
            <Route element={<ProtectedRoute />}>
              <Route element={<AppLayout />}>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />

                {/* Licenses Master & Detail */}
                <Route path="/licenses" element={<LicensesPage />} />
                <Route path="/licenses/:id" element={<LicenseDetailPage />} />

                {/* My Licenses Self-Service */}
                <Route path="/my-licenses" element={<MyLicensesPage />} />

                {/* Profile */}
                <Route path="/profile" element={<ProfilePage />} />

                {/* IT Manager & Admin Authorized Routes */}
                <Route element={<RoleGuard allowedRoles={['ADMIN', 'IT_MANAGER']} />}>
                  <Route path="/employees" element={<EmployeesPage />} />
                  <Route path="/employees/:id" element={<EmployeeDetailPage />} />
                  <Route path="/assignments" element={<AssignmentsPage />} />
                  <Route path="/renewals" element={<RenewalsPage />} />
                  <Route path="/reports" element={<ReportsPage />} />
                </Route>

                {/* Admin Only Routes */}
                <Route element={<RoleGuard allowedRoles={['ADMIN']} />}>
                  <Route path="/audit" element={<AuditPage />} />
                </Route>

                {/* Access Denied & Fallback */}
                <Route path="/access-denied" element={<AccessDeniedPage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Route>
          </Routes>
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
