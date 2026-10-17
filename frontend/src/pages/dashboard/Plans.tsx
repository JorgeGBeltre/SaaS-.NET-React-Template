import React, { useState, useEffect } from 'react';
import { Check, Sparkles, CreditCard, CheckCircle2, ArrowRight } from 'lucide-react';
import { PaymentProviderSelector, type PaymentGatewayType } from '../../components/payments/PaymentProviderSelector';
import { CardNetCheckoutModal } from '../../components/payments/CardNetCheckoutModal';

interface Plan {
  id: string;
  name: string;
  slug: string;
  priceUsd: number;
  priceDop: number;
  description: string;
  features: string[];
  popular?: boolean;
}

const PLANS: Plan[] = [
  {
    id: 'starter',
    name: 'Starter',
    slug: 'starter',
    priceUsd: 0,
    priceDop: 0,
    description: 'Perfect for small side projects and exploration.',
    features: ['Up to 3 projects', 'Basic API access', 'Community support', 'Standard rate limits'],
    popular: false,
  },
  {
    id: 'pro',
    name: 'Pro',
    slug: 'pro',
    priceUsd: 29,
    priceDop: 1750,
    description: 'Ideal for scaling businesses and high-performance apps.',
    features: ['Unlimited projects', 'Full API access', 'Priority support', 'Webhooks & integrations', 'Custom domain support'],
    popular: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    slug: 'enterprise',
    priceUsd: 99,
    priceDop: 5900,
    description: 'Advanced compliance, SLA, and dedicated infrastructure.',
    features: ['Dedicated infrastructure', '24/7 Phone & Slack support', '99.99% SLA guarantee', 'Audit logs & e-CF support', 'Custom billing terms'],
    popular: false,
  },
];

export const Plans: React.FC = () => {
  const [selectedInterval, setSelectedInterval] = useState<'monthly' | 'annual'>('monthly');
  const [selectedGateway, setSelectedGateway] = useState<PaymentGatewayType>('CardNet');
  const [isCardNetModalOpen, setIsCardNetModalOpen] = useState(false);
  const [activePlan, setActivePlan] = useState<Plan>(PLANS[1]);
  const [loadingCheckout, setLoadingCheckout] = useState(false);
  const [paymentSuccessMessage, setPaymentSuccessMessage] = useState<string | null>(null);

  const [activeSubscription, setActiveSubscription] = useState<{
    status: string;
    brand?: string;
    last4?: string;
    provider?: string;
  } | null>(null);

  useEffect(() => {
    // Fetch active payment methods / subscription info
    const fetchMethods = async () => {
      try {
        const token = localStorage.getItem('token');
        if (!token) return;

        const res = await fetch('/api/payments/methods', {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (res.ok) {
          const methods = await res.json();
          if (Array.isArray(methods) && methods.length > 0) {
            const def = methods[0];
            setActiveSubscription({
              status: 'active',
              brand: def.brand,
              last4: def.last4,
              provider: def.provider === 2 ? 'CardNET' : 'Stripe',
            });
          }
        }
      } catch (err) {
        console.error('Error fetching payment methods:', err);
      }
    };

    fetchMethods();
  }, []);

  const handleSelectPlan = async (plan: Plan) => {
    setActivePlan(plan);

    if (plan.priceUsd === 0) {
      setPaymentSuccessMessage('Plan Starter activado exitosamente.');
      return;
    }

    if (selectedGateway === 'CardNet') {
      setIsCardNetModalOpen(true);
    } else {
      // Stripe flow
      setLoadingCheckout(true);
      try {
        const token = localStorage.getItem('token');
        const res = await fetch('/api/payments/checkout', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: token ? `Bearer ${token}` : '',
          },
          body: JSON.stringify({
            planSlug: plan.slug,
            interval: selectedInterval,
            provider: 1, // PaymentProvider.Stripe
            successUrl: window.location.origin + '/dashboard/plans?success=true',
            cancelUrl: window.location.origin + '/dashboard/plans?canceled=true',
          }),
        });

        const data = await res.json();
        if (res.ok && data.checkoutUrl) {
          if (data.checkoutUrl.startsWith('http')) {
            window.location.href = data.checkoutUrl;
          } else {
            setPaymentSuccessMessage(`Checkout de Stripe iniciado exitosamente para ${plan.name}.`);
          }
        }
      } catch {
        console.error('Error initiating Stripe checkout');
      } finally {
        setLoadingCheckout(false);
      }
    }
  };

  const handleCardNetSuccess = (profile: any) => {
    setActiveSubscription({
      status: 'active',
      brand: profile.brand || 'Visa',
      last4: profile.last4 || '4242',
      provider: 'CardNET',
    });
    setPaymentSuccessMessage(`¡Plan ${activePlan.name} activado con éxito mediante pasarela CardNET!`);
  };

  return (
    <div className="space-y-10">
      <div>
        <h2 className="text-4xl font-extrabold tracking-tight text-gray-900 mb-3">
          Planes y Suscripción
        </h2>
        <p className="text-base text-gray-500 max-w-2xl">
          Selecciona el plan adecuado para tu negocio. Puedes pagar con tarjetas internacionales vía{' '}
          <strong className="text-gray-900">Stripe (USD)</strong> o tarjetas dominicanas vía{' '}
          <strong className="text-gray-900">CardNET (DOP)</strong>.
        </p>
      </div>

      {paymentSuccessMessage && (
        <div className="p-4 bg-emerald-50 border border-emerald-200 rounded-2xl flex items-center justify-between text-emerald-800 text-sm font-medium">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />
            <span>{paymentSuccessMessage}</span>
          </div>
          <button
            type="button"
            onClick={() => setPaymentSuccessMessage(null)}
            className="text-xs text-emerald-700 underline hover:text-emerald-900"
          >
            Cerrar
          </button>
        </div>
      )}

      {/* Current Subscription Card if active */}
      {activeSubscription && (
        <div className="p-6 bg-gray-50 border border-gray-200 rounded-2xl flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-black text-white flex items-center justify-center font-bold">
              <CreditCard className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h4 className="font-bold text-gray-900">Método de Pago Vinculado</h4>
                <span className="text-xs px-2 py-0.5 rounded-full font-bold bg-emerald-100 text-emerald-800">
                  Activo
                </span>
                <span className="text-xs px-2 py-0.5 rounded-full font-bold bg-gray-200 text-gray-800">
                  {activeSubscription.provider}
                </span>
              </div>
              <p className="text-xs text-gray-500 mt-1">
                Tarjeta {activeSubscription.brand} terminada en •••• {activeSubscription.last4}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setIsCardNetModalOpen(true)}
            className="px-4 py-2 border border-gray-300 rounded-xl text-xs font-bold text-gray-700 hover:bg-white transition-colors"
          >
            Actualizar Tarjeta
          </button>
        </div>
      )}

      {/* Gateway Selector (Strategy Pattern) */}
      <div className="bg-white p-6 rounded-3xl border border-gray-200 shadow-sm max-w-2xl">
        <PaymentProviderSelector
          selected={selectedGateway}
          onChange={setSelectedGateway}
        />
      </div>

      {/* Billing Interval Toggle */}
      <div className="flex items-center justify-center gap-3">
        <button
          type="button"
          onClick={() => setSelectedInterval('monthly')}
          className={`px-5 py-2 rounded-xl text-sm font-bold transition-all ${
            selectedInterval === 'monthly'
              ? 'bg-black text-white shadow-md'
              : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          Facturación Mensual
        </button>
        <button
          type="button"
          onClick={() => setSelectedInterval('annual')}
          className={`px-5 py-2 rounded-xl text-sm font-bold transition-all flex items-center gap-1.5 ${
            selectedInterval === 'annual'
              ? 'bg-black text-white shadow-md'
              : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          <span>Facturación Anual</span>
          <span className="text-[10px] bg-amber-400 text-black px-1.5 py-0.5 rounded-full font-extrabold uppercase">
            Ahorra 20%
          </span>
        </button>
      </div>

      {/* Plans Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        {PLANS.map((plan) => {
          const isSelected = activePlan.slug === plan.slug;
          const displayPrice = selectedGateway === 'CardNet'
            ? plan.priceDop === 0 ? 'RD$ 0' : `RD$ ${selectedInterval === 'annual' ? Math.round(plan.priceDop * 0.8).toLocaleString() : plan.priceDop.toLocaleString()}`
            : plan.priceUsd === 0 ? '$0' : `$${selectedInterval === 'annual' ? Math.round(plan.priceUsd * 0.8) : plan.priceUsd}`;

          return (
            <div
              key={plan.id}
              className={`relative bg-white rounded-3xl p-8 border-2 transition-all duration-300 flex flex-col justify-between ${
                plan.popular
                  ? 'border-black shadow-xl ring-1 ring-black'
                  : 'border-gray-200 hover:border-gray-300 shadow-sm'
              }`}
            >
              {plan.popular && (
                <div className="absolute -top-3.5 left-1/2 -translate-x-1/2 px-3 py-1 bg-black text-white text-[11px] font-bold uppercase tracking-wider rounded-full flex items-center gap-1">
                  <Sparkles className="w-3 h-3 text-amber-400" />
                  Recomendado
                </div>
              )}

              <div>
                <h3 className="text-2xl font-bold text-gray-900 mb-2">{plan.name}</h3>
                <p className="text-xs text-gray-500 leading-relaxed mb-6">{plan.description}</p>

                <div className="mb-8">
                  <span className="text-4xl font-extrabold text-gray-900">{displayPrice}</span>
                  {plan.priceUsd > 0 && (
                    <span className="text-xs text-gray-500 font-medium ml-1">/mes</span>
                  )}
                </div>

                <div className="space-y-3 mb-8">
                  {plan.features.map((feature, i) => (
                    <div key={i} className="flex items-center gap-2.5 text-xs text-gray-700 font-medium">
                      <div className="w-4 h-4 rounded-full bg-emerald-100 flex items-center justify-center flex-shrink-0">
                        <Check className="w-2.5 h-2.5 text-emerald-700 stroke-[3]" />
                      </div>
                      <span>{feature}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <button
                  type="button"
                  onClick={() => handleSelectPlan(plan)}
                  disabled={loadingCheckout}
                  className={`w-full py-3.5 px-4 rounded-xl font-bold text-xs uppercase tracking-wider transition-all flex items-center justify-center gap-2 shadow-sm ${
                    plan.popular
                      ? 'bg-black text-white hover:bg-gray-800'
                      : 'bg-gray-100 text-gray-900 hover:bg-gray-200'
                  }`}
                >
                  {loadingCheckout && isSelected ? (
                    <span>Procesando...</span>
                  ) : (
                    <>
                      <span>{plan.priceUsd === 0 ? 'Plan Actual' : `Elegir con ${selectedGateway}`}</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </>
                  )}
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* CardNet Modal */}
      <CardNetCheckoutModal
        isOpen={isCardNetModalOpen}
        onClose={() => setIsCardNetModalOpen(false)}
        onSuccess={handleCardNetSuccess}
        planName={activePlan.name}
        amountDop={activePlan.priceDop}
      />
    </div>
  );
};

export default Plans;
