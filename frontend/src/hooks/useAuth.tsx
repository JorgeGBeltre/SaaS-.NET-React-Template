import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';

interface User {
  email: string;
  name?: string;
  sub?: string;
}

interface AuthContextType {
  user: User | null;
  login: (token: string, refreshToken?: string) => void;
  logout: () => Promise<void>;
  refreshToken: () => Promise<boolean>;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const decoded = jwtDecode<User & { exp: number }>(token);
        if (decoded.exp * 1000 > Date.now()) {
          setUser(decoded);
          setIsAuthenticated(true);
        } else {
          // Token expired, attempt refresh
          attemptRefreshToken();
        }
      } catch {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
      }
    }
  }, []);

  const attemptRefreshToken = async (): Promise<boolean> => {
    const currentRefreshToken = localStorage.getItem('refreshToken');
    const currentToken = localStorage.getItem('token');
    if (!currentRefreshToken) {
      logoutLocal();
      return false;
    }

    try {
      const response = await fetch('/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ accessToken: currentToken, refreshToken: currentRefreshToken }),
      });

      if (response.ok) {
        const data = await response.json();
        if (data.token && data.refreshToken) {
          login(data.token, data.refreshToken);
          return true;
        }
      }
    } catch {
      // Refresh failed
    }

    logoutLocal();
    return false;
  };

  const login = (token: string, refreshToken?: string) => {
    localStorage.setItem('token', token);
    if (refreshToken) {
      localStorage.setItem('refreshToken', refreshToken);
    }
    const decoded = jwtDecode<User & { exp: number }>(token);
    setUser(decoded);
    setIsAuthenticated(true);
  };

  const logoutLocal = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    setUser(null);
    setIsAuthenticated(false);
  };

  const logout = async () => {
    const rt = localStorage.getItem('refreshToken');
    if (rt) {
      try {
        await fetch('/api/auth/logout', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: rt }),
        });
      } catch {
        // Continue with local logout
      }
    }
    logoutLocal();
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, refreshToken: attemptRefreshToken, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
