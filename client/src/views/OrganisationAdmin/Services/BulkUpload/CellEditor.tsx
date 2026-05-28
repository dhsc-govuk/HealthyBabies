import React, { useState, useRef, useEffect } from 'react';
import { ServiceFormQuestionDto, ServiceFormQuestionType } from '../../../../components/Global/Services';
import { CellError } from './types';
import { findOptionMatch } from './validation';
import './styles.css';

interface CellEditorProps {
  value: string;
  question: ServiceFormQuestionDto | undefined;
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
  // For single-select fields, translate a label (e.g. "Family Hub North") to its canonical
  // value (e.g. the location ID) so the <select> renders it as the selected option.
  const getInitialSingleValue = (rawValue: string, q: ServiceFormQuestionDto | undefined): string => {
    if (!rawValue || !q || q.options.length === 0) return rawValue;
    return findOptionMatch(q.options, rawValue)?.value ?? rawValue;
  };

  // For checkbox fields, drop unrecognised entries AND translate labels to canonical values
  // so `checkboxValues.includes(option.value)` matches and the box renders pre-checked.
  const getValidCheckboxValues = (rawValue: string, questionOptions: ServiceFormQuestionDto['options'] | undefined): string[] => {
    if (!rawValue) return [];
    const values = rawValue.split(',').map((v) => v.trim()).filter(Boolean);
    if (!questionOptions || questionOptions.length === 0) return values;

    return values
      .map((v) => findOptionMatch(questionOptions, v)?.value)
      .filter((v): v is string => v !== undefined);
  };

  const [editValue, setEditValue] = useState(() => getInitialSingleValue(value, question));
  const [checkboxValues, setCheckboxValues] = useState<string[]>(
    getValidCheckboxValues(value, question?.options)
  );
  const inputRef = useRef<HTMLInputElement>(null);
  const selectRef = useRef<HTMLSelectElement>(null);

  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.focus();
    } else if (selectRef.current) {
      selectRef.current.focus();
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
    if (question?.questionType === ServiceFormQuestionType.Checkbox) {
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
        />
      );
    }

    switch (question.questionType) {
      case ServiceFormQuestionType.Radio:
      case ServiceFormQuestionType.Select:
        return (
          <select
            ref={selectRef}
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onKeyDown={handleKeyDown}
            className="cell-editor__select"
            aria-label={question.label}
          >
            <option value="">-- Select --</option>
            {question.options.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        );

      case ServiceFormQuestionType.Checkbox:
        return (
          <div className="cell-editor__checkbox-list">
            <div className="cell-editor__search-box">
              <input
                type="text"
                placeholder="Type to search..."
                className="cell-editor__search-input"
                aria-label="Search options"
                onChange={(e) => {
                  // Filter is visual only, doesn't affect state
                }}
              />
            </div>
            {question.options.map((option) => (
              <label key={option.value} className="cell-editor__checkbox-item">
                <input
                  type="checkbox"
                  checked={checkboxValues.includes(option.value)}
                  onChange={(e) => handleCheckboxChange(option.value, e.target.checked)}
                  className="cell-editor__checkbox-input"
                />
                <span className="cell-editor__checkbox-label">{option.label}</span>
              </label>
            ))}
          </div>
        );

      case ServiceFormQuestionType.Date:
        return (
          <div className="cell-editor__date-wrapper">
            <input
              ref={inputRef}
              type="date"
              value={editValue}
              onChange={(e) => setEditValue(e.target.value)}
              onKeyDown={handleKeyDown}
              className="cell-editor__input"
              aria-label={question.label}
            />
            {editValue && (
              <button
                type="button"
                className="cell-editor__clear-btn"
                onClick={() => setEditValue('')}
                title="Clear date"
              >
                Clear
              </button>
            )}
          </div>
        );

      case ServiceFormQuestionType.Text:
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
          />
        );
    }
  };

  return (
    <div className="cell-editor">
      {error && (
        <div className="cell-editor__error-message">
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
            title="Save"
          >
            ✓
          </button>
          <button
            type="button"
            className="cell-editor__btn cell-editor__btn--cancel"
            onClick={onCancel}
            title="Cancel"
          >
            ✕
          </button>
        </div>
      </div>
    </div>
  );
};

export default CellEditor;
