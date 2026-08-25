import React, { useRef, useCallback, useState } from 'react';
import { FileUploadProfile, validateUploadedFile } from '../../../helpers/fileValidation';

interface GovUKFileUploadProps {
  id: string;
  name: string;
  label: string;
  hint?: string;
  accept?: string;
  maxSize?: number;
  error?: string;
  value?: File | null;
  fileName?: string | null;
  isUploading?: boolean;
  onChange?: (file: File | null) => void;
  questionCode?: string;
  profile?: FileUploadProfile;
}

const GovUKFileUpload: React.FC<GovUKFileUploadProps> = ({
  id,
  name,
  label,
  hint,
  accept = '.csv,.xlsx,.pdf,.docx,.png,.jpg,.jpeg',
  maxSize,
  error,
  value,
  fileName,
  isUploading = false,
  onChange,
  questionCode,
  profile = 'submissionAttachment',
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [localError, setLocalError] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [localFile, setLocalFile] = useState<File | null>(null);

  const runValidation = useCallback(
    async (file: File): Promise<string | null> => {
      if (typeof maxSize === 'number' && file.size > maxSize) {
        return `File size must be less than ${Math.round(maxSize / 1024 / 1024)}MB`;
      }
      const result = await validateUploadedFile(file, profile);
      return result.ok ? null : result.message;
    },
    [maxSize, profile]
  );

  const handleFileChange = useCallback(
    async (event: React.ChangeEvent<HTMLInputElement>) => {
      const selectedFile = event.target.files?.[0];
      setLocalError(null);

      if (!selectedFile) {
        setLocalFile(null);
        onChange?.(null);
        return;
      }

      const validationError = await runValidation(selectedFile);
      if (validationError) {
        setLocalError(validationError);
        return;
      }

      setLocalFile(selectedFile);
      onChange?.(selectedFile);
    },
    [onChange, runValidation]
  );

  const handleDrop = useCallback(
    async (event: React.DragEvent<HTMLButtonElement>) => {
      event.preventDefault();
      event.stopPropagation();
      setIsDragging(false);

      const droppedFile = event.dataTransfer.files?.[0];
      setLocalError(null);

      if (!droppedFile) {
        return;
      }

      const validationError = await runValidation(droppedFile);
      if (validationError) {
        setLocalError(validationError);
        return;
      }

      setLocalFile(droppedFile);
      onChange?.(droppedFile);
    },
    [onChange, runValidation]
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
  const displayFileName = localFile?.name || value?.name || fileName;
  const hasFile = !!displayFileName;

  return (
    <div className={`govuk-form-group ${displayError ? 'govuk-form-group--error' : ''}`}>
      <label className="govuk-label govuk-label--s" htmlFor={id} id={`${id}-label`}>
        {questionCode && <span className="govuk-visually-hidden">Question ID: {questionCode}: </span>}
        {label}
      </label>
      {hint && (
        <div className="govuk-hint" id={`${id}-hint`}>
          {hint}
        </div>
      )}

      {displayError && (
        <p className="govuk-error-message" id={`${id}-error`}>
          <span className="govuk-visually-hidden">Error:</span> {displayError}
        </p>
      )}

      {/* Official GOV.UK "enhanced" file upload: the entire area is one button that opens the
          file picker, with the real <input type="file"> hidden alongside it. govuk-frontend
          ships every state for this markup (--empty, --dragging, hover, focus, disabled), so
          no bespoke styling is needed — and the focus state is the design system's own
          black border plus yellow outline. */}
      <div className="govuk-file-upload-wrapper">
        <button
          type="button"
          id={id}
          className={`govuk-file-upload-button${hasFile ? '' : ' govuk-file-upload-button--empty'}${isDragging ? ' govuk-file-upload-button--dragging' : ''}`}
          disabled={isUploading}
          // The status and the comma are part of the accessible name so it reads as
          // "<label>, <file name or 'No file chosen'>", matching the design system.
          aria-labelledby={`${id}-label ${id}-comma ${id}`}
          aria-describedby={[hint ? `${id}-hint` : '', displayError ? `${id}-error` : ''].filter(Boolean).join(' ') || undefined}
          onClick={() => fileInputRef.current?.click()}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDragEnter={handleDragOver}>
          <span className="govuk-body govuk-file-upload-button__status" aria-live="polite">
            {isUploading ? 'Uploading...' : displayFileName || 'No file chosen'}
          </span>
          <span className="govuk-visually-hidden" id={`${id}-comma`}>
            ,{' '}
          </span>
          <span className="govuk-file-upload-button__pseudo-button-container">
            <span className="govuk-button govuk-button--secondary govuk-file-upload-button__pseudo-button">Choose file</span>{' '}
            <span className="govuk-body govuk-file-upload-button__instruction">or drop file</span>
          </span>
        </button>
        <input ref={fileInputRef} className="govuk-file-upload" id={`${id}-input`} name={name} type="file" accept={accept} onChange={handleFileChange} hidden />
      </div>
    </div>
  );
};

export default GovUKFileUpload;
