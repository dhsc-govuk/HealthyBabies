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
    async (event: React.DragEvent<HTMLDivElement>) => {
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

  const handleDragOver = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragging(false);
  }, []);

  const displayError = localError || error;
  const displayFileName = localFile?.name || value?.name || fileName;
  const hasFile = !!displayFileName;

  return (
    <div className={`govuk-form-group ${displayError ? 'govuk-form-group--error' : ''}`} style={{ position: 'relative' }}>
      {questionCode && (
        <span style={{ 
          position: 'absolute', 
          right: 0, 
          top: 0, 
          color: '#505a5f', 
          fontSize: '16px', 
          fontWeight: 'normal', 
          whiteSpace: 'nowrap' 
        }}>
          {questionCode}
        </span>
      )}
      <label className="govuk-label govuk-label--s" htmlFor={id} style={{ paddingRight: questionCode ? '100px' : undefined }}>
        {label}
      </label>
      {hint && <div className="govuk-hint">{hint}</div>}

      {displayError && (
        <p className="govuk-error-message">
          <span className="govuk-visually-hidden">Error:</span> {displayError}
        </p>
      )}

      <div
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDragEnter={handleDragOver}
        style={{
          border: isDragging ? '2px dashed #1d70b8' : '2px dashed #b1b4b6',
          backgroundColor: isDragging ? '#f3f2f1' : '#fff',
          padding: '20px',
          transition: 'border-color 0.2s, background-color 0.2s',
        }}
      >
        <p
          style={{
            backgroundColor: hasFile ? '#cce2d8' : '#bbd4ea',
            color: hasFile ? '#005a30' : '#0c2d4a',
            padding: '15px 10px',
            fontSize: '19px',
            lineHeight: '25px',
            marginBottom: '0',
          }}
        >
          {isUploading ? 'Uploading...' : (displayFileName || 'No file chosen')}
        </p>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: '15px', marginTop: '15px' }}>
          <button
            type="button"
            disabled={isUploading}
            style={{
              backgroundColor: '#f3f2f1',
              border: 'none',
              borderBottom: '2px solid #929191',
              color: '#0b0c0c',
              fontSize: '19px',
              lineHeight: '25px',
              padding: '8px 12px 7px',
              cursor: isUploading ? 'not-allowed' : 'pointer',
              fontFamily: 'inherit',
              opacity: isUploading ? 0.6 : 1,
            }}
            onClick={() => fileInputRef.current?.click()}
          >
            Choose file
          </button>
          <span style={{ fontSize: '19px', lineHeight: '25px', color: '#0b0c0c' }}>or drop file</span>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          id={id}
          name={name}
          accept={accept}
          onChange={handleFileChange}
          style={{ display: 'none' }}
        />
      </div>
    </div>
  );
};

export default GovUKFileUpload;
