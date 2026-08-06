import React, { useState, useEffect, useRef } from 'react';
import { validateDateParts } from '../../../helpers/validateDate';

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
  allowFuture?: boolean;
  disallowPast?: boolean;
  onChange?: (dateString: string) => void;
  onBlur?: () => void;
  onValidate?: (err: string | undefined) => void;
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
  allowFuture,
  disallowPast,
  onChange,
  onBlur,
  onValidate,
}: GovUKDateFieldProps): React.ReactElement {
  const [day, setDay] = useState('');
  const [month, setMonth] = useState('');
  const [year, setYear] = useState('');

  const hasDateConstraint = allowFuture !== undefined || disallowPast !== undefined;
  // Only live-validate once all three parts are filled in — a day typed on its own isn't
  // "missing a month and a year" yet, it's just not finished. Genuinely incomplete dates are
  // still caught at submit time by the parent's own required-field check. A single/double-digit
  // day or month is already a complete, meaningful value (e.g. "3" = the 3rd) but a year always
  // needs its full 4 digits — "202" isn't a partial year, it just isn't one yet.
  const isComplete = !!day && !!month && year.length === 4;
  const dateRuleError =
    hasDateConstraint && isComplete
      ? validateDateParts(day, month, year, { allowFuture: allowFuture ?? true, disallowPast: disallowPast ?? false, legendText: legend }).error
      : undefined;

  useEffect(() => {
    if (hasDateConstraint) {
      onValidate?.(dateRuleError);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dateRuleError, hasDateConstraint]);

  const displayError = error ?? dateRuleError;

  // Tracks the last value we ourselves sent via onChange, so the effect below
  // can ignore the round-trip echo of that change and avoid clobbering
  // in-progress typing (e.g. re-padding "1" to "01" mid-keystroke).
  const lastEmittedRef = useRef('');

  useEffect(() => {
    if (value === lastEmittedRef.current) {
      return;
    }

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
    } else {
      setDay('');
      setMonth('');
      setYear('');
    }
  }, [value]);

  const handleChange = (newDay: string, newMonth: string, newYear: string) => {
    if (newDay && newMonth && newYear) {
      const combined = `${newDay}/${newMonth}/${newYear}`;
      lastEmittedRef.current = combined;
      onChange?.(combined);
    } else if (!newDay && !newMonth && !newYear) {
      lastEmittedRef.current = '';
      onChange?.('');
    }
  };

  const handleFieldBlur = () => {
    // Pad a single-digit day/month once the user leaves the field, rather
    // than on every keystroke, so typing two digits isn't interrupted.
    const paddedDay = day ? day.padStart(2, '0') : day;
    const paddedMonth = month ? month.padStart(2, '0') : month;

    if (paddedDay !== day || paddedMonth !== month) {
      setDay(paddedDay);
      setMonth(paddedMonth);
      handleChange(paddedDay, paddedMonth, year);
    }

    onBlur?.();
  };

  return (
    <div className={`govuk-form-group ${displayError ? 'govuk-form-group--error' : ''}`}>
      {questionCode && <span className="question-code">{questionCode}</span>}
      <fieldset className="govuk-fieldset" role="group" aria-describedby={[hint ? `${id}-hint` : '', displayError ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}>
        <legend className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>
          {legend}
          {required && <span className="govuk-visually-hidden"> (required)</span>}
        </legend>

        {hint && (
          <div className="govuk-hint" id={`${id}-hint`}>
            {hint}
          </div>
        )}

        {displayError && (
          <p className="govuk-error-message" id={`${id}-error`}>
            <span className="govuk-visually-hidden">Error:</span> {displayError}
          </p>
        )}

        <div className="govuk-date-input" id={id}>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-day`}>
                Day
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-2 ${displayError ? 'govuk-input--error' : ''}`}
                id={`${id}-day`}
                name={`${id}-day`}
                type="text"
                inputMode="numeric"
                maxLength={2}
                value={day}
                disabled={disabled}
                onChange={(e) => {
                  const newDay = e.target.value;
                  setDay(newDay);
                  handleChange(newDay, month, year);
                }}
                onBlur={handleFieldBlur}
              />
            </div>
          </div>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-month`}>
                Month
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-2 ${displayError ? 'govuk-input--error' : ''}`}
                id={`${id}-month`}
                name={`${id}-month`}
                type="text"
                maxLength={2}
                inputMode="numeric"
                value={month}
                disabled={disabled}
                onChange={(e) => {
                  const newMonth = e.target.value;
                  setMonth(newMonth);
                  handleChange(day, newMonth, year);
                }}
                onBlur={handleFieldBlur}
              />
            </div>
          </div>
          <div className="govuk-date-input__item">
            <div className="govuk-form-group">
              <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-year`}>
                Year
              </label>
              <input
                className={`govuk-input govuk-date-input__input govuk-input--width-4 ${displayError ? 'govuk-input--error' : ''}`}
                id={`${id}-year`}
                name={`${id}-year`}
                type="text"
                inputMode="numeric"
                maxLength={4}
                value={year}
                disabled={disabled}
                onChange={(e) => {
                  const newYear = e.target.value;
                  setYear(newYear);
                  handleChange(day, month, newYear);
                }}
                onBlur={handleFieldBlur}
              />
            </div>
          </div>
        </div>
      </fieldset>
    </div>
  );
}

export default GovUKDateField;
