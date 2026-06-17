import React from 'react';
import { GovUKButton } from '../../../../components/GovUKComponents';
import { Delimiter, DELIMITER_OPTIONS, ParsedData } from './types';
import './styles.css';

interface StepPreviewProps {
  parsedData: ParsedData;
  delimiter: Delimiter;
  moduleName: string;
  onDelimiterChange: (delimiter: Delimiter) => void;
  onContinue: () => void;
  onBack: () => void;
  onFullScreen: () => void;
}

const StepPreview: React.FC<StepPreviewProps> = ({
  parsedData,
  delimiter,
  moduleName,
  onDelimiterChange,
  onContinue,
  onBack,
  onFullScreen,
}) => {
  return (
    <div className="bulk-upload-step">
      <div className="bulk-upload-header">
        <span className="govuk-caption-l">Step 2 of 5</span>
        <h1 className="govuk-heading-xl">Preview your uploaded spreadsheet</h1>
      </div>

      <div className="govuk-!-width-two-thirds">
        <p className="govuk-body">
          Below you can see how the application has read your uploaded CSV file.
          Check that it looks correct.
        </p>
        <p className="govuk-body">
          If the columns and rows do not look as you expected, select a different
          delimiter (the character that separates values in the file) until the table is
          shown correctly.
        </p>
        <p className="govuk-body">
          Continue to the next step when the preview looks correct.
        </p>
      </div>

      {/* Delimiter selection */}
      <div className="govuk-form-group">
        <fieldset className="govuk-fieldset">
          <legend className="govuk-fieldset__legend govuk-fieldset__legend--s">Delimiter</legend>
          <div className="govuk-hint">
            A delimiter is a character that separates values in a CSV file. It tells the application how to split your data into
            columns. Select the delimiter that shows your data in a table correctly.
          </div>
          <div className="govuk-radios govuk-radios--inline govuk-radios--small">
            {DELIMITER_OPTIONS.map((option) => (
              <div className="govuk-radios__item" key={option.value}>
                <input
                  type="radio"
                  name="delimiter"
                  id={`delimiter-${option.value}`}
                  value={option.value}
                  checked={delimiter === option.value}
                  onChange={() => onDelimiterChange(option.value)}
                  className="govuk-radios__input"
                />
                <label className="govuk-label govuk-radios__label" htmlFor={`delimiter-${option.value}`}>
                  {option.label}
                </label>
              </div>
            ))}
          </div>
        </fieldset>
      </div>

      {/* Table preview */}
      <div className="govuk-!-margin-bottom-6">
        <div className="bulk-upload-table-header">
          <p className="govuk-label govuk-label--s govuk-!-margin-bottom-0">{moduleName} spreadsheet preview</p>
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
              {parsedData.rows.slice(0, 10).map((row, rowIdx) => (
                <tr key={rowIdx}>
                  <td className="bulk-upload-table__row-number">{rowIdx + 1}</td>
                  {row.map((cell, cellIdx) => (
                    <td key={cellIdx}>{cell}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {parsedData.rows.length > 10 && (
          <p className="govuk-body-s govuk-!-margin-top-2">
            Showing first 10 of {parsedData.rows.length} rows
          </p>
        )}
      </div>

      {/* Buttons */}
      <div className="govuk-button-group">
        <GovUKButton onClick={onContinue}>
          Continue
        </GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={onBack}>
          Back
        </GovUKButton>
      </div>
    </div>
  );
};

export default StepPreview;
