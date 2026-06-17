import React, { useState, useEffect } from 'react';

interface GovUKDateFieldProps {
  id: string;
  legend: string;
  legendSize?: 's' | 'm' | 'l';
  questionCode?: string;
  value?: string;
  error?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  onChange?: (dateString: string) => void;
  onBlur?: () => void;
}

function GovUKDateField({
  id,
  legend,
  legendSize = 's',
  questionCode,
  value = '',
  error,
  hint,
  disabled = false,
  required = false,
  onChange,
  onBlur,
}: GovUKDateFieldProps): React.ReactElement {
  const [day, setDay] = useState('');
  const [month, setMonth] = useState('');
  const [year, setYear] = useState('');

  useEffect(() => {
    if (value) {
      // Strip time portion if present (handles "9/9/2025 12:00:00 AM" format)
      const dateOnly = value.split(' ')[0];
      const parts = dateOnly.split('/');
      if (parts.length === 3) {
        setDay(parts[0].padStart(2, '0'));
        setMonth(parts[1].padStart(2, '0'));
        setYear(parts[2]);
      } else {
        // Handle ISO format (YYYY-MM-DD or YYYY-MM-DDTHH:MM:SS)
        const isoDateOnly = dateOnly.split('T')[0];
        const dateParts = isoDateOnly.split('-');
        if (dateParts.length === 3) {
          setYear(dateParts[0]);
          setMonth(dateParts[1].padStart(2, '0'));
          setDay(dateParts[2].padStart(2, '0'));
        }
      }
    }
  }, [value]);

  const handleChange = (newDay: string, newMonth: string, newYear: string) => {
    if (newDay && newMonth && newYear) {
      onChange?.(`${newDay}/${newMonth}/${newYear}`);
    } else if (!newDay && !newMonth && !newYear) {
      onChange?.('');
    }
  };

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}>
      {questionCode && <span className="question-code">{questionCode}</span>}
      <fieldset className="govuk-fieldset" role="group" aria-describedby={[hint ? `${id}-hint` : '', error ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}>
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

        <div className="govuk-date-input" id={id}>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-day`}>
                Day
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-2 ${error ? 'govuk-input--error' : ''}`}
                id={`${id}-day`}
                name={`${id}-day`}
                type="text"
                inputMode="numeric"
                value={day}
                disabled={disabled}
                onChange={(e) => {
                  const newDay = e.target.value;
                  setDay(newDay);
                  handleChange(newDay, month, year);
                }}
                onBlur={onBlur}
              />
            </div>
          </div>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-month`}>
                Month
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-2 ${error ? 'govuk-input--error' : ''}`}
                id={`${id}-month`}
                name={`${id}-month`}
                type="text"
                inputMode="numeric"
                value={month}
                disabled={disabled}
                onChange={(e) => {
                  const newMonth = e.target.value;
                  setMonth(newMonth);
                  handleChange(day, newMonth, year);
                }}
                onBlur={onBlur}
              />
            </div>
          </div>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-year`}>
                Year
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-4 ${error ? 'govuk-input--error' : ''}`}
                id={`${id}-year`}
                name={`${id}-year`}
                type="text"
                inputMode="numeric"
                value={year}
                disabled={disabled}
                onChange={(e) => {
                  const newYear = e.target.value;
                  setYear(newYear);
                  handleChange(day, month, newYear);
                }}
                onBlur={onBlur}
              />
            </div>
          </div>
        </div>
      </fieldset>
    </div>
  );
}

export default GovUKDateField;
