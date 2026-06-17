import React from 'react';

interface CheckboxOption {
  value: string | number;
  label: string;
  hint?: string;
  exclusive?: boolean;
  conditionalContent?: React.ReactNode;
}

interface GovUKCheckboxProps {
  id: string;
  name: string;
  legend: string;
  legendSize?: 's' | 'm' | 'l';
  questionCode?: string;
  options: CheckboxOption[];
  value?: (string | number)[];
  error?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  size?: 'small';
  onChange?: (values: (string | number)[]) => void;
}

function GovUKCheckbox({
  id,
  name,
  legend,
  legendSize = 's',
  questionCode,
  options,
  value = [],
  error,
  hint,
  disabled = false,
  size,
  onChange,
}: GovUKCheckboxProps): React.ReactElement {
  const exclusiveOptions = options.filter((o) => o.exclusive);
  const regularOptions = options.filter((o) => !o.exclusive);
  const checkboxesClassName = ['govuk-checkboxes', size === 'small' ? 'govuk-checkboxes--small' : ''].filter(Boolean).join(' ');

  const handleChange = (optionValue: string | number, checked: boolean, isExclusive: boolean) => {
    if (isExclusive) {
      // Exclusive option: if checked, clear all others and only select this one
      onChange?.(checked ? [optionValue] : []);
    } else {
      // Regular option: if checked, remove any exclusive options
      const withoutExclusive = value.filter((v) => !exclusiveOptions.some((eo) => eo.value === v));
      const updated = checked
        ? [...withoutExclusive, optionValue]
        : withoutExclusive.filter((v) => v !== optionValue);
      onChange?.(updated);
    }
  };

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
      <fieldset className="govuk-fieldset" aria-describedby={[hint ? `${id}-hint` : '', error ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}>
        <legend className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`} style={{ paddingRight: questionCode ? '100px' : undefined }}>
          {legend}
        </legend>

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

        <div className={checkboxesClassName} data-module="govuk-checkboxes">
          {regularOptions.map((option) => (
            <React.Fragment key={String(option.value)}>
              <div className="govuk-checkboxes__item">
                <input
                  className="govuk-checkboxes__input"
                  id={`${id}-${option.value}`}
                  name={name}
                  type="checkbox"
                  value={String(option.value)}
                  checked={value.includes(option.value)}
                  disabled={disabled}
                  data-aria-controls={option.conditionalContent ? `conditional-${name}-${option.value}` : undefined}
                  onChange={(e) => handleChange(option.value, e.target.checked, false)}
                />
                <label className="govuk-label govuk-checkboxes__label" htmlFor={`${id}-${option.value}`}>
                  {option.label}
                </label>
                {option.hint && <div className="govuk-hint govuk-checkboxes__hint">{option.hint}</div>}
              </div>
              {option.conditionalContent && (
                <div 
                  className={`govuk-checkboxes__conditional ${!value.includes(option.value) ? 'govuk-checkboxes__conditional--hidden' : ''}`}
                  id={`conditional-${name}-${option.value}`}
                >
                  {option.conditionalContent}
                </div>
              )}
            </React.Fragment>
          ))}

          {exclusiveOptions.length > 0 && (
            <>
              <div className="govuk-checkboxes__divider">or</div>
              {exclusiveOptions.map((option) => (
                <div className="govuk-radios__item" key={String(option.value)}>
                  <input
                    className="govuk-radios__input"
                    id={`${id}-${option.value}`}
                    name={`${name}-exclusive`}
                    type="radio"
                    value={String(option.value)}
                    checked={value.includes(option.value)}
                    disabled={disabled}
                    onChange={(e) => handleChange(option.value, e.target.checked, true)}
                  />
                  <label className="govuk-label govuk-radios__label" htmlFor={`${id}-${option.value}`}>
                    {option.label}
                  </label>
                  {option.hint && <div className="govuk-hint govuk-radios__hint">{option.hint}</div>}
                </div>
              ))}
            </>
          )}
        </div>
      </fieldset>
    </div>
  );
}

export default GovUKCheckbox;
export type { CheckboxOption };
