import React, { useEffect, useRef, useCallback } from 'react';
import accessibleAutocomplete from 'accessible-autocomplete';
import 'accessible-autocomplete/dist/accessible-autocomplete.min.css';
import './styles.css';

export interface AutocompleteOption {
  value: string;
  label: string;
}

export interface GovUKAutocompleteProps {
  id: string;
  name: string;
  label: string;
  labelSize?: 's' | 'm' | 'l' | 'xl';
  options: AutocompleteOption[];
  value?: string;
  hint?: string;
  error?: string;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  onChange?: (value: string) => void;
}

function GovUKAutocomplete({
  id,
  name,
  label,
  labelSize = 's',
  options,
  value = '',
  hint,
  error,
  placeholder = '',
  disabled = false,
  required = false,
  onChange,
}: GovUKAutocompleteProps): React.ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);
  const initializedRef = useRef(false);
  const onChangeRef = useRef(onChange);
  const optionsRef = useRef(options);
  const valueRef = useRef(value);

  onChangeRef.current = onChange;
  optionsRef.current = options;
  valueRef.current = value;

  const getDefaultValue = useCallback(() => {
    const opt = optionsRef.current.find((o) => o.value === valueRef.current);
    return opt?.label || '';
  }, []);

  const handleConfirm = useCallback((confirmed: string | undefined) => {
    if (!confirmed) {
      onChangeRef.current?.('');
      return;
    }
    const matchedOption = optionsRef.current.find((opt) => opt.label === confirmed);
    onChangeRef.current?.(matchedOption?.value || '');
  }, []);

  useEffect(() => {
    if (!containerRef.current || initializedRef.current) return;

    const container = containerRef.current;

    accessibleAutocomplete({
      element: container,
      id,
      name,
      source: (query: string, populateResults: (results: string[]) => void) => {
        const filtered = optionsRef.current
          .filter((opt) => opt.value !== '' && opt.label.toLowerCase().includes(query.toLowerCase()))
          .map((opt) => opt.label);
        populateResults(filtered);
      },
      defaultValue: getDefaultValue(),
      placeholder,
      showNoOptionsFound: true,
      autoselect: false,
      confirmOnBlur: false,
      onConfirm: handleConfirm,
    });

    initializedRef.current = true;

    return () => {
      if (container) {
        container.innerHTML = '';
        initializedRef.current = false;
      }
    };
  }, [id, name, placeholder, handleConfirm, getDefaultValue]);

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}>
      <label className={`govuk-label govuk-label--${labelSize}`} htmlFor={id}>
        {label}
        {required && <span className="govuk-visually-hidden"> (required)</span>}
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

      <div
        ref={containerRef}
        className={`govuk-autocomplete-container ${disabled ? 'govuk-autocomplete-container--disabled' : ''}`}
        aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
      />
    </div>
  );
}

export default GovUKAutocomplete;
