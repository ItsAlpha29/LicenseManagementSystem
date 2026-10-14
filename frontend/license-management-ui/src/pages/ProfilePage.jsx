import React, { useState } from 'react';
import {
  User,
  Mail,
  Phone,
  Building,
  Shield,
  KeyRound,
  Lock,
  Save,
  CheckCircle2,
  Clock,
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { authService } from '../services/authService';
import { FormField } from '../components/FormField';
import { StatusBadge } from '../components/StatusBadge';
import { useToast } from '../context/ToastContext';

export const ProfilePage = () => {
  const { user, role, updateUserProfile } = useAuth();
  const [phone, setPhone] = useState(user?.phone || '');
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [saving, setSaving] = useState(false);

  const toast = useToast();

  const handleUpdate = async (e) => {
    e.preventDefault();

    if (newPassword && newPassword !== confirmPassword) {
      toast.error('New passwords do not match.');
      return;
    }

    if (newPassword && newPassword.length < 6) {
      toast.error('New password must be at least 6 characters.');
      return;
    }

    setSaving(true);
    try {
      const res = await authService.updateProfile({
        phone,
        currentPassword: currentPassword || undefined,
        newPassword: newPassword || undefined,
      });

      if (res.success && res.data) {
        updateUserProfile(res.data);
        toast.success('Profile updated successfully.');
        setCurrentPassword('');
        setNewPassword('');
        setConfirmPassword('');
      }
    } catch (err) {
      toast.error(err.message || 'Failed to update profile.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-slate-900 tracking-tight">User Account Profile</h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Manage your personal details, workstation configuration, and authentication credentials
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Left: Summary Card */}
        <div className="md:col-span-4 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs text-center flex flex-col items-center justify-between">
          <div className="w-full flex flex-col items-center">
            <div className="w-20 h-20 rounded-3xl bg-gradient-to-tr from-indigo-600 via-purple-600 to-indigo-700 text-white font-black text-3xl flex items-center justify-center shadow-lg shadow-indigo-500/20 mb-4">
              {user?.name?.charAt(0) || user?.email?.charAt(0) || 'U'}
            </div>

            <h3 className="text-base font-bold text-slate-900">{user?.name || user?.email}</h3>
            <p className="text-xs text-slate-500 font-mono mt-0.5">{user?.email}</p>

            <div className="mt-3">
              <StatusBadge status={role} size="md" />
            </div>

            <div className="w-full mt-6 pt-6 border-t border-slate-100 space-y-3 text-left text-xs text-slate-600">
              {user?.employeeCode && (
                <div className="flex justify-between">
                  <span className="text-slate-400">Employee Code:</span>
                  <span className="font-mono font-bold text-slate-800">{user.employeeCode}</span>
                </div>
              )}
              {user?.department && (
                <div className="flex justify-between">
                  <span className="text-slate-400">Department:</span>
                  <span className="font-semibold text-slate-800">{user.department}</span>
                </div>
              )}
              {user?.designation && (
                <div className="flex justify-between">
                  <span className="text-slate-400">Designation:</span>
                  <span className="font-semibold text-slate-800">{user.designation}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-slate-400">Account Status:</span>
                <span className="font-bold text-emerald-600">Active</span>
              </div>
            </div>
          </div>

          <div className="w-full pt-6 mt-6 border-t border-slate-100 text-[11px] text-slate-400 flex items-center justify-center gap-1.5">
            <Clock className="w-3.5 h-3.5" />
            <span>Session Active</span>
          </div>
        </div>

        {/* Right: Update Profile & Change Password Form */}
        <div className="md:col-span-8 bg-white rounded-3xl p-6 sm:p-8 border border-slate-200/80 shadow-xs">
          <form onSubmit={handleUpdate} className="space-y-6">
            <div>
              <h3 className="text-sm font-bold text-slate-900 mb-4 pb-2 border-b border-slate-100 flex items-center gap-2">
                <User className="w-4 h-4 text-indigo-600" />
                Contact Information
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField
                  label="Work Email Address"
                  value={user?.email || ''}
                  disabled
                  helperText="Managed by corporate directory administrator."
                />
                <FormField
                  label="Contact Phone"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="+1 (555) 000-0000"
                />
              </div>
            </div>

            <div>
              <h3 className="text-sm font-bold text-slate-900 mb-4 pb-2 border-b border-slate-100 flex items-center gap-2">
                <Lock className="w-4 h-4 text-indigo-600" />
                Update Security Password
              </h3>
              <div className="space-y-4">
                <FormField
                  label="Current Password"
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  placeholder="Enter current password to verify..."
                />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField
                    label="New Password"
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="Minimum 6 characters"
                  />
                  <FormField
                    label="Confirm New Password"
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Re-enter new password"
                  />
                </div>
              </div>
            </div>

            <div className="flex justify-end pt-4 border-t border-slate-100">
              <button
                type="submit"
                disabled={saving}
                className="px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center gap-2 disabled:opacity-50"
              >
                <Save className="w-4 h-4" />
                {saving ? 'Saving Changes...' : 'Save Profile Changes'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
