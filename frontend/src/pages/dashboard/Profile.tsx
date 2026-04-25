import React, { useState, useEffect } from 'react';
import { useAuth } from '../../hooks/useAuth';

const DashboardProfile = () => {
  const { user } = useAuth();
  const [formData, setFormData] = useState({ firstName: '', lastName: '' });
  const [loading, setLoading] = useState(false);
  const [editing, setEditing] = useState(false);

  useEffect(() => {
    if (user) {
      setFormData({
        firstName: user.name?.split(' ')[0] || '',
        lastName: user.name?.split(' ')[1] || ''
      });
    }
  }, [user]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const response = await fetch('/api/dashboard/profile', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(formData)
      });
      if (response.ok) {
        setEditing(false);
        // You could use a custom toast notification here for better UX
      }
    } catch (err) {
      console.error('Update failed', err);
    } finally {
      setLoading(false);
    }
  };

  if (!user) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-black"></div>
    </div>
  );

  return (
    <div className="max-w-2xl">
      <div className="mb-12">
        <h2 className="text-4xl font-bold tracking-tight text-gray-900 mb-4">Profile Settings</h2>
        <p className="text-lg text-gray-500">Update your account information and manage how you are seen.</p>
      </div>

      <div className="bg-white border border-gray-100 rounded-3xl shadow-sm overflow-hidden">
        <div className="p-8 border-b border-gray-100 bg-gray-50/50">
          <div className="flex items-center gap-6">
            <div className="w-20 h-20 bg-black rounded-full flex items-center justify-center text-white text-3xl font-bold border-4 border-white shadow-lg">
              {user.email?.charAt(0).toUpperCase()}
            </div>
            <div>
              <h3 className="text-xl font-bold text-gray-900">{user.email}</h3>
              <p className="text-sm text-gray-500 font-medium">Personal Account</p>
            </div>
          </div>
        </div>

        <div className="p-8">
          <form onSubmit={handleSubmit} className="space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div className="space-y-2">
                <label className="block text-sm font-bold text-gray-900 uppercase tracking-wider">First name</label>
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                  className="w-full px-5 py-4 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-black focus:bg-white focus:border-transparent transition-all outline-none disabled:opacity-50"
                  placeholder="John"
                  disabled={!editing}
                />
              </div>
              <div className="space-y-2">
                <label className="block text-sm font-bold text-gray-900 uppercase tracking-wider">Last name</label>
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                  className="w-full px-5 py-4 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-black focus:bg-white focus:border-transparent transition-all outline-none disabled:opacity-50"
                  placeholder="Doe"
                  disabled={!editing}
                />
              </div>
            </div>

            <div className="space-y-2">
              <label className="block text-sm font-bold text-gray-900 uppercase tracking-wider">Email address</label>
              <input
                type="email"
                value={user.email}
                readOnly
                className="w-full px-5 py-4 bg-gray-100 border border-gray-200 rounded-xl text-gray-500 cursor-not-allowed outline-none"
              />
              <p className="text-xs text-gray-400">Email cannot be changed directly. Contact support for assistance.</p>
            </div>

            <div className="flex items-center gap-4 pt-4">
              {!editing ? (
                <button
                  type="button"
                  onClick={() => setEditing(true)}
                  className="px-8 py-4 bg-black text-white font-bold rounded-xl hover:bg-gray-800 transition-all active:scale-95 shadow-md shadow-black/5"
                >
                  Edit Profile
                </button>
              ) : (
                <>
                  <button
                    type="submit"
                    disabled={loading}
                    className="px-8 py-4 bg-black text-white font-bold rounded-xl hover:bg-gray-800 transition-all active:scale-95 shadow-md shadow-black/5 disabled:opacity-50"
                  >
                    {loading ? 'Saving...' : 'Save Changes'}
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditing(false)}
                    className="px-8 py-4 bg-white border border-gray-200 text-gray-900 font-bold rounded-xl hover:bg-gray-50 transition-all"
                  >
                    Cancel
                  </button>
                </>
              )}
            </div>
          </form>
        </div>
      </div>
      
      <div className="mt-12 p-8 border border-red-100 bg-red-50/30 rounded-3xl">
        <h3 className="text-xl font-bold text-red-900 mb-2">Danger Zone</h3>
        <p className="text-red-600/70 mb-6">Once you delete your account, there is no going back. Please be certain.</p>
        <button className="px-6 py-3 border-2 border-red-200 text-red-600 font-bold rounded-xl hover:bg-red-600 hover:text-white transition-all">
          Delete Account
        </button>
      </div>
    </div>
  );
};

export default DashboardProfile;

