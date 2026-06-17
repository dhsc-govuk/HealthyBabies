import React from 'react';

interface RadioOption {
  value: string | number;
  label: string;
  hint?: string;
  conditional?: React.ReactNode;
}

interface GovUKRadioProps {
  id: string;
  name: string;
  legend: string;
  legendSize?: 's' | 'm' | 'l';
  questionCode?: string;
  options: RadioOption[];
  value?: string | number;
  error?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  onChange?: (value: string | number) => void;
}

function GovUKRadio({
  id,
  name,
  legend,
  legendSize = 's',
  questionCode,
  options,
  value,
  error,
  hint,
  disabled = false,
  required = false,
  onChange,
}: GovUKRadioProps): React.ReactElement {
  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}>
      {questionCode && <span className="question-code">{questionCode}</span>}
      <fieldset className="govuk-fieldset" aria-describedby={[hint ? `${id}-hint` : '', error ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}>
        <legend className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>
          {legend}
          {required && <span className="govuk-visually-hidden"> (required)</span>}
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

        <div className="govuk-radios" data-module="govuk-radios">
          {options.map((option) => (
            <React.Fragment key={String(option.value)}>
              <div className="govuk-radios__item">
                <input
                  className="govuk-radios__input"
                  id={`${id}-${option.value}`}
                  name={name}
                  type="radio"
                  value={String(option.value)}
                  checked={value === option.value}
                  disabled={disabled}
                  onChange={() => onChange?.(option.value)}
                  aria-controls={option.conditional ? `${id}-${option.value}-conditional` : undefined}
                  aria-expanded={option.conditional && value === option.value ? 'true' : undefined}
                />
                <label className="govuk-label govuk-radios__label" htmlFor={`${id}-${option.value}`}>
                  {option.label}
                </label>
                {option.hint && <div className="govuk-hint govuk-radios__hint">{option.hint}</div>}
              </div>
              {option.conditional && value === option.value && (
                <div className="govuk-radios__conditional" id={`${id}-${option.value}-conditional`}>
                  {option.conditional}
                </div>
              )}
            </React.Fragment>
          ))}
        </div>
      </fieldset>
    </div>
  );
}

export default GovUKRadio;
export type { RadioOption };
