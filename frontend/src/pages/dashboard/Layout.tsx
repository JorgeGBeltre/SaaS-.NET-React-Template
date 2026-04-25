import { Outlet, useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { useEffect } from 'react';

const DashboardLayout = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (!user) {
      navigate('/login');
    }
  }, [user, navigate]);

  const navItems = [
    { name: 'Overview', href: '/dashboard', icon: 'house' },
    { name: 'Profile', href: '/dashboard/profile', icon: 'user' },
    { name: 'Settings', href: '/dashboard/settings', icon: 'gear' },
    { name: 'Plans', href: '/dashboard/plans', icon: 'credit-card' },
  ];

  return (
    <div className="min-h-screen bg-white lg:flex">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:w-64 lg:fixed lg:inset-y-0 bg-gray-50 border-r border-gray-200 z-30">
        <nav className="flex flex-col w-full h-screen">
          <div className="h-20 flex items-center px-8 border-b border-gray-200 bg-white">
            <Link to="/" className="flex items-center gap-3">
              <div className="w-8 h-8 bg-black flex items-center justify-center rounded-sm">
                <span className="text-white text-xs font-bold">S</span>
              </div>
              <span className="text-xl font-bold tracking-tight">SaaS App</span>
            </Link>
          </div>
          
          <div className="flex-1 py-8 px-4 space-y-1 overflow-y-auto">
            {navItems.map((item) => {
              const isActive = location.pathname === item.href;
              return (
                <Link
                  key={item.name}
                  to={item.href}
                  className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-all duration-200 ${
                    isActive
                      ? 'bg-black text-white shadow-md'
                      : 'text-gray-600 hover:bg-gray-200 hover:text-black'
                  }`}
                >
                  <i className={`fa-solid fa-${item.icon} text-xs w-4`}></i>
                  {item.name}
                </Link>
              );
            })}
          </div>

          <div className="p-6 border-t border-gray-200 bg-white">
            <div className="flex items-center gap-4">
              <div className="w-10 h-10 bg-black flex-shrink-0 flex items-center justify-center text-white text-sm font-bold rounded-full border-2 border-gray-100">
                {user?.email?.charAt(0).toUpperCase()}
              </div>
              <div className="min-w-0">
                <p className="text-sm font-bold text-gray-900 truncate">{user?.email}</p>
                <Link to="/" className="text-xs text-gray-400 hover:text-black transition-colors">
                  Sign out
                </Link>
              </div>
            </div>
          </div>
        </nav>
      </aside>

      <div className="flex-1 lg:ml-64 bg-white">
        <header className="bg-white/80 backdrop-blur-md border-b border-gray-100 h-20 flex items-center px-8 sticky top-0 z-10">
          <div className="flex items-center justify-between w-full">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-gray-900 capitalize">
                {location.pathname.split('/').pop() || 'Dashboard'}
              </h1>
            </div>
            <div className="flex items-center gap-6">
              <div className="hidden md:flex flex-col items-end">
                <span className="text-xs font-semibold text-gray-400 uppercase tracking-widest leading-none">Free Plan</span>
                <span className="text-sm font-medium text-gray-900">{user?.email}</span>
              </div>
              <button className="w-10 h-10 rounded-full bg-gray-50 border border-gray-200 flex items-center justify-center hover:bg-gray-100 transition-colors">
                <i className="fa-solid fa-bell text-gray-400 text-sm"></i>
              </button>
            </div>
          </div>
        </header>

        <main className="p-8 lg:p-12 max-w-6xl mx-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;

