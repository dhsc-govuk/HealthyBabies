import React, { useRef, useCallback } from 'react';
import { GovUKButton } from '../../../../components/GovUKComponents';
import { validateUploadedFile } from '../../../../helpers/fileValidation';
import './styles.css';

interface StepUploadProps {
  file: File | null;
  error: string | null;
  isLoading: boolean;
  onFileSelect: (file: File) => void;
  onDownloadTemplate: (format: 'csv' | 'xlsx') => void;
  onContinue: () => void;
  moduleName: string;
}

const StepUpload: React.FC<StepUploadProps> = ({
  file,
  error,
  isLoading,
  onFileSelect,
  onDownloadTemplate,
  onContinue,
  moduleName,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [localError, setLocalError] = React.useState<string | null>(null);

  const validateFile = useCallback(async (selectedFile: File): Promise<string | null> => {
    const result = await validateUploadedFile(selectedFile, 'bulkUploadCsv');
    return result.ok ? null : result.message;
  }, []);

  const handleFileChange = useCallback(
    async (event: React.ChangeEvent<HTMLInputElement>) => {
      const selectedFile = event.target.files?.[0];
      setLocalError(null);

      if (!selectedFile) {
        return;
      }

      const validationError = await validateFile(selectedFile);
      if (validationError) {
        setLocalError(validationError);
        return;
      }

      onFileSelect(selectedFile);
    },
    [onFileSelect, validateFile]
  );

  const handleDrop = useCallback(
    async (event: React.DragEvent<HTMLDivElement>) => {
      event.preventDefault();
      event.stopPropagation();

      const droppedFile = event.dataTransfer.files[0];
      setLocalError(null);

      if (!droppedFile) {
        return;
      }

      const validationError = await validateFile(droppedFile);
      if (validationError) {
        setLocalError(validationError);
        return;
      }

      onFileSelect(droppedFile);
    },
    [onFileSelect, validateFile]
  );

  const handleDragOver = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();
  }, []);

  const displayError = localError || error;

  return (
    <div className="bulk-upload-step">
      <div className="bulk-upload-header">
        <span className="govuk-caption-l">Step 1 of 5</span>
        <h1 className="govuk-heading-xl">Upload your data</h1>
      </div>

      <p className="govuk-body">
        Upload a CSV file with your {moduleName.toLowerCase()} data.
      </p>

      <div className="govuk-form-group">
        <label className="govuk-label govuk-label--m" htmlFor="csv-file">
          Select CSV file
        </label>

        <div className="govuk-hint">
          <p>Before you upload:</p>
          <ul className="govuk-list govuk-list--bullet">
            <li>Make sure your services are added to your service list</li>
            <li>Export your file as CSV format</li>
            <li>File size must be less than 5MB</li>
          </ul>
        </div>

        {displayError && (
          <span className="govuk-error-message">
            <span className="govuk-visually-hidden">Error:</span> {displayError}
          </span>
        )}

        <div
          className="bulk-upload-dropzone"
          onDrop={handleDrop}
          onDragOver={handleDragOver}
        >
          <p className={`govuk-body ${file ? 'govuk-!-font-weight-bold' : ''}`}>
            {file ? file.name : 'No file selected'}
          </p>
          <div className="govuk-button-group">
            <button
              type="button"
              className="govuk-button govuk-button--secondary"
              onClick={() => fileInputRef.current?.click()}
            >
              Choose file
            </button>
            <span className="govuk-body">or drag and drop</span>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            id="csv-file"
            name="csv-file"
            accept=".csv"
            onChange={handleFileChange}
            style={{ display: 'none' }}
          />
        </div>
      </div>

      <div className="govuk-form-group govuk-!-margin-top-6">
        <p className="govuk-body govuk-!-font-weight-bold">Download a template</p>
        <div className="govuk-button-group">
          <GovUKButton
            className="govuk-button govuk-button--secondary"
            onClick={() => onDownloadTemplate('csv')}
            disabled={isLoading}
          >
            Download CSV template
          </GovUKButton>
          <GovUKButton
            className="govuk-button govuk-button--secondary"
            onClick={() => onDownloadTemplate('xlsx')}
            disabled={isLoading}
          >
            Download Excel template
          </GovUKButton>
        </div>
      </div>

      <div className="govuk-button-group govuk-!-margin-top-6">
        <GovUKButton
          onClick={onContinue}
          disabled={!file || isLoading}
        >
          Continue
        </GovUKButton>
      </div>
    </div>
  );
};

export default StepUpload;
