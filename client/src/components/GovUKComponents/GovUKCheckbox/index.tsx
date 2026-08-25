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
  /** Standard size by default; pass 'small' for the 'Smaller checkboxes' variant. */
  size?: 'small' | 'large';
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
      const updated = checked ? [...withoutExclusive, optionValue] : withoutExclusive.filter((v) => v !== optionValue);
      onChange?.(updated);
    }
  };

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      <fieldset
        className="govuk-fieldset"
        aria-describedby={[hint ? `${id}-hint` : '', error ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}
        aria-labelledby={questionCode ? `${id}-question-code ${id}-legend` : undefined}>
        <legend id={`${id}-legend`} className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>{legend}</legend>

        {/* Identifier sits inside the fieldset after the legend (never nested in it), but is
            wired into the group's accessible NAME via aria-labelledby rather than its
            description: a group's description is not reliably announced, whereas its name
            demonstrably is — that is how the legend itself gets read out. This also matches the
            label-based fields, where the code sits inside the <label> and so forms part of the
            control's name. */}
        {questionCode && (
          <span id={`${id}-question-code`} className="govuk-visually-hidden">
            Question ID: {questionCode}:{' '}
          </span>
        )}

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
                  id={`conditional-${name}-${option.value}`}>
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
