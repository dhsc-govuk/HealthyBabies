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
  /** Render without the form-group/label chrome, for use inside a table cell. The label becomes visually hidden. */
  inline?: boolean;
  /** Focus the input once the widget has mounted. */
  autoFocus?: boolean;
  /** Show the full option list (and a dropdown arrow) on focus, so the control still reads as a dropdown. */
  showAllValues?: boolean;
  /** Highlight the first match while typing, so Enter confirms what the user typed. */
  autoselect?: boolean;
  /**
   * Confirm the highlighted option when the input loses focus.
   * Beware combining this with `autoselect` and `showAllValues`: clicking the input runs the
   * source, which highlights the first option, so a plain click-then-click-away would commit
   * an option the user never chose. Left off for that reason.
   */
  confirmOnBlur?: boolean;
  /** Extra class names for the input the library renders. */
  inputClassName?: string;
  /** Marks the input as invalid. Used by `inline`, where there is no error chrome to render. */
  invalid?: boolean;
  /** Id of an element describing the input, e.g. an error message. */
  describedBy?: string;
  /**
   * Bubble-phase key handler on the wrapper. Not called for an Enter that the autocomplete
   * consumed to pick an option, so a host can safely treat Enter as "submit".
   */
  onKeyDown?: (event: React.KeyboardEvent) => void;
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
  inline = false,
  autoFocus = false,
  showAllValues = false,
  autoselect = false,
  confirmOnBlur = false,
  inputClassName,
  invalid = false,
  describedBy,
  onKeyDown,
  onChange,
}: GovUKAutocompleteProps): React.ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);
  const initializedRef = useRef(false);
  // The library owns the input element and exposes no props for aria-invalid/aria-describedby,
  // so we sync them by hand. Remember its own describedby so we append rather than clobber it.
  const libraryDescribedByRef = useRef<string | null>(null);
  const onChangeRef = useRef(onChange);
  const optionsRef = useRef(options);
  const valueRef = useRef(value);
  // Escape is routed through the library's blur handler, which fires onConfirm when
  // confirmOnBlur is on. This lets us swallow that one confirm so Escape never commits.
  const suppressConfirmRef = useRef(false);
  // Set when the library confirms an option during the keydown currently being dispatched,
  // so the wrapper can tell "Enter picked an option" from "Enter did nothing".
  const confirmedDuringKeyRef = useRef(false);
  const invalidRef = useRef(invalid);
  const describedByRef = useRef(describedBy);

  onChangeRef.current = onChange;
  optionsRef.current = options;
  valueRef.current = value;
  invalidRef.current = invalid;
  describedByRef.current = describedBy;

  // Callers pass useId(), which produces ids containing ':' — invalid in CSS selectors.
  const fieldId = id.replace(/:/g, '-');

  const getDefaultValue = useCallback(() => {
    const opt = optionsRef.current.find((o) => o.value === valueRef.current);
    return opt?.label || '';
  }, []);

  const handleConfirm = useCallback((confirmed: string | undefined) => {
    if (suppressConfirmRef.current) {
      suppressConfirmRef.current = false;
      return;
    }

    confirmedDuringKeyRef.current = true;

    if (!confirmed) {
      // onConfirm(undefined) also fires on blur when nothing is highlighted, so it cannot
      // mean "clear" on its own. Only treat it as a clear when the user emptied the input.
      const input = containerRef.current?.querySelector('input');
      if (input && input.value.trim() === '') {
        onChangeRef.current?.('');
      }
      return;
    }

    const matchedOption = optionsRef.current.find((opt) => opt.label === confirmed);
    onChangeRef.current?.(matchedOption?.value || '');
  }, []);

  // Capture runs before the library's own handler, so both flags are set in time.
  const handleKeyDownCapture = useCallback((e: React.KeyboardEvent) => {
    confirmedDuringKeyRef.current = false;

    if (e.key !== 'Escape') return;

    // The library handles Escape (and any onConfirm it triggers) synchronously within this
    // event, so clearing on the next microtask means the flag can never be left stranded
    // and swallow a later, genuine confirm - e.g. when confirmOnBlur is off and Escape
    // therefore never reaches onConfirm at all.
    suppressConfirmRef.current = true;
    queueMicrotask(() => {
      suppressConfirmRef.current = false;
    });
  }, []);

  // The library owns the input and rewrites its aria-describedby on every keystroke
  // (autocomplete.js toggles the assistive hint), so this has to be re-applied on input
  // as well as on render, or the error message is disconnected exactly while typing.
  const syncInputAria = useCallback(() => {
    const input = containerRef.current?.querySelector('input');
    if (!input) return;

    if (libraryDescribedByRef.current === null) {
      libraryDescribedByRef.current = input.getAttribute('aria-describedby') || '';
    }

    const ids = [libraryDescribedByRef.current, describedByRef.current].filter(Boolean).join(' ');
    if (ids) {
      input.setAttribute('aria-describedby', ids);
    } else {
      input.removeAttribute('aria-describedby');
    }

    if (invalidRef.current) {
      input.setAttribute('aria-invalid', 'true');
    } else {
      input.removeAttribute('aria-invalid');
    }
  }, []);

  const handleWrapperKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      // The library preventDefaults Enter whenever its menu is open, even when nothing is
      // highlighted, so defaultPrevented cannot tell us whether it actually picked an
      // option. Only a confirm during this keydown means Enter was consumed.
      if (e.key === 'Enter' && confirmedDuringKeyRef.current) return;
      onKeyDown?.(e);
    },
    [onKeyDown]
  );

  useEffect(() => {
    if (!containerRef.current || initializedRef.current) return;

    const container = containerRef.current;

    accessibleAutocomplete({
      element: container,
      id: fieldId,
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
      showAllValues,
      autoselect,
      confirmOnBlur,
      inputClasses: inputClassName,
      onConfirm: handleConfirm,
    });

    initializedRef.current = true;

    const input = container.querySelector('input');
    const handleInput = () => {
      syncInputAria();
      // With confirmOnBlur off, onConfirm only fires for a deliberate pick, so an emptied
      // field is never reported. Surface it here or a cell can no longer be blanked the
      // way the '-- Select --' option used to allow.
      if (input && input.value.trim() === '' && valueRef.current !== '') {
        onChangeRef.current?.('');
      }
    };
    input?.addEventListener('input', handleInput);

    if (autoFocus) {
      input?.focus();
    }

    return () => {
      if (container) {
        input?.removeEventListener('input', handleInput);
        container.innerHTML = '';
        initializedRef.current = false;
        libraryDescribedByRef.current = null;
      }
    };
  }, [
    syncInputAria,
    fieldId,
    name,
    placeholder,
    showAllValues,
    autoselect,
    confirmOnBlur,
    inputClassName,
    autoFocus,
    handleConfirm,
    getDefaultValue,
  ]);

  // Runs after every render so a validation error appearing mid-edit is still announced.
  useEffect(syncInputAria);

  if (inline) {
    return (
      <div className="govuk-autocomplete-inline" onKeyDownCapture={handleKeyDownCapture} onKeyDown={handleWrapperKeyDown}>
        <label className="govuk-visually-hidden" htmlFor={fieldId}>
          {label}
        </label>
        <div
          ref={containerRef}
          className={`govuk-autocomplete-container govuk-autocomplete-container--inline ${disabled ? 'govuk-autocomplete-container--disabled' : ''}`}
        />
      </div>
    );
  }

  return (
    <div className={`govuk-form-group ${error ? 'govuk-form-group--error' : ''}`} onKeyDownCapture={handleKeyDownCapture} onKeyDown={handleWrapperKeyDown}>
      <label className={`govuk-label govuk-label--${labelSize}`} htmlFor={fieldId}>
        {label}
        {required && <span className="govuk-visually-hidden"> (required)</span>}
      </label>

      {hint && (
        <div className="govuk-hint" id={`${fieldId}-hint`}>
          {hint}
        </div>
      )}

      {error && (
        <p className="govuk-error-message" id={`${fieldId}-error`}>
          <span className="govuk-visually-hidden">Error:</span> {error}
        </p>
      )}

      <div
        ref={containerRef}
        className={`govuk-autocomplete-container ${disabled ? 'govuk-autocomplete-container--disabled' : ''}`}
        aria-describedby={[hint && `${fieldId}-hint`, error && `${fieldId}-error`].filter(Boolean).join(' ') || undefined}
      />
    </div>
  );
}

export default GovUKAutocomplete;
