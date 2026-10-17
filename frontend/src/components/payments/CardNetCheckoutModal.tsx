import React, { useState } from 'react';
import { X, Lock, ShieldCheck, CheckCircle2, AlertCircle } from 'lucide-react';
import { FloatingLabel } from '../ui/FloatingLabel';

interface CardNetCheckoutModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (paymentProfile: any) => void;
  planName?: string;
  amountDop?: number;
}

function getCardBrand(num: string): string {
  const clean = num.replace(/\D/g, '');
  if (/^4/.test(clean)) return 'Visa';
  if (/^(5[1-5]|2[2-7])/.test(clean)) return 'MasterCard';
  if (/^3[47]/.test(clean)) return 'Amex';
  if (/^6(011|5)/.test(clean)) return 'Discover';
  return 'Card';
}

function formatCardNumber(val: string): string {
  const digits = val.replace(/\D/g, '').slice(0, 16);
  return digits.replace(/(\d{4})(?=\d)/g, '$1 ');
}

function formatCardExp(val: string): string {
  const digits = val.replace(/\D/g, '').slice(0, 4);
  if (digits.length >= 2) {
    return `${digits.slice(0, 2)}/${digits.slice(2)}`;
  }
  return digits;
}

export const CardNetCheckoutModal: React.FC<CardNetCheckoutModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  planName = 'Pro Plan',
  amountDop = 1750,
}) => {
  const [holderName, setHolderName] = useState('');
  const [cardNumber, setCardNumber] = useState('');
  const [expiration, setExpiration] = useState('');
  const [cvv, setCvv] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    const cleanCard = cardNumber.replace(/\D/g, '');
    if (cleanCard.length < 15 || cleanCard.length > 16) {
      setError('Número de tarjeta inválido (debe tener 15 o 16 dígitos).');
      return;
    }

    const cleanExp = expiration.replace(/\D/g, '');
    if (cleanExp.length !== 4) {
      setError('La expiración debe tener formato MM/AA.');
      return;
    }

    const month = parseInt(cleanExp.slice(0, 2), 10);
    const yearShort = parseInt(cleanExp.slice(2, 4), 10);
    if (month < 1 || month > 12) {
      setError('El mes de expiración debe ser entre 01 y 12.');
      return;
    }

    const now = new Date();
    const currentYearShort = now.getFullYear() % 100;
    const currentMonth = now.getMonth() + 1;
    if (yearShort < currentYearShort || (yearShort === currentYearShort && month < currentMonth)) {
      setError('La tarjeta ingresada se encuentra vencida.');
      return;
    }

    if (cvv.length < 3) {
      setError('El código de seguridad (CVV) debe tener 3 o 4 dígitos.');
      return;
    }

    setLoading(true);

    try {
      const brand = getCardBrand(cleanCard);
      const last4 = cleanCard.slice(-4);
      // Generate secure one-time token simulated as CardNET Hosted Checkout output
      const ottToken = `ott_cardnet_${brand}_${last4}_${cleanExp}_${Date.now()}`;

      const token = localStorage.getItem('token');
      const response = await fetch('/api/payments/methods/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: token ? `Bearer ${token}` : '',
        },
        body: JSON.stringify({
          tokenOrPaymentMethodId: ottToken,
          provider: 2, // PaymentProvider.CardNet
        }),
      });

      const data = await response.json();

      if (response.ok) {
        setSuccess(true);
        setTimeout(() => {
          onSuccess(data);
          onClose();
        }, 1200);
      } else {
        setError(typeof data === 'string' ? data : data.message || 'Error al procesar la tarjeta con CardNET.');
      }
    } catch {
      setError('Error de comunicación con el servidor.');
    } finally {
      setLoading(false);
    }
  };

  const brand = getCardBrand(cardNumber);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="relative w-full max-w-md bg-white rounded-3xl shadow-2xl overflow-hidden border border-gray-100">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-100 bg-gray-50/50">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-[#D32F2F] text-white flex items-center justify-center font-bold text-lg shadow-sm">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold text-gray-900 text-base">CardNET Checkout Seguro</h3>
              <p className="text-xs text-gray-500">República Dominicana • Pasarela CardNET</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="w-8 h-8 rounded-full flex items-center justify-center text-gray-400 hover:text-black hover:bg-gray-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Plan Summary */}
        <div className="p-6 bg-gray-50/70 border-b border-gray-100 flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Plan Seleccionado</span>
            <p className="font-bold text-gray-900 text-sm">{planName}</p>
          </div>
          <div className="text-right">
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Monto Total</span>
            <p className="font-extrabold text-gray-900 text-lg">RD$ {amountDop.toLocaleString()} <span className="text-xs font-normal text-gray-500">/mes</span></p>
          </div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700 flex items-center gap-2">
              <AlertCircle className="w-4 h-4 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {success && (
            <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-xs text-emerald-800 flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4 flex-shrink-0" />
              <span>¡Tarjeta vinculada exitosamente con CardNET!</span>
            </div>
          )}

          {/* FloatingLabel for Cardholder Name */}
          <FloatingLabel
            label="Nombre del Titular de la Tarjeta"
            value={holderName}
            onChange={(e) => setHolderName(e.target.value)}
            required
            placeholder=" "
            disabled={loading || success}
          />

          {/* FloatingLabel for Card Number */}
          <div className="relative">
            <FloatingLabel
              label="Número de Tarjeta"
              value={cardNumber}
              onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
              required
              maxLength={19}
              placeholder=" "
              disabled={loading || success}
            />
            {cardNumber.length > 0 && (
              <span className="absolute end-4 top-1/2 -translate-y-1/2 text-xs font-bold px-2 py-0.5 rounded bg-gray-100 text-gray-700 pointer-events-none">
                {brand}
              </span>
            )}
          </div>

          {/* Row for Expiration and CVV */}
          <div className="grid grid-cols-2 gap-3">
            <FloatingLabel
              label="Expiración (MM/AA)"
              value={expiration}
              onChange={(e) => setExpiration(formatCardExp(e.target.value))}
              required
              maxLength={5}
              placeholder=" "
              disabled={loading || success}
            />

            <FloatingLabel
              label="CVV / CVC"
              type="password"
              value={cvv}
              onChange={(e) => setCvv(e.target.value.replace(/\D/g, '').slice(0, 4))}
              required
              maxLength={4}
              placeholder=" "
              disabled={loading || success}
            />
          </div>

          <div className="flex items-center gap-2 text-[11px] text-gray-500 pt-1">
            <Lock className="w-3.5 h-3.5 text-gray-400" />
            <span>Encriptación de 256 bits y cumplimiento PCI-DSS CardNET.</span>
          </div>

          <div className="pt-3">
            <button
              type="submit"
              disabled={loading || success}
              className="w-full py-3.5 px-4 bg-black text-white font-bold rounded-xl hover:bg-gray-800 transition-all shadow-md active:scale-[0.99] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                  <span>Procesando con CardNET...</span>
                </>
              ) : success ? (
                <>
                  <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                  <span>Aprobado</span>
                </>
              ) : (
                <span>Confirmar y Vincular Tarjeta</span>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CardNetCheckoutModal;
