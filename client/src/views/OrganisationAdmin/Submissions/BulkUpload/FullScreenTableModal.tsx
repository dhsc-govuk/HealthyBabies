import React, { useState, useMemo } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import CellEditor from './CellEditor';
import { isFieldConditionallyRequired, isFieldValueInvalid, shouldValidateField } from './validation';
import './styles.css';

// Helper function to convert system IDs to display labels for selection fields
const getDisplayValue = (
  value: string,
  fieldType?: string,
  options?: { value: string; label: string }[]
): string => {
  if (!value || !options || options.length === 0) return value;
  
  const normalizedFieldType = fieldType?.toLowerCase();
  const isSelectionField = normalizedFieldType === 'radio' || normalizedFieldType === 'select' || normalizedFieldType === 'checkbox';
  
  if (!isSelectionField) return value;
  
  // For checkbox/multi-select fields, value may be comma-separated
  if (normalizedFieldType === 'checkbox') {
    const values = value.split(',').map(v => v.trim()).filter(v => v);
    const labels = values.map(v => {
      const option = options.find(opt => 
        opt.value.toLowerCase() === v.toLowerCase() || 
        opt.label.toLowerCase() === v.toLowerCase()
      );
      return option?.label || v;
    });
    return labels.join(', ');
  }
  
  // For single-select fields (radio/select)
  const option = options.find(opt => 
    opt.value.toLowerCase() === value.toLowerCase() || 
    opt.label.toLowerCase() === value.toLowerCase()
  );
  return option?.label || value;
};

interface CellError {
  rowIndex: number;
  cellIndex: number;
  fieldCode: string;
  message: string;
}

interface FieldOption {
  value: string;
  label: string;
}

interface FieldMetadata {
  fieldCode: string;
  fieldType: string;
  isRequired: boolean;
  options: FieldOption[];
  conditionalRules: string | null;
}

interface FullScreenTableModalProps {
  open: boolean;
  title: string;
  headers: string[];
  labels: string[];
  requiredFields?: boolean[];
  rows: string[][];
  editable?: boolean;
  cellErrors?: CellError[];
  fieldMetadata?: FieldMetadata[];
  onCellEdit?: (rowIndex: number, cellIndex: number, value: string) => void;
  onClose: () => void;
}

const FullScreenTableModal: React.FC<FullScreenTableModalProps> = ({
  open,
  title,
  headers,
  labels,
  requiredFields = [],
  rows,
  editable = false,
  cellErrors = [],
  fieldMetadata = [],
  onCellEdit,
  onClose,
}) => {
  const [editingCell, setEditingCell] = useState<{ rowIndex: number; cellIndex: number } | null>(null);

  // Calculate all validation errors including required field errors
  const allErrors = useMemo(() => {
    const errors: Array<{ rowIndex: number; cellIndex: number; message: string }> = [...cellErrors];
    
    // Check each cell for required field errors that aren't in cellErrors
    rows.forEach((row, rowIdx) => {
      headers.forEach((fieldCode, cellIdx) => {
        const cell = row[cellIdx];
        const fieldMeta = fieldMetadata.find(
          (f) => f.fieldCode === fieldCode
        );
        
        // Skip if already has an error from cellErrors
        if (cellErrors.some(e => e.rowIndex === rowIdx && e.cellIndex === cellIdx)) {
          return;
        }
        
        // Progressive validation check
        const shouldValidate = shouldValidateField(
          fieldCode,
          row,
          headers,
          fieldMetadata
        );
        
        if (!shouldValidate) return;
        
        // Check backward validation (value exists when it shouldn't)
        const backwardValidation = isFieldValueInvalid(
          fieldCode,
          cell,
          row,
          headers,
          fieldMetadata,
          labels
        );
        
        if (backwardValidation.isInvalid) {
          errors.push({
            rowIndex: rowIdx,
            cellIndex: cellIdx,
            message: backwardValidation.errorMessage || 'Invalid value'
          });
          return;
        }
        
        // Check conditionally required fields
        const isConditionallyRequired = isFieldConditionallyRequired(
          fieldCode,
          row,
          headers,
          fieldMetadata
        );
        
        // Check top-level required fields
        const isServiceColumn = fieldCode === 'ServiceName' || fieldCode === 'PPS01';
        const isTopLevelRequired = fieldMeta?.isRequired && !fieldMeta?.conditionalRules;
        const isFieldRequired = isConditionallyRequired || isTopLevelRequired;
        const isEmpty = !cell?.trim();
        
        if (isFieldRequired && isEmpty && !isServiceColumn) {
          const fieldLabel = labels[cellIdx] || fieldCode;
          errors.push({
            rowIndex: rowIdx,
            cellIndex: cellIdx,
            message: `${fieldLabel} is required`
          });
        }
      });
    });
    
    return errors;
  }, [rows, headers, labels, cellErrors, fieldMetadata]);

  const getCellError = (rowIndex: number, cellIndex: number): CellError | undefined => {
    return allErrors.find((e) => e.rowIndex === rowIndex && e.cellIndex === cellIndex) as CellError | undefined;
  };

  const handleCellClick = (rowIndex: number, cellIndex: number) => {
    if (!editable) return;
    setEditingCell({ rowIndex, cellIndex });
  };

  const handleCellSave = (value: string) => {
    if (editingCell && onCellEdit) {
      onCellEdit(editingCell.rowIndex, editingCell.cellIndex, value);
      setEditingCell(null);
    }
  };

  const handleCellCancel = () => {
    setEditingCell(null);
  };

  return (
    <Dialog open={open} onClose={onClose} fullScreen>
      <DialogTitle
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          borderBottom: '1px solid #b1b4b6',
          padding: '15px 20px',
        }}
      >
        <span style={{ fontSize: '24px', fontWeight: 700, color: '#0b0c0c' }}>{title}</span>
        <IconButton onClick={onClose} aria-label="Close">
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent sx={{ padding: 0 }}>
        <div style={{ overflow: 'auto', height: '100%' }}>
          <table className="bulk-upload-table" style={{ minWidth: '100%' }}>
            <thead>
              <tr>
                <th className="bulk-upload-table__row-number" style={{ position: 'sticky', left: 0 }}>
                  #
                </th>
                {headers.map((header, index) => (
                  <th key={header} style={{ minWidth: '180px' }} className={requiredFields[index] ? 'bulk-upload-table__header--required' : ''}>
                    <span className="bulk-upload-table__header-code">{header}</span>
                    {labels[index] || ''}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, rowIndex) => (
                <tr key={rowIndex}>
                  <td
                    className="bulk-upload-table__row-number"
                    style={{ position: 'sticky', left: 0 }}
                  >
                    {rowIndex + 1}
                  </td>
                  {row.map((cell, cellIndex) => {
                    const cellError = getCellError(rowIndex, cellIndex);
                    const isEditing =
                      editingCell?.rowIndex === rowIndex && editingCell?.cellIndex === cellIndex;

                    // Get field metadata for this column (if available)
                    const fieldCode = headers[cellIndex];
                    const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);

                    // Progressive validation - only validate if preceding required fields are answered
                    const shouldValidate = shouldValidateField(
                      fieldCode,
                      row,
                      headers,
                      fieldMetadata
                    );

                    // Check if this field is conditionally required based on parent field values
                    const isConditionallyReq = isFieldConditionallyRequired(
                      fieldCode,
                      row,
                      headers,
                      fieldMetadata
                    );

                    // Check backward validation - field has value when parent condition is NOT met
                    const backwardValidation = isFieldValueInvalid(
                      fieldCode,
                      cell,
                      row,
                      headers,
                      fieldMetadata,
                      labels
                    );

                    // Service columns (ServiceName, PPS01) are always prefilled, so don't show required styling
                    const isServiceColumn = fieldCode === 'ServiceName' || fieldCode === 'PPS01';

                    // Check if field is a top-level required field (no conditional rules)
                    const isTopLevelRequired = fieldMeta?.isRequired && !fieldMeta?.conditionalRules;

                    // Show as required only if progressive validation passed and field is required
                    const isFieldRequired = isConditionallyReq || isTopLevelRequired;
                    const showAsRequired = shouldValidate && isFieldRequired && !cell?.trim() && !isServiceColumn;

                    // Generate error message for conditionally required empty fields
                    const fieldLabel = labels[cellIndex] || fieldCode;
                    const requiredErrorMessage = showAsRequired ? `${fieldLabel} is required` : undefined;

                    // Determine the error to show - backward validation or backend error (with progressive check)
                    const effectiveError = backwardValidation.isInvalid 
                      ? backwardValidation.errorMessage 
                      : (shouldValidate && cellError?.message ? cellError.message : requiredErrorMessage);
                    const hasError = backwardValidation.isInvalid || (shouldValidate && !!cellError) || showAsRequired;

                    if (isEditing) {
                      return (
                        <td key={cellIndex} className="bulk-upload-table__cell--editing">
                          <CellEditor
                            value={cell}
                            error={effectiveError}
                            fieldType={fieldMeta?.fieldType}
                            fieldLabel={fieldLabel}
                            options={fieldMeta?.options}
                            isConditionallyRequired={showAsRequired}
                            onSave={handleCellSave}
                            onCancel={handleCellCancel}
                          />
                        </td>
                      );
                    }

                    // Build cell class names
                    let cellClassName = editable ? 'bulk-upload-table__cell--editable' : '';
                    if (hasError) {
                      cellClassName += ' bulk-upload-table__cell--error';
                    } else if (showAsRequired) {
                      cellClassName += ' bulk-upload-table__cell--conditional-required';
                    }

                    // Convert system IDs to display labels for selection fields
                    const displayValue = getDisplayValue(cell, fieldMeta?.fieldType, fieldMeta?.options);

                    return (
                      <td
                        key={cellIndex}
                        className={cellClassName}
                        onClick={() => handleCellClick(rowIndex, cellIndex)}
                        title={effectiveError || (showAsRequired ? 'This field is required when parent field is selected' : undefined)}
                      >
                        {displayValue || ''}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </DialogContent>

      <DialogActions sx={{ padding: '15px 20px', borderTop: '1px solid #b1b4b6' }}>
        <button type="button" className="bulk-upload-button" onClick={onClose}>
          Close
        </button>
      </DialogActions>
    </Dialog>
  );
};

export default FullScreenTableModal;
