import React from 'react';

/* =======================
   Interfaces
======================= */

interface GovUKFieldsetProps {
  legend: string;
  legendSize?: 's' | 'm' | 'l' | 'xl';
  children: React.ReactNode;
}

interface GovUKFieldsetInputProps {
  id: string;
  name: string;
  label: string;
  value?: string;
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  hint?: string;
  error?: string;
  width?: '2' | '3' | '4' | '5' | '10' | '20' | 'two-thirds' | 'full';
  disabled?: boolean;
  required?: boolean;
  readOnly?: boolean;
  autoComplete?: string;
  spellCheck?: boolean;
  labelSize?: 's' | 'm' | 'l' | 'xl';
  questionCode?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: (e: React.FocusEvent<HTMLInputElement>) => void;
  onKeyUp?: (e: React.KeyboardEvent<HTMLInputElement>) => void;
}

interface GovUKFieldsetCheckboxProps {
  id: string;
  name: string;
  label: string;
  labelSize?: 's' | 'm' | 'l' | 'xl';
  checked?: boolean;
  hint?: string;
  disabled?: boolean;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

interface GovUKFieldsetSelectProps {
  id: string;
  name: string;
  label: string;
  labelSize?: 's' | 'm' | 'l' | 'xl';
  value?: string;
  options: { value: string; label: string }[];
  hint?: string;
  error?: string;
  width?: 'one-quarter' | 'one-third' | 'one-half' | 'two-thirds' | 'full';
  disabled?: boolean;
  required?: boolean;
  questionCode?: string;
  onChange?: (e: React.ChangeEvent<HTMLSelectElement>) => void;
}

interface RadioOption {
  value: string;
  text: string;
  hint?: string;
  conditionalContent?: React.ReactNode;
}

interface GovUKFieldsetRadioGroupProps {
  name: string;
  legend: string;
  legendSize?: 's' | 'm' | 'l';
  labelSize?: 's' | 'm' | 'l' | 'xl';
  options: RadioOption[];
  value?: string;
  error?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  questionCode?: string;
  /** Radios/checkboxes use the 'Smaller' variant by default; pass 'large' to opt out. */
  size?: 'small' | 'large';
  onChange?: (value: string) => void;
}

interface CheckboxOption {
  value: string;
  text: string;
  hint?: string;
}

interface GovUKFieldsetCheckboxGroupProps {
  name: string;
  legend: string;
  legendSize?: 's' | 'm' | 'l';
  labelSize?: 's' | 'm' | 'l' | 'xl';
  options: CheckboxOption[];
  value?: string[];
  error?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  questionCode?: string;
  /** Radios/checkboxes use the 'Smaller' variant by default; pass 'large' to opt out. */
  size?: 'small' | 'large';
  onChange?: (value: string, checked: boolean, allValues: string[]) => void;
}

interface GovUKFieldsetTextareaProps {
  id: string;
  name: string;
  label: string;
  labelSize?: 's' | 'm' | 'l' | 'xl';
  value?: string;
  rows?: number;
  hint?: string;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  questionCode?: string;
  onChange?: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  onBlur?: (e: React.FocusEvent<HTMLTextAreaElement>) => void;
}

/* =======================
   Components
======================= */

function GovUKFieldsetInput({
  id,
  name,
  label,
  value = '',
  type = 'text',
  hint,
  error,
  width,
  disabled = false,
  required = false,
  readOnly = false,
  autoComplete,
  spellCheck,
  questionCode,
  labelSize = 's',
  onChange,
  onBlur,
  onKeyUp,
}: GovUKFieldsetInputProps): React.ReactElement {
  const getWidthClass = () => {
    if (!width) return '';
    if (width === 'two-thirds') return 'govuk-!-width-two-thirds';
    if (width === 'full') return '';
    return `govuk-input--width-${width}`;
  };

  const inputClassName = ['govuk-input', error ? 'govuk-input--error' : '', getWidthClass()].filter(Boolean).join(' ');

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      <label className={`govuk-label govuk-label--${labelSize}`} htmlFor={id}>
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

      <input
        className={inputClassName}
        id={id}
        name={name}
        type={type}
        value={value}
        disabled={disabled}
        required={required}
        readOnly={readOnly}
        autoComplete={autoComplete}
        spellCheck={spellCheck}
        aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
        onChange={onChange}
        onBlur={onBlur}
        onKeyUp={onKeyUp}
      />
    </div>
  );
}

function GovUKFieldsetCheckbox({ id, name, label, labelSize = 's', checked = false, hint, disabled = false, onChange }: GovUKFieldsetCheckboxProps): React.ReactElement {
  return (
    <div className="govuk-form-group">
      <div className="govuk-checkboxes" data-module="govuk-checkboxes">
        <div className="govuk-checkboxes__item">
          <input className="govuk-checkboxes__input" id={id} name={name} type="checkbox" checked={checked} disabled={disabled} onChange={onChange} />
          <label className="govuk-label govuk-checkboxes__label" htmlFor={id}>
            {label}
          </label>
          {hint && (
            <div className="govuk-hint govuk-checkboxes__hint" id={`${id}-hint`}>
              {hint}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function GovUKFieldsetSelect({
  id,
  name,
  label,
  labelSize = 's',
  value = '',
  options,
  hint,
  error,
  width,
  disabled = false,
  required = false,
  questionCode,
  onChange,
}: GovUKFieldsetSelectProps): React.ReactElement {
  const getWidthClass = () => {
    if (!width || width === 'full') return '';
    return `govuk-!-width-${width}`;
  };

  const selectClassName = ['govuk-select', error ? 'govuk-select--error' : '', getWidthClass()].filter(Boolean).join(' ');
  const isFullWidth = !width || width === 'full';

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      <label className={`govuk-label govuk-label--${labelSize}`} htmlFor={id}>
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

      <select
        className={selectClassName}
        id={id}
        name={name}
        value={value}
        disabled={disabled}
        required={required}
        aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
        onChange={onChange}
        style={isFullWidth ? { width: '100%' } : undefined}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

function GovUKFieldsetRadioGroup({
  name,
  legend,
  legendSize = 's',
  options,
  value,
  error,
  hint,
  disabled = false,
  required = false,
  questionCode,
  size,
  onChange,
}: GovUKFieldsetRadioGroupProps): React.ReactElement {
  const fieldsetId = `${name}-fieldset`;
  // 'Smaller radios' is the service-wide default (DAC_Styles_and_Layout_Usability_09); it is
  // the variant with a visible hover state. Callers can still opt out with size="large".
  const radiosClassName = ['govuk-radios', size === 'large' ? '' : 'govuk-radios--small'].filter(Boolean).join(' ');

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      <fieldset
        className="govuk-fieldset"
        aria-describedby={[hint ? `${fieldsetId}-hint` : '', error ? `${fieldsetId}-error` : ''].filter(Boolean).join(' ') || undefined}
        aria-labelledby={questionCode ? `${fieldsetId}-question-code ${fieldsetId}-legend` : undefined}>
        <legend id={`${fieldsetId}-legend`} className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>
          {legend}
          {required && <span className="govuk-visually-hidden"> (required)</span>}
        </legend>

        {/* Identifier sits inside the fieldset after the legend (never nested in it), but is
            wired into the group's accessible NAME via aria-labelledby rather than its
            description: a group's description is not reliably announced, whereas its name
            demonstrably is — that is how the legend itself gets read out. This also matches the
            label-based fields, where the code sits inside the <label> and so forms part of the
            control's name. */}
        {questionCode && (
          <span id={`${fieldsetId}-question-code`} className="govuk-visually-hidden">
            Question ID: {questionCode}:{' '}
          </span>
        )}

        {hint && (
          <div className="govuk-hint" id={`${fieldsetId}-hint`}>
            {hint}
          </div>
        )}

        {error && (
          <p className="govuk-error-message" id={`${fieldsetId}-error`}>
            <span className="govuk-visually-hidden">Error:</span> {error}
          </p>
        )}

        <div className={radiosClassName} data-module="govuk-radios">
          {options.map((option: RadioOption) => (
            <React.Fragment key={option.value}>
              <div className="govuk-radios__item">
                <input
                  className="govuk-radios__input"
                  id={`${name}-${option.value}`}
                  name={name}
                  type="radio"
                  value={option.value}
                  checked={value === option.value}
                  disabled={disabled}
                  data-aria-controls={option.conditionalContent ? `conditional-${name}-${option.value}` : undefined}
                  onChange={() => onChange?.(option.value)}
                />
                <label className="govuk-label govuk-radios__label" htmlFor={`${name}-${option.value}`}>
                  {option.text}
                </label>
                {option.hint && <div className="govuk-hint govuk-radios__hint">{option.hint}</div>}
              </div>
              {option.conditionalContent && (
                <div className={`govuk-radios__conditional ${value !== option.value ? 'govuk-radios__conditional--hidden' : ''}`} id={`conditional-${name}-${option.value}`}>
                  {option.conditionalContent}
                </div>
              )}
            </React.Fragment>
          ))}
        </div>
      </fieldset>
    </div>
  );
}

function GovUKFieldsetCheckboxGroup({
  name,
  legend,
  legendSize = 's',
  options,
  value = [],
  error,
  hint,
  disabled = false,
  questionCode,
  size,
  onChange,
}: GovUKFieldsetCheckboxGroupProps): React.ReactElement {
  const fieldsetId = `${name}-fieldset`;
  const handleChange = (optionValue: string, checked: boolean) => {
    const updated = checked ? [...value, optionValue] : value.filter((v: string) => v !== optionValue);
    onChange?.(optionValue, checked, updated);
  };
  const checkboxesClassName = ['govuk-checkboxes', size === 'small' ? 'govuk-checkboxes--small' : ''].filter(Boolean).join(' ');

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`}>
      <fieldset
        className="govuk-fieldset"
        aria-describedby={[hint ? `${fieldsetId}-hint` : '', error ? `${fieldsetId}-error` : ''].filter(Boolean).join(' ') || undefined}
        aria-labelledby={questionCode ? `${fieldsetId}-question-code ${fieldsetId}-legend` : undefined}>
        <legend id={`${fieldsetId}-legend`} className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>
          {legend}
        </legend>

        {/* Identifier sits inside the fieldset after the legend (never nested in it), but is
            wired into the group's accessible NAME via aria-labelledby rather than its
            description: a group's description is not reliably announced, whereas its name
            demonstrably is — that is how the legend itself gets read out. This also matches the
            label-based fields, where the code sits inside the <label> and so forms part of the
            control's name. */}
        {questionCode && (
          <span id={`${fieldsetId}-question-code`} className="govuk-visually-hidden">
            Question ID: {questionCode}:{' '}
          </span>
        )}

        {hint && (
          <div className="govuk-hint" id={`${fieldsetId}-hint`}>
            {hint}
          </div>
        )}

        {error && (
          <p className="govuk-error-message" id={`${fieldsetId}-error`}>
            <span className="govuk-visually-hidden">Error:</span> {error}
          </p>
        )}

        <div className={checkboxesClassName} data-module="govuk-checkboxes">
          {options.map((option: CheckboxOption) => (
            <div className="govuk-checkboxes__item" key={option.value}>
              <input
                className="govuk-checkboxes__input"
                id={`${name}-${option.value}`}
                type="checkbox"
                checked={value.includes(option.value)}
                disabled={disabled}
                onChange={(e) => handleChange(option.value, e.target.checked)}
              />
              <label className="govuk-label govuk-checkboxes__label" htmlFor={`${name}-${option.value}`}>
                {option.text}
              </label>
            </div>
          ))}
        </div>
      </fieldset>
    </div>
  );
}

function GovUKFieldsetTextarea({
  id,
  name,
  label,
  labelSize = 's',
  value = '',
  rows = 4,
  hint,
  error,
  disabled = false,
  required = false,
  questionCode,
  onChange,
  onBlur,
}: GovUKFieldsetTextareaProps): React.ReactElement {
  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      <label className={`govuk-label govuk-label--${labelSize}`} htmlFor={id}>
        {questionCode && <span className="govuk-visually-hidden">Question ID: {questionCode}: </span>}
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

      <textarea
        className={`govuk-textarea ${error ? 'govuk-textarea--error' : ''}`}
        id={id}
        name={name}
        rows={rows}
        value={value}
        disabled={disabled}
        required={required}
        aria-describedby={[hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
        onChange={onChange}
        onBlur={onBlur}
      />
    </div>
  );
}

function GovUKFieldset({ legend, legendSize = 'l', children }: GovUKFieldsetProps): React.ReactElement {
  return (
    <fieldset className="govuk-fieldset">
      <legend className={`govuk-fieldset__legend govuk-fieldset__legend--${legendSize}`}>
        <h1 className="govuk-fieldset__heading">{legend}</h1>
      </legend>
      {children}
    </fieldset>
  );
}

/* =======================
   Attach Sub-components
======================= */

GovUKFieldset.Input = GovUKFieldsetInput;
GovUKFieldset.Checkbox = GovUKFieldsetCheckbox;
GovUKFieldset.Select = GovUKFieldsetSelect;
GovUKFieldset.RadioGroup = GovUKFieldsetRadioGroup;
GovUKFieldset.CheckboxGroup = GovUKFieldsetCheckboxGroup;
GovUKFieldset.Textarea = GovUKFieldsetTextarea;

export default GovUKFieldset;
export type { RadioOption, CheckboxOption };
