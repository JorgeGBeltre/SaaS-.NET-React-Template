import React, { useId, forwardRef, useRef, useState, useEffect } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '../../lib/utils';

export interface FloatingLabelProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  variant?: 'outlined' | 'filled' | 'standard';
  helperText?: string;
  error?: string;
  labelBg?: string;
}

export const FloatingLabel = forwardRef<HTMLInputElement, FloatingLabelProps>(
  (
    {
      label,
      variant = 'outlined',
      id: customId,
      className = '',
      required,
      error,
      helperText,
      labelBg = 'bg-white',
      value,
      defaultValue,
      placeholder = ' ',
      type = 'text',
      onFocus,
      onBlur,
      ...props
    },
    ref
  ) => {
    const generatedId = useId();
    const inputId = customId || generatedId;

    const internalRef = useRef<HTMLInputElement>(null);
    const [isFocused, setIsFocused] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [hasContent, setHasContent] = useState(
      Boolean((value !== undefined && value !== '') || (defaultValue !== undefined && defaultValue !== ''))
    );

    const isPassword = type === 'password';
    const computedType = isPassword ? (showPassword ? 'text' : 'password') : type;

    const setRefs = (node: HTMLInputElement | null) => {
      internalRef.current = node;
      if (typeof ref === 'function') {
        ref(node);
      } else if (ref) {
        (ref as React.MutableRefObject<HTMLInputElement | null>).current = node;
      }
    };

    useEffect(() => {
      if (value !== undefined && value !== '') {
        setHasContent(true);
      } else if (value === '') {
        if (internalRef.current?.value) {
          setHasContent(true);
        } else {
          setHasContent(false);
        }
      }
    }, [value]);

    // Proactive autofill detection
    useEffect(() => {
      const el = internalRef.current;
      if (!el) return;

      const checkAutofill = () => {
        try {
          const autofilled = el.matches(':-webkit-autofill') || el.matches(':autofill');
          const hasVal = Boolean(el.value && el.value.length > 0);
          if (autofilled || hasVal) {
            setHasContent(true);
          }
        } catch {}
      };

      checkAutofill();
      const t1 = setTimeout(checkAutofill, 50);
      const t2 = setTimeout(checkAutofill, 200);
      const t3 = setTimeout(checkAutofill, 800);

      const handleAnimationStart = (e: AnimationEvent) => {
        if (e.animationName?.toLowerCase().includes('autofill')) {
          setHasContent(true);
        }
      };
      el.addEventListener('animationstart', handleAnimationStart);

      return () => {
        clearTimeout(t1);
        clearTimeout(t2);
        clearTimeout(t3);
        el.removeEventListener('animationstart', handleAnimationStart);
      };
    }, []);

    const isFloating = isFocused || hasContent;

    return (
      <div className="w-full">
        <div className="relative">
          <input
            ref={setRefs}
            id={inputId}
            type={computedType}
            value={value}
            defaultValue={defaultValue}
            placeholder={placeholder}
            required={required}
            onFocus={(e) => {
              setIsFocused(true);
              onFocus?.(e);
            }}
            onBlur={(e) => {
              setIsFocused(false);
              setHasContent(Boolean(e.target.value));
              onBlur?.(e);
            }}
            onChange={(e) => {
              setHasContent(Boolean(e.target.value));
              props.onChange?.(e);
            }}
            className={cn(
              'block w-full px-4 pt-5 pb-2 text-sm text-gray-900 rounded-xl border appearance-none outline-none transition-all duration-200 peer',
              isPassword && 'pr-11',
              variant === 'filled'
                ? 'bg-gray-100 border-transparent hover:border-gray-300 focus:bg-white focus:border-black focus:ring-1 focus:ring-black'
                : 'bg-white border-gray-300 hover:border-gray-400 focus:border-black focus:ring-1 focus:ring-black',
              error
                ? 'border-red-500 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                : '',
              className
            )}
            {...props}
          />
          <label
            htmlFor={inputId}
            className={cn(
              'absolute start-4 duration-200 pointer-events-none select-none transition-all origin-[0]',
              error
                ? 'text-red-500 peer-focus:text-red-500'
                : 'text-gray-500 peer-focus:text-black',
              isFloating
                ? 'top-1.5 text-xs font-semibold transform-none'
                : cn(
                    'top-1.5 text-xs font-semibold',
                    'peer-[&:placeholder-shown:not(:focus):not(:autofill):not(:-webkit-autofill)]:top-1/2 peer-[&:placeholder-shown:not(:focus):not(:autofill):not(:-webkit-autofill)]:-translate-y-1/2 peer-[&:placeholder-shown:not(:focus):not(:autofill):not(:-webkit-autofill)]:text-sm peer-[&:placeholder-shown:not(:focus):not(:autofill):not(:-webkit-autofill)]:font-normal',
                    'peer-focus:top-1.5 peer-focus:-translate-y-0 peer-focus:text-xs peer-focus:font-semibold',
                    'peer-autofill:top-1.5 peer-autofill:-translate-y-0 peer-autofill:text-xs peer-autofill:font-semibold',
                    'peer-[:-webkit-autofill]:top-1.5 peer-[:-webkit-autofill]:-translate-y-0 peer-[:-webkit-autofill]:text-xs peer-[:-webkit-autofill]:font-semibold'
                  )
            )}
          >
            {label}
            {required && <span className="text-red-500 ml-0.5">*</span>}
          </label>

          {isPassword && (
            <button
              type="button"
              tabIndex={-1}
              onClick={() => setShowPassword((prev) => !prev)}
              className="absolute end-3.5 top-1/2 -translate-y-1/2 p-1 text-gray-400 hover:text-black transition-colors cursor-pointer"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          )}
        </div>

        {error && <p className="mt-1.5 text-xs text-red-600 font-medium">{error}</p>}
        {helperText && !error && (
          <p className="mt-1 text-xs text-gray-500 leading-relaxed">{helperText}</p>
        )}
      </div>
    );
  }
);

FloatingLabel.displayName = 'FloatingLabel';

export interface FloatingLabelTextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string;
  variant?: 'outlined' | 'filled' | 'standard';
  helperText?: string;
  error?: string;
  success?: string;
  labelBg?: string;
}

export const FloatingLabelTextarea = forwardRef<HTMLTextAreaElement, FloatingLabelTextareaProps>(
  (
    {
      label,
      variant = 'outlined',
      id: customId,
      className = '',
      required,
      error,
      success,
      helperText,
      labelBg = 'bg-white',
      value,
      placeholder = ' ',
      disabled,
      rows = 4,
      ...props
    },
    ref
  ) => {
    const generatedId = useId();
    const textareaId = customId || generatedId;

    return (
      <div className="w-full">
        <div className="relative group">
          <textarea
            ref={ref}
            id={textareaId}
            value={value}
            rows={rows}
            placeholder={placeholder}
            required={required}
            disabled={disabled}
            className={cn(
              'peer block w-full appearance-none rounded-xl border bg-white px-4 pb-2.5 pt-5 text-sm text-gray-900 focus:outline-none focus:ring-1 focus:ring-black focus:border-black transition-colors duration-200 resize-y',
              error
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                : 'border-gray-300 hover:border-gray-400',
              disabled && 'opacity-50 cursor-not-allowed bg-gray-100',
              className
            )}
            {...props}
          />

          <label
            htmlFor={textareaId}
            className={cn(
              'absolute top-2 start-3.5 z-10 origin-[0] -translate-y-4 scale-75 px-1.5 text-xs font-semibold duration-200 cursor-text pointer-events-none select-none transition-all',
              labelBg,
              error
                ? 'text-red-500 peer-focus:text-red-500'
                : 'text-gray-500 peer-focus:text-black',
              'peer-placeholder-shown:top-4 peer-placeholder-shown:translate-y-0 peer-placeholder-shown:scale-100 peer-placeholder-shown:font-normal peer-placeholder-shown:text-sm',
              'peer-focus:top-2 peer-focus:-translate-y-4 peer-focus:scale-75 peer-focus:px-1.5 peer-focus:font-semibold peer-focus:text-xs'
            )}
          >
            {label}
            {required && <span className="text-red-500 ml-0.5">*</span>}
          </label>
        </div>

        {error && <p className="mt-1.5 text-xs font-medium text-red-600">{error}</p>}
        {helperText && !error && (
          <p className="mt-1.5 text-xs text-gray-500 leading-relaxed">{helperText}</p>
        )}
      </div>
    );
  }
);

FloatingLabelTextarea.displayName = 'FloatingLabelTextarea';

export const FloatingLabelInput = FloatingLabel;
export default FloatingLabel;
