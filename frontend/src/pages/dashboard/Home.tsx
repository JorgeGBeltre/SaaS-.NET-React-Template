import React from 'react';
import { Link } from 'react-router-dom';

const DashboardHome = () => {
  return (
    <div className="space-y-12">
      <div>
        <h2 className="text-4xl font-bold tracking-tight text-gray-900 mb-4">
          Welcome back
        </h2>
        <p className="text-lg text-gray-500 max-w-2xl">
          Manage your account, API keys, and subscriptions from your central command center.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        {[
          {
            title: 'API Keys',
            description: 'Generate and manage your secret API access keys.',
            icon: 'key',
            href: '/dashboard/settings',
            linkText: 'Manage API keys',
          },
          {
            title: 'Subscription',
            description: 'Trial active • Ends in 14 days',
            icon: 'credit-card',
            href: '/dashboard/plans',
            linkText: 'View plans',
          },
          {
            title: 'Profile',
            description: 'Update your personal account information.',
            icon: 'user',
            href: '/dashboard/profile',
            linkText: 'Edit profile',
          },
        ].map((item) => (
          <div key={item.title} className="group bg-white border border-gray-200 rounded-2xl p-8 hover:border-black hover:shadow-2xl transition-all duration-300 flex flex-col justify-between">
            <div>
              <div className="w-12 h-12 bg-black rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform duration-300">
                <i className={`fa-solid fa-${item.icon} text-white text-lg`}></i>
              </div>
              <h3 className="text-xl font-bold text-gray-900 mb-2">{item.title}</h3>
              <p className="text-gray-500 leading-relaxed mb-8">{item.description}</p>
            </div>
            <Link 
              to={item.href} 
              className="inline-flex items-center text-sm font-bold text-gray-900 group-hover:translate-x-1 transition-transform"
            >
              {item.linkText} <span className="ml-2">→</span>
            </Link>
          </div>
        ))}
      </div>

      <div className="bg-black rounded-3xl p-10 text-white overflow-hidden relative">
        <div className="relative z-10">
          <h3 className="text-2xl font-bold mb-4">Need help getting started?</h3>
          <p className="text-gray-400 mb-8 max-w-md">
            Check out our quick start guide to integrate our API into your existing platform in minutes.
          </p>
          <button className="px-6 py-3 bg-white text-black font-bold rounded-lg hover:bg-gray-200 transition-colors">
            Read Documentation
          </button>
        </div>
        <div className="absolute right-0 bottom-0 opacity-10 translate-x-1/4 translate-y-1/4">
          <i className="fa-solid fa-rocket text-[200px]"></i>
        </div>
      </div>
    </div>
  );
};

export default DashboardHome;

