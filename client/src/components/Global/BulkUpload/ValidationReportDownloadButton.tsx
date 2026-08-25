import React, { useState } from 'react';
import { GovUKButton } from '../../GovUKComponents';
import { downloadValidationReport, ValidationReportData, ValidationReportUnavailableError } from './validationReport';

interface ValidationReportDownloadButtonProps {
  report: ValidationReportData;
  className?: string;
}

const ValidationReportDownloadButton: React.FC<ValidationReportDownloadButtonProps> = ({ report, className }) => {
  const [isGenerating, setIsGenerating] = useState(false);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  const [needsReload, setNeedsReload] = useState(false);

  const handleDownload = async () => {
    setIsGenerating(true);
    setStatus('Generating validation report');
    setError('');
    setNeedsReload(false);

    try {
      await downloadValidationReport(report);
      setStatus('Validation report downloaded');
    } catch (downloadError) {
      console.error('Failed to generate validation report:', downloadError);
      setStatus('');

      // Retrying cannot help once the chunk is missing, because the page keeps asking for the
      // build it was loaded from until it is reloaded.
      if (downloadError instanceof ValidationReportUnavailableError) {
        setNeedsReload(true);
        setError('The service has been updated since you opened this page. Reload the page and upload your file again to download the report.');
      } else {
        setError('The validation report could not be downloaded. Try again.');
      }
    } finally {
      setIsGenerating(false);
    }
  };

  return (
    <>
      <GovUKButton className={className ?? 'govuk-button govuk-button--secondary'} onClick={handleDownload} isLoading={isGenerating}>
        Download validation report
      </GovUKButton>
      <span className="govuk-visually-hidden" role="status" aria-live="polite">
        {status}
      </span>
      {error && (
        <p className="govuk-error-message" role="alert">
          {error}
        </p>
      )}
      {needsReload && (
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => window.location.reload()}>
          Reload the page
        </GovUKButton>
      )}
    </>
  );
};

export default ValidationReportDownloadButton;
