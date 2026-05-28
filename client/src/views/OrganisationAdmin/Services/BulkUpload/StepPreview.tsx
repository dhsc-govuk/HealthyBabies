import React from 'react';
import { Delimiter, DELIMITER_OPTIONS, RowData } from './types';
import './styles.css';

interface StepPreviewProps {
  delimiter: Delimiter;
  headers: string[];
  labels: string[];
  rows: RowData[];
  onDelimiterChange: (delimiter: Delimiter) => void;
  onOpenFullScreen: () => void;
  onBack: () => void;
  onContinue: () => void;
}

const StepPreview: React.FC<StepPreviewProps> = ({
  delimiter,
  headers,
  labels,
  rows,
  onDelimiterChange,
  onOpenFullScreen,
  onContinue,
}) => {
  return (
    <div>
      {/* Step caption and heading */}
      <div className="bulk-upload-content">
        <p className="bulk-upload-caption">Step 2 of 4</p>
        <h1 className="bulk-upload-heading">Preview your uploaded spreadsheet</h1>
      </div>

      {/* Description */}
      <div className="bulk-upload-content bulk-upload-section">
        <p className="bulk-upload-body">
          Below you can see how the application has read your uploaded CSV file. Check that it looks
          correct.
        </p>
        <p className="bulk-upload-body">
          If the columns and rows do not look as you expected, select a different delimiter (the
          character that separates values in the file) until the table is shown correctly.
        </p>
        <p className="bulk-upload-body">
          Continue to the next step when the preview looks correct.
        </p>
      </div>

      {/* Delimiter selection */}
      <div className="bulk-upload-delimiter">
        <label className="bulk-upload-delimiter__label">Delimiter</label>
        <span className="bulk-upload-delimiter__hint">
          A delimiter is a character that separates values in a CSV file. It tells the application
          how to split your data into columns. Select the delimiter that shows your data in a table
          correctly.
        </span>
        <div className="bulk-upload-delimiter__options">
          {DELIMITER_OPTIONS.map((option) => (
            <label key={option.value} className="bulk-upload-radio-small">
              <input
                type="radio"
                name="delimiter"
                value={option.value}
                checked={delimiter === option.value}
                onChange={() => onDelimiterChange(option.value)}
                className="bulk-upload-radio-small__input"
              />
              <span className="bulk-upload-radio-small__label">{option.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Table section */}
      <div className="bulk-upload-table-section">
        <div className="bulk-upload-table-header">
          <p className="bulk-upload-table-label">Service users spreadsheet preview</p>
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
              {rows.slice(0, 15).map((row, rowIndex) => (
                <tr key={row.rowIndex}>
                  <td className="bulk-upload-table__row-number">{rowIndex + 1}</td>
                  {headers.map((header) => (
                    <td key={`${row.rowIndex}-${header}`}>{row.data[header] || ''}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {rows.length > 15 && (
          <p className="bulk-upload-body-small" style={{ marginTop: '10px' }}>
            Showing 15 of {rows.length} rows. Open full screen to see all data.
          </p>
        )}
      </div>

      {/* Continue button */}
      <div>
        <button
          type="button"
          className="bulk-upload-button"
          onClick={onContinue}
          disabled={rows.length === 0}
        >
          Continue
        </button>
      </div>
    </div>
  );
};

export default StepPreview;
