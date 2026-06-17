import React, { useState, useMemo } from 'react';
import { RowData } from './types';
import { SiteFormQuestionDto } from '../../../SiteForms/types';
import { getTotalErrorCount, getRowsWithErrors, getCellError } from './validation';
import CellEditor from './CellEditor';
import './styles.css';

interface StepCheckDataProps {
  headers: string[];
  labels: string[];
  rows: RowData[];
  questions: SiteFormQuestionDto[];
  onCellEdit: (rowIndex: number, questionCode: string, value: string) => void;
  onOpenFullScreen: () => void;
  onBack: () => void;
  onContinue: () => void;
}

const StepCheckData: React.FC<StepCheckDataProps> = ({
  headers,
  labels,
  rows,
  questions,
  onCellEdit,
  onOpenFullScreen,
  onContinue,
}) => {
  const [showErrorsOnly, setShowErrorsOnly] = useState(false);
  const [editingCell, setEditingCell] = useState<{
    rowIndex: number;
    header: string;
  } | null>(null);

  const totalErrors = useMemo(() => getTotalErrorCount(rows), [rows]);
  const rowsWithErrors = useMemo(() => getRowsWithErrors(rows), [rows]);

  const displayedRows = showErrorsOnly ? rowsWithErrors : rows;

  const getQuestionByCode = (code: string): SiteFormQuestionDto | undefined => {
    return questions.find((q) => q.code === code);
  };

  const handleCellClick = (rowIndex: number, header: string) => {
    setEditingCell({ rowIndex, header });
  };

  const handleCellSave = (value: string) => {
    if (editingCell) {
      onCellEdit(editingCell.rowIndex, editingCell.header, value);
      setEditingCell(null);
    }
  };

  const handleCellCancel = () => {
    setEditingCell(null);
  };

  const canContinue = totalErrors === 0;

  return (
    <div>
      {/* Error summary - shown only when there are errors */}
      {totalErrors > 0 && (
        <div className="bulk-upload-error-summary" role="alert">
          <h2 className="bulk-upload-error-summary__title">There is a problem</h2>
          <p className="bulk-upload-error-summary__body">
            {totalErrors} cell{totalErrors !== 1 ? 's' : ''} of the uploaded file contain
            {totalErrors === 1 ? 's' : ''} data validation errors
          </p>
        </div>
      )}

      {/* Step caption and heading */}
      <div className="bulk-upload-content">
        <p className="bulk-upload-caption">Step 3 of 4</p>
        <h1 className="bulk-upload-heading">Check your uploaded data</h1>
      </div>

      {/* Description */}
      <div className="bulk-upload-content bulk-upload-section">
        <p className="bulk-upload-body">
          We have checked your uploaded file for errors. If we found any problems, the affected
          cells are highlighted in the table. Select a highlighted cell to see what you need to
          change.
        </p>
        <p className="bulk-upload-body">
          You must review and fix any errors before you can continue. You can either:
        </p>
        <ul style={{ fontSize: '19px', lineHeight: '25px', marginBottom: '15px' }}>
          <li>edit the cells directly in the table below, or</li>
          <li>update your file and upload it again.</li>
        </ul>
      </div>

      {/* Table section */}
      <div className="bulk-upload-table-section">
        <div className="bulk-upload-table-header">
          <label className="bulk-upload-checkbox-small">
            <input
              type="checkbox"
              checked={showErrorsOnly}
              onChange={(e) => setShowErrorsOnly(e.target.checked)}
              disabled={totalErrors === 0}
              className="bulk-upload-checkbox-small__input"
            />
            <span className="bulk-upload-checkbox-small__label">Show only rows with errors</span>
          </label>
          <button
            type="button"
            className="bulk-upload-button bulk-upload-button--secondary"
            onClick={onOpenFullScreen}
          >
            Open full screen
          </button>
        </div>

        <div className="bulk-upload-table__wrapper">
          <table className="bulk-upload-table">
            <thead>
              <tr>
                <th className="bulk-upload-table__row-number">#</th>
                {headers.map((header, index) => (
                  <th key={header}>
                    <span className="bulk-upload-table__header-code">{header}</span>
                    {labels[index] || 'Header'}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {displayedRows.slice(0, 15).map((row) => (
                <tr key={row.rowIndex}>
                  <td className="bulk-upload-table__row-number">{row.rowIndex + 1}</td>
                  {headers.map((header) => {
                    const cellError = getCellError(row, header);
                    const isEditing =
                      editingCell?.rowIndex === row.rowIndex && editingCell?.header === header;
                    const cellValue = row.data[header] || '';
                    const question = getQuestionByCode(header);

                    if (isEditing) {
                      return (
                        <td
                          key={`${row.rowIndex}-${header}`}
                          className="bulk-upload-table__cell--editing"
                        >
                          <CellEditor
                            value={cellValue}
                            question={question}
                            error={cellError}
                            onSave={handleCellSave}
                            onCancel={handleCellCancel}
                          />
                        </td>
                      );
                    }

                    return (
                      <td
                        key={`${row.rowIndex}-${header}`}
                        className={`bulk-upload-table__cell--editable${cellError ? ' bulk-upload-table__cell--error' : ''}`}
                        onClick={() => handleCellClick(row.rowIndex, header)}
                        title={cellError?.message}
                      >
                        {cellValue || (cellError ? '(empty)' : '')}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {displayedRows.length > 15 && (
          <p className="bulk-upload-body-small" style={{ marginTop: '10px' }}>
            Showing 15 of {displayedRows.length} rows. Open full screen to see all data.
          </p>
        )}
      </div>

      {/* Continue button */}
      <div>
        <button
          type="button"
          className="bulk-upload-button"
          onClick={onContinue}
          disabled={!canContinue}
        >
          Continue
        </button>
      </div>
    </div>
  );
};

export default StepCheckData;
