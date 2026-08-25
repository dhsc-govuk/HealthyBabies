import React, { useRef, useCallback } from 'react';
import { GovUKButton } from '../../../components/GovUKComponents';
import { validateUploadedFile } from '../../../helpers/fileValidation';
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
  const [isDragging, setIsDragging] = React.useState(false);
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
    async (event: React.DragEvent<HTMLButtonElement>) => {
      event.preventDefault();
      event.stopPropagation();
      setIsDragging(false);

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

  const handleDragOver = useCallback((event: React.DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((event: React.DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragging(false);
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
        <label className="govuk-label govuk-label--m" id="csv-file-label" htmlFor="csv-file">
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

        {/* Official GOV.UK "enhanced" file upload: the whole area is one button that opens
            the picker, with the real <input type="file"> hidden alongside. govuk-frontend ships
            every state for this markup (--empty, --dragging, hover, focus, disabled), so the
            focus ring is the design system's own black border plus yellow outline. */}
        <div className="govuk-file-upload-wrapper">
          <button
            type="button"
            id="csv-file"
            className={`govuk-file-upload-button${file ? '' : ' govuk-file-upload-button--empty'}${isDragging ? ' govuk-file-upload-button--dragging' : ''}`}
            aria-labelledby="csv-file-label csv-file-comma csv-file"
            onClick={() => fileInputRef.current?.click()}
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onDragEnter={handleDragOver}
            onDragLeave={handleDragLeave}>
            <span className="govuk-body govuk-file-upload-button__status" aria-live="polite">
              {file ? file.name : 'No file chosen'}
            </span>
            <span className="govuk-visually-hidden" id="csv-file-comma">
              ,{' '}
            </span>
            <span className="govuk-file-upload-button__pseudo-button-container">
              <span className="govuk-button govuk-button--secondary govuk-file-upload-button__pseudo-button">Choose file</span>{' '}
              <span className="govuk-body govuk-file-upload-button__instruction">or drop file</span>
            </span>
          </button>
          <input ref={fileInputRef} className="govuk-file-upload" id="csv-file-input" name="csv-file" type="file" accept=".csv" onChange={handleFileChange} hidden />
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
