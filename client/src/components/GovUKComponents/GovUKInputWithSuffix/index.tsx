import React from 'react';

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
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

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
  onChange,
}) => {
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
          — {label}
          {questionCode && <sup style={{ color: '#505a5f', fontSize: '14px', fontWeight: 'normal', marginLeft: '2px' }}>{questionCode}</sup>}
        </span>
        <div style={{ display: 'flex', alignItems: 'stretch' }}>
          <input
            className={inputClassName}
            id={id}
            name={name}
            type={type}
            value={value}
            disabled={disabled}
            aria-label={label}
            onChange={onChange}
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
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      {questionCode && (
        <span style={{ 
          position: 'absolute', 
          right: 0, 
          top: 0, 
          color: '#505a5f', 
          fontSize: '16px', 
          fontWeight: 'normal', 
          whiteSpace: 'nowrap' 
        }}>
          {questionCode}
        </span>
      )}
      <label className="govuk-label govuk-label--s" htmlFor={id} style={{ paddingRight: questionCode ? '100px' : undefined }}>
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
          className={inputClassName}
          id={id}
          name={name}
          type={type}
          value={value}
          disabled={disabled}
          aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
          onChange={onChange}
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
