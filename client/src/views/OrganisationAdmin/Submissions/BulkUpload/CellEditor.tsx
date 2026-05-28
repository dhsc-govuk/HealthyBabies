import React, { useState, useRef, useEffect } from 'react';
import './styles.css';

interface FieldOption {
  value: string;
  label: string;
}

interface CellEditorProps {
  value: string;
  error?: string;
  fieldType?: string;
  fieldLabel?: string;
  options?: FieldOption[];
  isConditionallyRequired?: boolean;
  onSave: (value: string) => void;
  onCancel: () => void;
}

const CellEditor: React.FC<CellEditorProps> = ({
  value,
  error,
  fieldType,
  fieldLabel,
  options,
  isConditionallyRequired,
  onSave,
  onCancel,
}) => {
  const [editValue, setEditValue] = useState(value);
  const inputRef = useRef<HTMLInputElement>(null);
  const selectRef = useRef<HTMLSelectElement>(null);

  // Normalize field type to lowercase for comparison
  const normalizedFieldType = fieldType?.toLowerCase();
  
  // Single-select dropdown fields: radio or select with options
  const isDropdownField = (normalizedFieldType === 'radio' || normalizedFieldType === 'select') && options && options.length > 0;
  
  // Multi-select checkbox fields
  const isCheckboxField = normalizedFieldType === 'checkbox' && options && options.length > 0;

  useEffect(() => {
    if (isDropdownField && selectRef.current) {
      selectRef.current.focus();
    } else if (inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select();
    }
  }, [isDropdownField]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      onSave(editValue);
    } else if (e.key === 'Escape') {
      onCancel();
    }
  };

  const handleSelectChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newValue = e.target.value;
    setEditValue(newValue);
    // Auto-save on selection for dropdown fields
    if (newValue) {
      onSave(newValue);
    }
  };

  // Check if a value matches an option (by value or label, case-insensitive)
  const isOptionSelected = (optionValue: string, optionLabel: string, currentValues: string[]): boolean => {
    return currentValues.some(v => 
      v.toLowerCase() === optionValue.toLowerCase() || 
      v.toLowerCase() === optionLabel.toLowerCase()
    );
  };

  // Handle checkbox multi-select toggle
  const handleCheckboxToggle = (optionValue: string, optionLabel: string) => {
    const currentValues = editValue ? editValue.split(',').map(v => v.trim()).filter(v => v) : [];
    const isSelected = isOptionSelected(optionValue, optionLabel, currentValues);
    
    let newValues: string[];
    if (isSelected) {
      // Remove by matching value or label
      newValues = currentValues.filter(v => 
        v.toLowerCase() !== optionValue.toLowerCase() && 
        v.toLowerCase() !== optionLabel.toLowerCase()
      );
    } else {
      // Add the value (not label) for consistency
      newValues = [...currentValues, optionValue];
    }
    
    setEditValue(newValues.join(','));
  };

  // Render checkbox multi-select
  const renderCheckboxField = () => {
    const currentValues = editValue ? editValue.split(',').map(v => v.trim()).filter(v => v) : [];
    
    return (
      <div className="cell-editor__checkbox-list">
        {options?.map((opt) => (
          <label key={opt.value} className="cell-editor__checkbox-item">
            <input
              type="checkbox"
              checked={isOptionSelected(opt.value, opt.label, currentValues)}
              onChange={() => handleCheckboxToggle(opt.value, opt.label)}
            />
            <span>{opt.label}</span>
          </label>
        ))}
      </div>
    );
  };

  // Find the matching option value for the current edit value (match by value or label)
  const getMatchedOptionValue = (): string => {
    if (!options || !editValue) return '';
    const normalizedValue = editValue.toLowerCase().trim();
    const match = options.find(
      opt => opt.value.toLowerCase() === normalizedValue || opt.label.toLowerCase() === normalizedValue
    );
    return match?.value || editValue;
  };

  // Render the appropriate input based on field type
  const renderInput = () => {
    if (isDropdownField) {
      const matchedValue = getMatchedOptionValue();
      return (
        <select
          ref={selectRef}
          value={matchedValue}
          onChange={handleSelectChange}
          onKeyDown={handleKeyDown}
          className="cell-editor__select"
          aria-label={fieldLabel || 'Select an option'}
        >
          <option value="">-- Select --</option>
          {options?.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      );
    }
    
    if (isCheckboxField) {
      return renderCheckboxField();
    }
    
    return (
      <input
        ref={inputRef}
        type="text"
        value={editValue}
        onChange={(e) => setEditValue(e.target.value)}
        onKeyDown={handleKeyDown}
        className={`cell-editor__input${isConditionallyRequired ? ' cell-editor__input--required' : ''}`}
        aria-label={fieldLabel || 'Edit cell value'}
      />
    );
  };

  return (
    <div className={`cell-editor${isConditionallyRequired ? ' cell-editor--required' : ''}`}>
      {error && (
        <div className="cell-editor__error-message">
          {error}
        </div>
      )}
      <div className="cell-editor__content">
        {renderInput()}
        <div className="cell-editor__actions">
          <button
            type="button"
            className="cell-editor__btn cell-editor__btn--save"
            onClick={() => onSave(editValue)}
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
