import React from 'react';
import { CreditCard, ShieldCheck } from 'lucide-react';
import { cn } from '../../lib/utils';

export type PaymentGatewayType = 'Stripe' | 'CardNet';

interface PaymentProviderSelectorProps {
  selected: PaymentGatewayType;
  onChange: (provider: PaymentGatewayType) => void;
  className?: string;
}

export const PaymentProviderSelector: React.FC<PaymentProviderSelectorProps> = ({
  selected,
  onChange,
  className = '',
}) => {
  return (
    <div className={cn('space-y-3', className)}>
      <label className="block text-xs font-bold uppercase tracking-wider text-gray-700">
        Select Payment Gateway / Pasarela de Pago
      </label>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {/* Stripe Option */}
        <button
          type="button"
          onClick={() => onChange('Stripe')}
          className={cn(
            'flex items-start gap-4 p-4 rounded-2xl border-2 text-left transition-all duration-200 cursor-pointer',
            selected === 'Stripe'
              ? 'border-black bg-gray-50 shadow-md ring-1 ring-black'
              : 'border-gray-200 hover:border-gray-300 bg-white'
          )}
        >
          <div className={cn(
            'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors',
            selected === 'Stripe' ? 'bg-black text-white' : 'bg-gray-100 text-gray-600'
          )}>
            <CreditCard className="w-5 h-5" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between">
              <span className="font-bold text-sm text-gray-900">Stripe</span>
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full bg-blue-50 text-blue-700">
                USD ($)
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              International Cards (Visa, MasterCard, Amex)
            </p>
          </div>
        </button>

        {/* CardNET Option */}
        <button
          type="button"
          onClick={() => onChange('CardNet')}
          className={cn(
            'flex items-start gap-4 p-4 rounded-2xl border-2 text-left transition-all duration-200 cursor-pointer',
            selected === 'CardNet'
              ? 'border-black bg-gray-50 shadow-md ring-1 ring-black'
              : 'border-gray-200 hover:border-gray-300 bg-white'
          )}
        >
          <div className={cn(
            'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors',
            selected === 'CardNet' ? 'bg-[#D32F2F] text-white' : 'bg-gray-100 text-gray-600'
          )}>
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between">
              <span className="font-bold text-sm text-gray-900">CardNET</span>
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full bg-red-50 text-red-700">
                DOP (RD$)
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              República Dominicana (Banreservas, BHD, Popular)
            </p>
          </div>
        </button>
      </div>
    </div>
  );
};

export default PaymentProviderSelector;
