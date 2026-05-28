import React, { useRef, useCallback } from 'react';
import { validateUploadedFile } from '../../../../helpers/fileValidation';
import './styles.css';

interface StepUploadProps {
  file: File | null;
  error: string | null;
  isLoading: boolean;
  onFileSelect: (file: File) => void;
  onDownloadTemplate: (format: 'csv' | 'xlsx') => void;
  onContinue: () => void;
}

const StepUpload: React.FC<StepUploadProps> = ({
  file,
  error,
  isLoading,
  onFileSelect,
  onDownloadTemplate,
  onContinue,
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
    <div>
      {/* Step caption and heading */}
      <div className="bulk-upload-content">
        <p className="bulk-upload-caption">Step 1 of 4</p>
        <h1 className="bulk-upload-heading">
          Upload the spreadsheet with services
        </h1>
      </div>

      {/* File upload */}
      <div className="bulk-upload-file">
        <label className="bulk-upload-file__label">Service users spreadsheet</label>
        <div className="bulk-upload-file__hint">
          <p style={{ marginBottom: '10px' }}>
            Upload the completed template for all Best Start Family
            Hub and Healthy Babies services.
          </p>
          <p style={{ marginBottom: '10px' }}>Before you upload, make sure you have:</p>
          <ul>
            <li>added all services to your service list</li>
            <li>confirmed the information in your service list is up to date</li>
            <li>exported the completed template as a CSV file.</li>
          </ul>
          <p style={{ marginTop: '15px', marginBottom: '10px' }}>
            <strong>Download a template:</strong>
          </p>
          <div style={{ display: 'flex', gap: '10px' }}>
            <button
              type="button"
              className="bulk-upload-button bulk-upload-button--secondary"
              onClick={() => onDownloadTemplate('csv')}
              disabled={isLoading}
            >
              Download CSV template
            </button>
            <button
              type="button"
              className="bulk-upload-button bulk-upload-button--secondary"
              onClick={() => onDownloadTemplate('xlsx')}
              disabled={isLoading}
            >
              Download Excel template
            </button>
          </div>
          <p style={{ marginTop: '15px', marginBottom: 0 }}>
            Your upload will be checked for errors, and guidance on how to correct any issues will
            be provided on the next pages.
          </p>
        </div>

        {displayError && (
          <span className="nhsuk-error-message" style={{ marginBottom: '15px', display: 'block' }}>
            <span className="nhsuk-u-visually-hidden">Error:</span> {displayError}
          </span>
        )}

        <div
          className="bulk-upload-file__dropzone"
          onDrop={handleDrop}
          onDragOver={handleDragOver}
        >
          <p
            className={`bulk-upload-file__status ${file ? 'bulk-upload-file__status--success' : ''}`}
          >
            {file ? file.name : 'No file chosen'}
          </p>
          <div className="bulk-upload-file__controls">
            <button
              type="button"
              className="bulk-upload-file__button"
              onClick={() => fileInputRef.current?.click()}
            >
              Choose file
            </button>
            <span className="bulk-upload-file__or-text">or drop file</span>
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

      {/* Continue button */}
      <div>
        <button
          type="button"
          className="bulk-upload-button"
          onClick={onContinue}
          disabled={!file || isLoading}
        >
          Continue
        </button>
      </div>
    </div>
  );
};

export default StepUpload;
