import React from 'react';
import { GovUKButton } from '../../../../components/GovUKComponents';
import { ParsedData, CellError } from './types';
import { BulkUploadValidationResultDto } from '../queries';
import { isFieldConditionallyRequired, isFieldValueInvalid, shouldValidateField } from './validation';
import CellEditor from './CellEditor';
import './styles.css';

interface StepCheckDataProps {
  parsedData: ParsedData;
  cellErrors: CellError[];
  showErrorsOnly: boolean;
  editingCell: { rowIndex: number; cellIndex: number } | null;
  validationResult: BulkUploadValidationResultDto | null;
  onToggleShowErrors: (show: boolean) => void;
  onCellEdit: (rowIndex: number, cellIndex: number, value: string) => void;
  onStartEditing: (rowIndex: number, cellIndex: number) => void;
  onCancelEditing: () => void;
  onContinue: () => void;
  onRevalidate: () => void;
  onStartOver: () => void;
  onFullScreen: () => void;
}

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

const StepCheckData: React.FC<StepCheckDataProps> = ({
  parsedData,
  cellErrors,
  showErrorsOnly,
  editingCell,
  validationResult,
  onToggleShowErrors,
  onCellEdit,
  onStartEditing,
  onCancelEditing,
  onContinue,
  onRevalidate,
  onStartOver,
  onFullScreen,
}) => {
  // Calculate all validation errors including required field errors
  const allErrors = React.useMemo(() => {
    const errors: Array<{ rowIndex: number; cellIndex: number; message: string }> = [...cellErrors];
    
    // Check each cell for required field errors that aren't in cellErrors
    parsedData.rows.forEach((row, rowIdx) => {
      parsedData.headers.forEach((fieldCode, cellIdx) => {
        const cell = row[cellIdx];
        const fieldMeta = validationResult?.fieldMetadata?.find(
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
          parsedData.headers,
          validationResult?.fieldMetadata
        );
        
        if (!shouldValidate) return;
        
        // Check backward validation (value exists when it shouldn't)
        const backwardValidation = isFieldValueInvalid(
          fieldCode,
          cell,
          row,
          parsedData.headers,
          validationResult?.fieldMetadata,
          parsedData.labels
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
          parsedData.headers,
          validationResult?.fieldMetadata
        );
        
        // Check top-level required fields
        const isServiceColumn = fieldCode === 'ServiceName' || fieldCode === 'PPS01';
        const isTopLevelRequired = fieldMeta?.isRequired && !fieldMeta?.conditionalRules;
        const isFieldRequired = isConditionallyRequired || isTopLevelRequired;
        const isEmpty = !cell?.trim();
        
        if (isFieldRequired && isEmpty && !isServiceColumn) {
          const fieldLabel = parsedData.labels[cellIdx] || fieldCode;
          errors.push({
            rowIndex: rowIdx,
            cellIndex: cellIdx,
            message: `${fieldLabel} is required`
          });
        }
      });
    });
    
    return errors;
  }, [parsedData, cellErrors, validationResult]);
  
  const hasErrors = allErrors.length > 0;
  const errorCount = allErrors.length;

  return (
    <div className="bulk-upload-step">
      {/* Error summary - shown only when there are errors */}
      {hasErrors && (
        <div className="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
          <h2 className="govuk-error-summary__title" id="error-summary-title">There is a problem</h2>
          <div className="govuk-error-summary__body">
            <p className="govuk-body">
              {errorCount} cell{errorCount !== 1 ? 's' : ''} of the uploaded file contain
              {errorCount === 1 ? 's' : ''} data validation errors
            </p>
          </div>
        </div>
      )}

      {/* Step caption and heading */}
      <div className="govuk-!-width-two-thirds">
        <span className="govuk-caption-l">Step 3 of 5</span>
        <h1 className="govuk-heading-l">Check your uploaded data</h1>
      </div>

      {/* Description */}
      <div className="govuk-!-width-two-thirds govuk-!-margin-bottom-6">
        <p className="govuk-body">
          We have checked your uploaded file for errors. If we found any problems, the affected
          cells are highlighted in the table. Select a highlighted cell to see what you need to
          change.
        </p>
        <p className="govuk-body">
          You must review and fix any errors before you can continue. You can either:
        </p>
        <ul className="govuk-list govuk-list--bullet">
          <li>edit the cells directly in the table below, or</li>
          <li>update your file and upload it again.</li>
        </ul>
      </div>

      {/* Table section */}
      <div className="govuk-!-margin-bottom-6">
        <div className="bulk-upload-table-header">
          <div className="govuk-checkboxes govuk-checkboxes--small">
            <div className="govuk-checkboxes__item">
              <input
                type="checkbox"
                id="show-errors-only"
                checked={showErrorsOnly}
                onChange={(e) => onToggleShowErrors(e.target.checked)}
                disabled={!hasErrors}
                className="govuk-checkboxes__input"
              />
              <label className="govuk-label govuk-checkboxes__label" htmlFor="show-errors-only">
                Show only rows with errors
              </label>
            </div>
          </div>
          <GovUKButton
            className="govuk-button govuk-button--secondary"
            onClick={onFullScreen}
          >
            Open full screen
          </GovUKButton>
        </div>

        <div className="bulk-upload-table__wrapper">
          <table className="bulk-upload-table">
            <thead>
              <tr>
                <th className="bulk-upload-table__row-number">#</th>
                {parsedData.headers.map((header, idx) => (
                  <th key={idx} className={parsedData.requiredFields[idx] ? 'bulk-upload-table__header--required' : ''}>
                    <span className="bulk-upload-table__header-code">{header}</span>
                    {parsedData.labels[idx] && (
                      <span>{parsedData.labels[idx]}</span>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {(showErrorsOnly
                ? parsedData.rows
                    .map((row, idx) => ({ row, idx }))
                    .filter(({ idx }) => allErrors.some((e) => e.rowIndex === idx))
                : parsedData.rows.map((row, idx) => ({ row, idx }))
              )
                .slice(0, 15)
                .map(({ row, idx: rowIdx }) => (
                  <tr key={rowIdx}>
                    <td className="bulk-upload-table__row-number">{rowIdx + 1}</td>
                    {row.map((cell, cellIdx) => {
                      const cellError = allErrors.find(
                        (e) => e.rowIndex === rowIdx && e.cellIndex === cellIdx
                      );
                      const isEditing =
                        editingCell?.rowIndex === rowIdx && editingCell?.cellIndex === cellIdx;

                      const fieldCode = parsedData.headers[cellIdx];
                      const fieldMeta = validationResult?.fieldMetadata?.find(
                        (f) => f.fieldCode === fieldCode
                      );

                      // Progressive validation - only validate if preceding required fields are answered
                      const shouldValidate = shouldValidateField(
                        fieldCode,
                        row,
                        parsedData.headers,
                        validationResult?.fieldMetadata
                      );

                      // Check if this field is conditionally required based on parent field values
                      const isConditionallyRequired = isFieldConditionallyRequired(
                        fieldCode,
                        row,
                        parsedData.headers,
                        validationResult?.fieldMetadata
                      );

                      // Check backward validation - field has value when parent condition is NOT met
                      // Always check backward validation regardless of progressive validation
                      const backwardValidation = isFieldValueInvalid(
                        fieldCode,
                        cell,
                        row,
                        parsedData.headers,
                        validationResult?.fieldMetadata,
                        parsedData.labels
                      );

                      // Service columns (ServiceName, PPS01) are always prefilled, so don't show required styling
                      const isServiceColumn = fieldCode === 'ServiceName' || fieldCode === 'PPS01';

                      // Check if field is a top-level required field (no conditional rules)
                      const isTopLevelRequired = fieldMeta?.isRequired && !fieldMeta?.conditionalRules;

                      // Show as required only if:
                      // 1. Field should be validated (progressive validation passed)
                      // 2. Field is required (either conditionally or top-level)
                      // 3. Cell is empty
                      // 4. Not a service column
                      const isFieldRequired = isConditionallyRequired || isTopLevelRequired;
                      const showAsRequired = shouldValidate && isFieldRequired && !cell?.trim() && !isServiceColumn;

                      // Generate error message for conditionally required empty fields
                      const fieldLabel = parsedData.labels[cellIdx] || fieldCode;
                      const requiredErrorMessage = showAsRequired ? `${fieldLabel} is required` : undefined;

                      // Determine the error to show:
                      // - Backward validation errors (value exists when it shouldn't)
                      // - Backend errors (if field should be validated)
                      // - Required field errors (for empty conditionally required fields)
                      const effectiveError = backwardValidation.isInvalid 
                        ? backwardValidation.errorMessage 
                        : (shouldValidate && cellError?.message ? cellError.message : requiredErrorMessage);
                      const hasError = backwardValidation.isInvalid || (shouldValidate && !!cellError) || showAsRequired;

                      if (isEditing) {
                        return (
                          <td key={cellIdx} className="bulk-upload-table__cell--editing">
                            <CellEditor
                              value={cell}
                              error={effectiveError}
                              fieldType={fieldMeta?.fieldType}
                              fieldLabel={fieldLabel}
                              options={fieldMeta?.options}
                              isConditionallyRequired={showAsRequired}
                              onSave={(value) => onCellEdit(rowIdx, cellIdx, value)}
                              onCancel={onCancelEditing}
                            />
                          </td>
                        );
                      }

                      // Build cell class names
                      let cellClassName = 'bulk-upload-table__cell--editable';
                      if (hasError) {
                        cellClassName += ' bulk-upload-table__cell--error';
                      } else if (showAsRequired) {
                        cellClassName += ' bulk-upload-table__cell--conditional-required';
                      }

                      // Convert system IDs to display labels for selection fields
                      const displayValue = getDisplayValue(cell, fieldMeta?.fieldType, fieldMeta?.options);
 
                      return (
                        <td
                          key={cellIdx}
                          className={cellClassName}
                          onClick={() => onStartEditing(rowIdx, cellIdx)}
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
        {parsedData.rows.length > 15 && (
          <p className="govuk-body-s govuk-!-margin-top-2">
            Showing 15 of {parsedData.rows.length} rows. Open full screen to see all data.
          </p>
        )}
      </div>

      {/* Buttons */}
      <div className="govuk-button-group">
        {hasErrors ? (
          <GovUKButton onClick={onRevalidate}>
            Revalidate
          </GovUKButton>
        ) : (
          <GovUKButton onClick={onContinue}>
            Continue
          </GovUKButton>
        )}
        <GovUKButton
          className="govuk-button govuk-button--secondary"
          onClick={onStartOver}
        >
          Upload different file
        </GovUKButton>
      </div>
    </div>
  );
};

export default StepCheckData;
