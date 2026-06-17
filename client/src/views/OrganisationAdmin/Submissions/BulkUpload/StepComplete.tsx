import React from 'react';
import { GovUKButton, GovUKNotificationBanner } from '../../../../components/GovUKComponents';
import { BulkUploadResultDto } from '../queries';
import './styles.css';

interface StepCompleteProps {
  processResult: BulkUploadResultDto;
  moduleName: string;
  onReturn: () => void;
  onUploadMore: () => void;
}

const StepComplete: React.FC<StepCompleteProps> = ({
  processResult,
  moduleName,
  onReturn,
  onUploadMore,
}) => {
  return (
    <div className="bulk-upload-step">
      {/* Step caption and heading */}
      <div className="govuk-!-width-two-thirds">
        <span className="govuk-caption-l">Step 5 of 5</span>
        <h1 className="govuk-heading-l">Upload complete</h1>
      </div>

      <GovUKNotificationBanner
        type={processResult.success ? 'success' : 'important'}
        title={processResult.success ? 'Upload complete' : 'Upload completed with errors'}
      >
        <p className="govuk-body">
          {processResult.successfulRows} of {processResult.totalRows} row(s) were successfully uploaded.
          {processResult.failedRows > 0 && ` ${processResult.failedRows} row(s) failed.`}
        </p>
      </GovUKNotificationBanner>

      <div className="govuk-!-margin-bottom-6">
        <p className="govuk-body">
          <strong>Total rows processed:</strong> {processResult.totalRows} | 
          <strong> Successful:</strong> {processResult.successfulRows} | 
          <strong> Failed:</strong> {processResult.failedRows}
        </p>
      </div>

      {processResult.failedRows > 0 && (
        <div className="govuk-!-margin-bottom-6">
          <h2 className="govuk-heading-m">Failed rows</h2>
          <div className="bulk-upload-table__wrapper">
            <table className="govuk-table">
              <thead className="govuk-table__head">
                <tr className="govuk-table__row">
                  <th scope="col" className="govuk-table__header">Row</th>
                  <th scope="col" className="govuk-table__header">Service</th>
                  <th scope="col" className="govuk-table__header">Error</th>
                </tr>
              </thead>
              <tbody className="govuk-table__body">
                {processResult.rowResults
                  .filter((row) => !row.success)
                  .map((row) => (
                    <tr key={row.rowNumber} className="govuk-table__row">
                      <td className="govuk-table__cell">{row.rowNumber}</td>
                      <td className="govuk-table__cell">{row.serviceName || '-'}</td>
                      <td className="govuk-table__cell">{row.errorMessage}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="govuk-button-group">
        <GovUKButton onClick={onReturn}>
          Return to {moduleName.toLowerCase()}
        </GovUKButton>
        {processResult.failedRows > 0 && (
          <GovUKButton
            className="govuk-button govuk-button--secondary"
            onClick={onUploadMore}
          >
            Upload more data
          </GovUKButton>
        )}
      </div>
    </div>
  );
};

export default StepComplete;
