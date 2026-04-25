import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './hooks/useAuth';
import { Nav } from './components/Nav';
import Home from './pages/landing/Home';
import Pricing from './pages/landing/Pricing';
import Login from './pages/auth/Login';
import Signup from './pages/auth/Signup';
import DashboardHome from './pages/dashboard/Home';
import DashboardLayout from './pages/dashboard/Layout';
import DashboardProfile from './pages/dashboard/Profile';

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="min-h-screen flex flex-col">
          <Nav />
          <main className="flex-1">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/pricing" element={<Pricing />} />
              <Route path="/features" element={<div>Features</div>} />
              <Route path="/login" element={<Login />} />
              <Route path="/signup" element={<Signup />} />
              <Route path="/dashboard" element={<DashboardLayout />}>
                <Route index element={<DashboardHome />} />
                <Route path="profile" element={<DashboardProfile />} />
                <Route path="settings" element={<div>Settings</div>} />
                <Route path="plans" element={<div>Plans</div>} />
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;

