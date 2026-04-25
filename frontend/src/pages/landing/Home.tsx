import { Shield, CreditCard, Gauge, Database, Zap, Rocket, CheckCircle2 } from 'lucide-react';

const Home = () => {
  const features = [
    {
      icon: <Shield className="w-6 h-6 text-accent" />,
      title: 'Authentication',
      description: 'JWT auth with .NET backend. Email signup/login ready to go.',
    },
    {
      icon: <CreditCard className="w-6 h-6 text-accent" />,
      title: 'Stripe Payments',
      description: 'Subscriptions, trials, webhooks. Production ready Stripe integration.',
    },
    {
      icon: <Gauge className="w-6 h-6 text-accent" />,
      title: 'Dashboard',
      description: 'Profile, settings, API key management, subscription status.',
    },
    {
      icon: <Database className="w-6 h-6 text-accent" />,
      title: '.NET + EF',
      description: 'Entity Framework with SQLite/PostgreSQL. Full SaaS models.',
    },
    {
      icon: <Zap className="w-6 h-6 text-accent" />,
      title: 'Lightning Fast',
      description: 'Optimized React 18, TypeScript, and .NET 8 performance.',
    },
    {
      icon: <Rocket className="w-6 h-6 text-accent" />,
      title: 'Deploy Ready',
      description: 'Docker, CI/CD ready. Azure, Vercel, Railway compatible.',
    },
  ];

  return (
    <div className="relative overflow-hidden pt-32 pb-20">
      {/* Background Decorative Elements */}
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[1000px] h-[600px] bg-accent/10 blur-[120px] rounded-full -z-10 animate-pulse"></div>
      <div className="absolute -top-[200px] -right-[200px] w-[500px] h-[500px] bg-primary/5 blur-[100px] rounded-full -z-10"></div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Hero Section */}
        <div className="text-center mb-32 relative">
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full glass mb-8 animate-bounce">
            <CheckCircle2 className="w-4 h-4 text-accent" />
            <span className="text-xs font-bold uppercase tracking-widest text-secondary">The #1 SaaS Boilerplate for .NET</span>
          </div>
          
          <h1 className="text-6xl md:text-8xl font-black text-primary tracking-tight mb-8 leading-[0.9]">
            Build Faster. <br />
            <span className="text-accent">Scale Smarter.</span>
          </h1>
          
          <p className="text-xl text-secondary max-w-2xl mx-auto mb-12 font-medium leading-relaxed">
            Everything you need to ship your SaaS in days, not months. <br className="hidden md:block" />
            React frontend, .NET backend, and full Stripe integration.
          </p>
          
          <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
            <a href="/signup" className="w-full sm:w-auto px-10 py-5 bg-primary text-white text-lg font-bold rounded-2xl hover:bg-secondary hover:shadow-[0_0_40px_rgba(28,25,23,0.3)] premium-transition shadow-xl">
              Get Started for Free
            </a>
            <a href="/features" className="w-full sm:w-auto px-10 py-5 glass text-primary text-lg font-bold rounded-2xl hover:bg-white/80 premium-transition">
              Explore Features
            </a>
          </div>

          <div className="mt-20 pt-10 border-t border-gray-200/50 flex flex-wrap justify-center gap-12 opacity-50 grayscale hover:grayscale-0 premium-transition">
            <span className="font-bold text-xl tracking-tighter italic">STRIPE</span>
            <span className="font-bold text-xl tracking-tighter italic">AZURE</span>
            <span className="font-bold text-xl tracking-tighter italic">DOCKER</span>
            <span className="font-bold text-xl tracking-tighter italic">VITE</span>
          </div>
        </div>

        {/* Features Grid */}
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8 relative">
          {features.map((feature, index) => (
            <div key={index} className="group p-10 glass rounded-[2.5rem] hover:bg-white/90 premium-transition hover:-translate-y-2 cursor-pointer">
              <div className="w-14 h-14 mb-8 bg-primary/5 rounded-2xl flex items-center justify-center group-hover:bg-primary group-hover:scale-110 premium-transition shadow-inner">
                <div className="group-hover:text-white premium-transition">
                  {feature.icon}
                </div>
              </div>
              <h3 className="text-2xl font-bold mb-4 text-primary tracking-tight">{feature.title}</h3>
              <p className="text-secondary leading-relaxed font-medium">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Home;

