import React, { useState, useRef, useEffect } from 'react';
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
        />
      );
    }

    switch (question.questionType) {
      case SiteFormQuestionType.Radio:
      case SiteFormQuestionType.Select:
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

      case SiteFormQuestionType.Checkbox:
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
