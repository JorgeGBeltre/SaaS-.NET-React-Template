import { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { useNavigate, Link } from 'react-router-dom';
import { ShieldCheck } from 'lucide-react';
import { FloatingLabel } from '../../components/ui/FloatingLabel';

const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [requires2fa, setRequires2fa] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handlePasswordLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });

      const data = await response.json();
      if (response.ok) {
        if (data.requires2fa) {
          setRequires2fa(true);
          return;
        }

        login(data.token, data.refreshToken);
        navigate('/dashboard');
      } else {
        setError(typeof data === 'string' ? data : data.message || 'Login failed');
      }
    } catch {
      setError('Network error');
    } finally {
      setLoading(false);
    }
  };

  const handle2faSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const response = await fetch('/api/auth/login/verify-2fa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code: twoFactorCode }),
      });

      const data = await response.json();
      if (response.ok) {
        login(data.token, data.refreshToken);
        navigate('/dashboard');
      } else {
        setError(typeof data === 'string' ? data : data.message || 'Invalid 2FA code');
      }
    } catch {
      setError('Network error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="mx-auto h-12 w-12 bg-black flex items-center justify-center text-white rounded">
            {requires2fa ? <ShieldCheck size={24} /> : <span className="text-sm font-bold">S</span>}
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {requires2fa ? 'Two-Factor Authentication' : 'Sign in to your account'}
          </h2>
          {requires2fa && (
            <p className="mt-2 text-center text-sm text-gray-600">
              Enter the 6-digit code from your authenticator app.
            </p>
          )}
        </div>

        {requires2fa ? (
          <form className="mt-8 space-y-6" onSubmit={handle2faSubmit}>
            {error && (
              <div className="border border-red-300 bg-red-50 p-4 rounded text-sm text-red-800">
                {error}
              </div>
            )}
            <div>
              <label htmlFor="twoFactorCode" className="block text-sm font-medium text-gray-700">
                Security Code
              </label>
              <input
                id="twoFactorCode"
                name="twoFactorCode"
                type="text"
                maxLength={6}
                required
                autoFocus
                placeholder="123456"
                value={twoFactorCode}
                onChange={(e) => setTwoFactorCode(e.target.value.replace(/\D/g, ''))}
                className="mt-1 block w-full border border-black rounded-md px-3 py-2 text-center tracking-widest text-xl font-mono"
              />
            </div>
            <div>
              <button
                type="submit"
                disabled={loading || twoFactorCode.length !== 6}
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Verifying...' : 'Verify Code'}
              </button>
            </div>
            <div className="text-center">
              <button
                type="button"
                onClick={() => { setRequires2fa(false); setTwoFactorCode(''); }}
                className="text-sm text-gray-600 hover:text-black"
              >
                Back to password login
              </button>
            </div>
          </form>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handlePasswordLogin}>
            {error && (
              <div className="border border-red-300 bg-red-50 p-4 rounded text-sm text-red-800">
                {error}
              </div>
            )}
            <div className="space-y-4">
              <FloatingLabel
                id="email"
                name="email"
                type="email"
                label="Email address"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />

              <FloatingLabel
                id="password"
                name="password"
                type="password"
                label="Password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>

            <div>
              <button
                type="submit"
                disabled={loading}
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Signing in...' : 'Sign in'}
              </button>
            </div>
            <div className="text-center">
              <Link to="/signup" className="text-sm text-gray-600 hover:text-black">
                Don't have an account? Sign up
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default Login;
