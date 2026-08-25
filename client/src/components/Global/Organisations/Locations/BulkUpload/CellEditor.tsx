import React, { useState, useRef, useEffect, useId } from 'react';
import { GovUKAutocomplete } from '../../../../GovUKComponents';
import { SiteFormQuestionDto, SiteFormQuestionType } from '../../../SiteForms/types';
import { CellError } from './types';
import './styles.css';

interface CellEditorProps {
  value: string;
  question: SiteFormQuestionDto | undefined;
  error: CellError | undefined;
  onSave: (value: string) => void;
  onCancel: () => void;
}

const CellEditor: React.FC<CellEditorProps> = ({
  value,
  question,
  error,
  onSave,
  onCancel,
}) => {
  const [editValue, setEditValue] = useState(value);

  // For checkbox fields, filter out invalid values - only keep values that match valid options
  const getValidCheckboxValues = (rawValue: string, questionOptions: SiteFormQuestionDto['options'] | undefined): string[] => {
    if (!rawValue) return [];
    const values = rawValue.split(',').map((v) => v.trim()).filter(Boolean);
    if (!questionOptions || questionOptions.length === 0) return values;

    // Only keep values that match a valid option (by value or label, case-insensitive)
    return values.filter((v) =>
      questionOptions.some(
        (opt) => opt.value.toLowerCase() === v.toLowerCase() || opt.label.toLowerCase() === v.toLowerCase()
      )
    );
  };

  const [checkboxValues, setCheckboxValues] = useState<string[]>(
    getValidCheckboxValues(value, question?.options)
  );
  const inputRef = useRef<HTMLInputElement>(null);
  const errorId = useId();
  const fieldId = useId();

  useEffect(() => {
    // Dropdown fields focus themselves via GovUKAutocomplete's autoFocus.
    if (inputRef.current) {
      inputRef.current.focus();
    }
  }, []);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSave();
    } else if (e.key === 'Escape') {
      onCancel();
    }
  };

  const handleSave = () => {
    if (question?.questionType === SiteFormQuestionType.Checkbox) {
      onSave(checkboxValues.join(','));
    } else {
      onSave(editValue);
    }
  };

  const handleCheckboxChange = (optionValue: string, checked: boolean) => {
    const updated = checked
      ? [...checkboxValues, optionValue]
      : checkboxValues.filter((v) => v !== optionValue);
    setCheckboxValues(updated);
  };

  const renderEditor = () => {
    if (!question) {
      return (
        <input
          ref={inputRef}
          type="text"
          value={editValue}
          onChange={(e) => setEditValue(e.target.value)}
          onKeyDown={handleKeyDown}
          className="cell-editor__input"
          aria-label="Edit cell value"
          aria-invalid={!!error}
          aria-describedby={error ? errorId : undefined}
        />
      );
    }

    switch (question.questionType) {
      case SiteFormQuestionType.Radio:
      case SiteFormQuestionType.Select:
        return (
          <GovUKAutocomplete
            inline
            autoFocus
            showAllValues
            autoselect
            id={fieldId}
            name={question.code || question.label}
            label={question.label}
            options={question.options}
            value={editValue}
            onChange={setEditValue}
            onKeyDown={handleKeyDown}
            invalid={!!error}
            describedBy={error ? errorId : undefined}
          />
        );

      case SiteFormQuestionType.Checkbox:
        return (
          <div className="cell-editor__checkbox-list">
            {question.options.map((option) => (
              <label key={option.value} className="cell-editor__checkbox-item">
                <input
                  type="checkbox"
                  checked={checkboxValues.includes(option.value)}
                  onChange={(e) => handleCheckboxChange(option.value, e.target.checked)}
                  className="cell-editor__checkbox-input"
                  // Native checkboxes only respond to Space. Inside a cell editor that leaves
                  // Enter as a dead key, so keyboard users cannot correct a flagged row.
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault();
                      handleCheckboxChange(option.value, !checkboxValues.includes(option.value));
                    } else if (e.key === 'Escape') {
                      onCancel();
                    }
                  }}
                />
                <span className="cell-editor__checkbox-label">{option.label}</span>
              </label>
            ))}
          </div>
        );

      case SiteFormQuestionType.Date:
        return (
          <input
            ref={inputRef}
            type="date"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onKeyDown={handleKeyDown}
            className="cell-editor__input"
            aria-label={question.label}
            aria-invalid={!!error}
            aria-describedby={error ? errorId : undefined}
          />
        );

      case SiteFormQuestionType.Text:
      default:
        return (
          <input
            ref={inputRef}
            type="text"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onKeyDown={handleKeyDown}
            className="cell-editor__input"
            aria-label={question.label}
            aria-invalid={!!error}
            aria-describedby={error ? errorId : undefined}
          />
        );
    }
  };

  return (
    <div className="cell-editor">
      {error && (
        <div id={errorId} role="alert" className="cell-editor__error-message">
          {error.message}
        </div>
      )}
      <div className="cell-editor__content">
        {renderEditor()}
        <div className="cell-editor__actions">
          <button
            type="button"
            className="cell-editor__btn cell-editor__btn--save"
            onClick={handleSave}
            aria-label="Save"
          >
            ✓
          </button>
          <button
            type="button"
            className="cell-editor__btn cell-editor__btn--cancel"
            onClick={onCancel}
            aria-label="Cancel edit"
          >
            ✕
          </button>
        </div>
      </div>
    </div>
  );
};

export default CellEditor;
