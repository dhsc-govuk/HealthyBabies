import React, { useLayoutEffect, useRef } from 'react';

interface GovUKInputWithSuffixProps {
  id: string;
  name: string;
  label: string;
  suffix?: string;
  prefix?: string;
  hint?: string;
  error?: string;
  value?: string;
  type?: 'text' | 'number';
  width?: 'full' | 'three-quarters' | 'two-thirds' | 'one-half' | 'one-third' | 'one-quarter' | '20' | '10' | '5' | '4' | '3' | '2';
  questionCode?: string;
  disabled?: boolean;
  inline?: boolean;
  /**
   * Displays the value with thousand separators (1,234.56) while typing, purely as a display
   * aesthetic — onChange still emits the plain unformatted number, so state/backend/parseFloat
   * are unaffected. Renders as a text input under the hood (a native number input rejects commas
   * outright), with inputMode="decimal" to keep the numeric keypad on mobile.
   */
  formatThousands?: boolean;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

// Strips everything but digits and a single decimal point (first one wins, rest are dropped).
const toPlainNumberString = (raw: string): string => {
  const digitsAndDots = raw.replace(/[^\d.]/g, '');
  const firstDot = digitsAndDots.indexOf('.');
  if (firstDot === -1) return digitsAndDots;
  return digitsAndDots.slice(0, firstDot + 1) + digitsAndDots.slice(firstDot + 1).replace(/\./g, '');
};

// "120000.5" -> "120,000.5". Never forces decimal places, so it doesn't fight the user mid-type.
const formatWithThousands = (plain: string): string => {
  if (!plain) return plain;
  const [intPart, ...rest] = plain.split('.');
  const formattedInt = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  return rest.length > 0 ? `${formattedInt}.${rest.join('')}` : formattedInt;
};

// How many "value" characters (digits/dot, i.e. what toPlainNumberString keeps) sit before the
// cursor in the raw (possibly just-typed, comma-containing) input.
const countValueCharsBefore = (raw: string, cursorPos: number): number => toPlainNumberString(raw.slice(0, cursorPos)).length;

// Inverse of the above: find the index in `formatted` right after the nth value character.
const indexAfterNthValueChar = (formatted: string, n: number): number => {
  if (n <= 0) return 0;
  let count = 0;
  for (let i = 0; i < formatted.length; i++) {
    if (/[\d.]/.test(formatted[i])) {
      count++;
      if (count === n) return i + 1;
    }
  }
  return formatted.length;
};

const GovUKInputWithSuffix: React.FC<GovUKInputWithSuffixProps> = ({
  id,
  name,
  label,
  suffix,
  prefix,
  hint,
  error,
  value = '',
  type = 'text',
  width = '5',
  questionCode,
  disabled = false,
  inline = false,
  formatThousands = false,
  onChange,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const pendingCursorRef = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (pendingCursorRef.current !== null && inputRef.current) {
      inputRef.current.setSelectionRange(pendingCursorRef.current, pendingCursorRef.current);
      pendingCursorRef.current = null;
    }
  }, [value]);

  const handleFormattedChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const cursorPos = e.target.selectionStart ?? e.target.value.length;
    const valueCharsBeforeCursor = countValueCharsBefore(e.target.value, cursorPos);
    const plain = toPlainNumberString(e.target.value);

    pendingCursorRef.current = indexAfterNthValueChar(formatWithThousands(plain), valueCharsBeforeCursor);

    // Give the change event downstream the plain (unformatted) number — same shape it always was.
    // A DOM node's properties (e.target.value etc.) are prototype accessors, not own enumerable
    // properties, so spreading e.target wouldn't carry them; every caller only reads .value anyway.
    onChange?.({ target: { value: plain } } as React.ChangeEvent<HTMLInputElement>);
  };

  const displayValue = formatThousands ? formatWithThousands(value) : value;
  const effectiveType = formatThousands ? 'text' : type;
  const effectiveOnChange = formatThousands ? handleFormattedChange : onChange;
  const getWidthClass = () => {
    if (!width) return '';
    const numericWidths = ['20', '10', '5', '4', '3', '2'];
    if (numericWidths.includes(width)) {
      return `govuk-input--width-${width}`;
    }
    return `govuk-!-width-${width}`;
  };

  const inputClassName = ['govuk-input', error ? 'govuk-input--error' : '', getWidthClass()].filter(Boolean).join(' ');

  if (inline) {
    return (
      <div
        className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '15px',
          marginBottom: '15px',
        }}
      >
        <span
          className="govuk-body"
          style={{
            minWidth: '150px',
            display: 'flex',
            alignItems: 'center',
          }}
        >
          {questionCode && <span className="govuk-visually-hidden">Question ID: {questionCode}: </span>}
          — {label}
        </span>
        <div style={{ display: 'flex', alignItems: 'stretch' }}>
          <input
            ref={inputRef}
            className={inputClassName}
            id={id}
            name={name}
            type={effectiveType}
            inputMode={formatThousands ? 'decimal' : undefined}
            value={displayValue}
            disabled={disabled}
            aria-label={label}
            onChange={effectiveOnChange}
            style={{
              borderRight: suffix ? 'none' : undefined,
            }}
          />
          {suffix && (
            <div
              style={{
                backgroundColor: '#f3f2f1',
                border: '2px solid #0b0c0c',
                borderLeft: 'none',
                padding: '5px 10px',
                fontSize: '19px',
                lineHeight: '25px',
                display: 'flex',
                alignItems: 'center',
              }}
              aria-hidden="true"
            >
              {suffix}
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}>
      <label className="govuk-label govuk-label--s" htmlFor={id}>
        {questionCode && <span className="govuk-visually-hidden">Question ID: {questionCode}: </span>}
        {label}
      </label>

      {hint && (
        <div className="govuk-hint" id={`${id}-hint`}>
          {hint}
        </div>
      )}

      {error && (
        <p className="govuk-error-message" id={`${id}-error`}>
          <span className="govuk-visually-hidden">Error:</span> {error}
        </p>
      )}

      <div style={{ display: 'flex', alignItems: 'stretch' }}>
        {prefix && (
          <div
            style={{
              backgroundColor: '#f3f2f1',
              border: '2px solid #0b0c0c',
              borderRight: 'none',
              padding: '5px 10px',
              fontSize: '19px',
              lineHeight: '25px',
              display: 'flex',
              alignItems: 'center',
            }}
            aria-hidden="true"
          >
            {prefix}
          </div>
        )}
        <input
          ref={inputRef}
          className={inputClassName}
          id={id}
          name={name}
          type={effectiveType}
          inputMode={formatThousands ? 'decimal' : undefined}
          value={displayValue}
          disabled={disabled}
          aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
          onChange={effectiveOnChange}
          style={{
            borderRight: suffix ? 'none' : undefined,
            borderLeft: prefix ? 'none' : undefined,
          }}
        />
        {suffix && (
          <div
            style={{
              backgroundColor: '#f3f2f1',
              border: '2px solid #0b0c0c',
              borderLeft: 'none',
              padding: '5px 10px',
              fontSize: '19px',
              lineHeight: '25px',
              display: 'flex',
              alignItems: 'center',
            }}
            aria-hidden="true"
          >
            {suffix}
          </div>
        )}
      </div>
    </div>
  );
};

export default GovUKInputWithSuffix;
