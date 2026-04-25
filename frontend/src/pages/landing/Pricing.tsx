import { Check, Sparkles } from 'lucide-react';

const Pricing = () => {
  const tiers = [
    {
      name: 'Starter',
      price: '$0',
      description: 'Perfect for exploring the possibilities.',
      features: ['Up to 3 projects', 'Community support', 'Basic API access'],
      cta: 'Get Started',
      popular: false,
    },
    {
      name: 'Pro',
      price: '$29',
      description: 'Everything you need to scale your SaaS.',
      features: ['Unlimited projects', 'Priority support', 'Full API access', 'Custom domains'],
      cta: 'Start Free Trial',
      popular: true,
    },
    {
      name: 'Enterprise',
      price: 'Custom',
      description: 'Advanced features for large teams.',
      features: ['Dedicated support', 'SLA guarantees', 'Custom integrations', 'Advanced security'],
      cta: 'Contact Sales',
      popular: false,
    },
  ];

  return (
    <div className="relative pt-32 pb-20">
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-accent/5 blur-[100px] rounded-full -z-10"></div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-20">
          <h1 className="text-5xl md:text-7xl font-black text-primary mb-6 tracking-tight">
            Simple, <span className="text-accent">Transparent</span> Pricing
          </h1>
          <p className="text-xl text-secondary max-w-2xl mx-auto font-medium">
            Choose the plan that's right for you. No hidden fees, no surprises.
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
          {tiers.map((tier, index) => (
            <div 
              key={index} 
              className={`relative p-10 glass rounded-[2.5rem] flex flex-col premium-transition hover:-translate-y-2 ${tier.popular ? 'border-accent/40 shadow-[0_20px_50px_rgba(202,138,4,0.15)] ring-1 ring-accent/20' : ''}`}
            >
              {tier.popular && (
                <div className="absolute -top-4 left-1/2 -translate-x-1/2 px-4 py-1 bg-accent text-white text-xs font-bold uppercase tracking-widest rounded-full flex items-center gap-1 shadow-lg">
                  <Sparkles className="w-3 h-3" />
                  Most Popular
                </div>
              )}
              
              <div className="mb-8">
                <h3 className="text-2xl font-bold text-primary mb-2 tracking-tight">{tier.name}</h3>
                <p className="text-secondary font-medium text-sm">{tier.description}</p>
              </div>

              <div className="mb-10">
                <span className="text-5xl font-black text-primary tracking-tighter">{tier.price}</span>
                {tier.price !== 'Custom' && <span className="text-secondary font-bold text-lg ml-1">/mo</span>}
              </div>

              <ul className="space-y-5 mb-12 flex-1">
                {tier.features.map((feature, fIndex) => (
                  <li key={fIndex} className="flex items-center gap-3 text-secondary font-medium">
                    <div className="flex-shrink-0 w-5 h-5 bg-accent/10 rounded-full flex items-center justify-center">
                      <Check className="w-3 h-3 text-accent stroke-[3]" />
                    </div>
                    {feature}
                  </li>
                ))}
              </ul>

              <a 
                href="/signup" 
                className={`w-full py-5 px-6 text-center font-bold rounded-2xl premium-transition shadow-lg ${
                  tier.popular 
                    ? 'bg-primary text-white hover:bg-secondary hover:shadow-primary/20' 
                    : 'glass text-primary hover:bg-white/80'
                }`}
              >
                {tier.cta}
              </a>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Pricing;

