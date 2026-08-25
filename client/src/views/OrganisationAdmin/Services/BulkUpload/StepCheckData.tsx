import React, { useState, useMemo, useRef } from 'react';
import { RowData, dataRowFileOffset } from './types';
import { ServiceFormQuestionDto, ServiceFormQuestionType } from '../../../../components/Global/Services';
import { getTotalErrorCount, getRowsWithErrors, getCellError } from './validation';
import CellEditor from './CellEditor';
import { focusErrorTargetById } from '../../../../helpers/focusErrorTarget';
import ValidationReportDownloadButton from '../../../../components/Global/BulkUpload/ValidationReportDownloadButton';
import './styles.css';

interface StepCheckDataProps {
  headers: string[];
  labels: string[];
  rows: RowData[];
  questions: ServiceFormQuestionDto[];
  fileName: string;
  onCellEdit: (rowIndex: number, questionCode: string, value: string) => void;
  onOpenFullScreen: () => void;
  onContinue: () => void;
}

const StepCheckData: React.FC<StepCheckDataProps> = ({ headers, labels, rows, questions, fileName, onCellEdit, onOpenFullScreen, onContinue }) => {
  const [showErrorsOnly, setShowErrorsOnly] = useState(false);
  const [editingCell, setEditingCell] = useState<{
    rowIndex: number;
    header: string;
  } | null>(null);
  const activeCellRef = useRef<HTMLButtonElement | null>(null);

  const totalErrors = useMemo(() => getTotalErrorCount(rows), [rows]);
  const rowsWithErrors = useMemo(() => getRowsWithErrors(rows), [rows]);

  const ERROR_LINK_LIMIT = 10;
  // Only rows that will actually be rendered (the table shows the first 15 error rows once
  // the filter is on), so a link can never point at a cell that is not in the DOM.
  const errorLinks = useMemo(
    () =>
      rowsWithErrors
        .slice(0, 15)
        .flatMap((row) => row.errors.map((error) => ({ error, row })))
        // An error for a code absent from the uploaded headers has no rendered cell to
        // target, so exclude it rather than emit a link that goes nowhere.
        .filter(({ error }) => headers.includes(error.questionCode))
        .slice(0, ERROR_LINK_LIMIT)
        .map(({ error, row }) => ({
          targetId: `cell-${row.rowIndex}-${error.questionCode}`,
          text: `CSV row ${row.rowIndex + dataRowFileOffset}, ${labels[headers.indexOf(error.questionCode)] || error.questionCode}: ${error.message}`,
        })),
    [rowsWithErrors, headers, labels]
  );

  const displayedRows = showErrorsOnly ? rowsWithErrors : rows;

  const reportErrors = useMemo(
    () =>
      rowsWithErrors.flatMap((row) =>
        row.errors.map((error) => {
          const columnIndex = headers.indexOf(error.questionCode);
          const question = questions.find((item) => item.code === error.questionCode);
          return {
            rowNumber: row.rowIndex + dataRowFileOffset,
            columnCode: error.questionCode || `Column ${error.columnIndex + 1}`,
            columnLabel: labels[columnIndex] || question?.label || error.questionCode || `Column ${error.columnIndex + 1}`,
            currentValue: row.data[error.questionCode] ?? '',
            message: error.message,
          };
        })
      ),
    [headers, labels, questions, rowsWithErrors]
  );

  const getQuestionByCode = (code: string): ServiceFormQuestionDto | undefined => {
    return questions.find((q) => q.code === code);
  };

  const getDisplayValue = (value: string, question: ServiceFormQuestionDto | undefined): string => {
    if (!value || !question || !question.options || question.options.length === 0) {
      return value;
    }

    if (question.questionType === ServiceFormQuestionType.Checkbox) {
      const values = value.split(',').map((v) => v.trim());
      const labelsList = values.map((v) => {
        const option = question.options.find((opt) => opt.value === v);
        return option ? option.label : v;
      });
      return labelsList.join(', ');
    }

    const option = question.options.find((opt) => opt.value === value);
    return option ? option.label : value;
  };

  const handleCellActivate = (rowIndex: number, header: string, cellEl: HTMLButtonElement) => {
    activeCellRef.current = cellEl;
    setEditingCell({ rowIndex, header });
  };

  const handleCellSave = (value: string) => {
    if (editingCell) {
      onCellEdit(editingCell.rowIndex, editingCell.header, value);
      setEditingCell(null);
      activeCellRef.current?.focus();
    }
  };

  const handleCellCancel = () => {
    setEditingCell(null);
    activeCellRef.current?.focus();
  };

  const canContinue = totalErrors === 0;

  return (
    <div>
      {/* Error summary - shown only when there are errors */}
      {totalErrors > 0 && (
        <div className="bulk-upload-error-summary" role="alert" tabIndex={-1}>
          <h2 className="bulk-upload-error-summary__title">There is a problem</h2>
          <p className="bulk-upload-error-summary__body">
            {totalErrors} cell{totalErrors !== 1 ? 's' : ''} of the uploaded file contain
            {totalErrors === 1 ? 's' : ''} data validation errors
          </p>
          <ul className="govuk-list govuk-error-summary__list">
            {errorLinks.map((link) => (
              <li key={link.targetId}>
                <a
                  href={`#${link.targetId}`}
                  onClick={(e) => {
                    e.preventDefault();
                    setShowErrorsOnly(true);
                    requestAnimationFrame(() => focusErrorTargetById(link.targetId));
                  }}>
                  {link.text}
                </a>
              </li>
            ))}
          </ul>
          {totalErrors > errorLinks.length && (
            <p className="bulk-upload-error-summary__body">
              And {totalErrors - errorLinks.length} more error{totalErrors - errorLinks.length !== 1 ? 's' : ''}. Review the highlighted cells in the table below.
            </p>
          )}
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
          We have checked your uploaded file for errors. If we found any problems, the affected cells are highlighted in the table. Select a highlighted cell to see what you need
          to change.
        </p>
        <p className="bulk-upload-body">You must review and fix any errors before you can continue. You can either:</p>
        <ul style={{ fontSize: '19px', lineHeight: '25px', marginBottom: '15px' }}>
          <li>edit the cells directly in the table below, or</li>
          <li>update your file and upload it again.</li>
        </ul>
        {totalErrors > 0 && (
          <>
            <p className="bulk-upload-body">Download a PDF containing every error if you want to correct the CSV offline.</p>
            <ValidationReportDownloadButton
              className="govuk-button govuk-button--secondary"
              report={{ fileName, uploadName: 'Services', errors: reportErrors }}
            />
          </>
        )}
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
          <button type="button" className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={onOpenFullScreen}>
            Open full screen
          </button>
        </div>

        <div className="bulk-upload-table__wrapper" tabIndex={0}>
          <table className="bulk-upload-table">
            <caption className="govuk-visually-hidden">Uploaded services requiring review</caption>
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
                  <td className="bulk-upload-table__row-number">{row.rowIndex + dataRowFileOffset}</td>
                  {headers.map((header) => {
                    const cellError = getCellError(row, header);
                    const isEditing = editingCell?.rowIndex === row.rowIndex && editingCell?.header === header;
                    const cellValue = row.data[header] || '';
                    const question = getQuestionByCode(header);

                    if (isEditing) {
                      return (
                        <td key={`${row.rowIndex}-${header}`} className="bulk-upload-table__cell--editing" role="gridcell">
                          <CellEditor value={cellValue} question={question} error={cellError} onSave={handleCellSave} onCancel={handleCellCancel} />
                        </td>
                      );
                    }

                    const displayValue = getDisplayValue(cellValue, question);

                    return (
                      <td
                        key={`${row.rowIndex}-${header}`}
                        id={`cell-${row.rowIndex}-${header}`}
                        role="gridcell"
                        className={`bulk-upload-table__cell--editable${cellError ? ' bulk-upload-table__cell--error' : ''}`}>
                        {/* A focusable <td> is announced as a plain cell — assistive tech gets no
                            hint that it can be edited. A real button carries the interactive
                            semantics, and brings Enter/Space activation natively. */}
                        <button
                          type="button"
                          className="bulk-upload-table__cell-button"
                          aria-invalid={!!cellError}
                          onClick={(e) => handleCellActivate(row.rowIndex, header, e.currentTarget)}>
                          {displayValue || (cellError ? '(empty)' : '')}
                          {cellError && <span className="govuk-visually-hidden"> Error: {cellError.message}</span>}
                          <span className="govuk-visually-hidden"> select to edit</span>
                        </button>
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
        <button type="button" className="govuk-button" onClick={onContinue} disabled={!canContinue}>
          Continue
        </button>
      </div>
    </div>
  );
};

export default StepCheckData;
