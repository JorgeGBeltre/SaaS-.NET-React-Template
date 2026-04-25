import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export const Nav = ({ isDashboard = false }: { isDashboard?: boolean }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <nav className="fixed top-4 left-4 right-4 z-50 glass rounded-2xl mx-auto max-w-7xl">
      <div className="px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex items-center gap-10">
            <Link to="/" className="flex items-center gap-2 group">
              <div className="w-8 h-8 bg-primary flex items-center justify-center rounded-lg group-hover:scale-110 premium-transition shadow-lg">
                <span className="text-white text-xs font-bold">S</span>
              </div>
              <span className="text-base font-bold tracking-tight text-primary">SaaS Elite</span>
            </Link>
            {!isDashboard && (
              <div className="hidden md:flex items-center gap-8">
                <Link to="/features" className="text-sm font-medium text-secondary hover:text-accent premium-transition">Features</Link>
                <Link to="/pricing" className="text-sm font-medium text-secondary hover:text-accent premium-transition">Pricing</Link>
              </div>
            )}
          </div>

          <div className="flex items-center gap-6">
            {user ? (
              <>
                {!isDashboard ? (
                  <>
                    <Link to="/dashboard" className="text-sm font-medium text-secondary hover:text-accent premium-transition">Dashboard</Link>
                    <button onClick={handleLogout} className="text-sm font-medium text-secondary hover:text-accent premium-transition">Log out</button>
                    <Link to="/dashboard/profile" className="flex items-center justify-center w-9 h-9 bg-primary text-white text-sm font-bold rounded-full shadow-lg hover:scale-105 premium-transition">
                      {user.email.charAt(0).toUpperCase()}
                    </Link>
                  </>
                ) : (
                  <div className="flex items-center gap-3">
                    <span className="hidden sm:block text-xs font-medium text-secondary">{user.email}</span>
                    <div className="flex items-center justify-center w-9 h-9 bg-primary text-white text-sm font-bold rounded-full shadow-lg">
                      {user.email.charAt(0).toUpperCase()}
                    </div>
                  </div>
                )}
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm font-medium text-secondary hover:text-accent premium-transition">Log in</Link>
                <Link to="/signup" className="text-sm font-bold px-6 py-2.5 bg-primary text-white hover:bg-secondary hover:shadow-xl premium-transition rounded-xl shadow-md">
                  Get Started
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};

