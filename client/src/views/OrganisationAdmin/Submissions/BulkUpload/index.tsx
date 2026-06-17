import React, { useReducer, useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import { validateBulkUpload, processStagedBulkUpload, downloadBulkUploadTemplate, getBulkUploadTemplateFileName, type TemplateFormat } from '../queries';
import { BulkUploadProps, initialBulkUploadState, Delimiter, ServiceSelection } from './types';
import { bulkUploadReducer, BulkUploadActionType } from './reducer';
import { parseCSV, validateFile, convertValidationErrorsToCellErrors, updateRequiredFieldsFromMetadata, validateSumGroups } from './validation';
import StepUpload from './StepUpload';
import StepPreview from './StepPreview';
import StepCheckData from './StepCheckData';
import StepSelect from './StepSelect';
import StepComplete from './StepComplete';
import FullScreenTableModal from './FullScreenTableModal';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

const downloadFile = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

const BulkUpload: React.FC<BulkUploadProps> = ({ moduleType, moduleName, backUrl }) => {
  usePageTitle('Bulk upload');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const { trackStarted, trackSubmitted, trackValidationFailed } = useFormTelemetry('bulk_upload_submission');

  const [state, dispatch] = useReducer(bulkUploadReducer, initialBulkUploadState);
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, state.currentStep === 'check' && state.cellErrors.length > 0);

  useEffect(() => {
    trackStarted();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (state.currentStep === 'check' && state.cellErrors.length > 0) {
      setSubmitAttempts((n) => n + 1);
    }
  }, [state.currentStep, state.cellErrors.length]);

  // Parse CSV when delimiter or file content changes
  useEffect(() => {
    if (state.fileContent && state.currentStep === 'preview') {
      const parsed = parseCSV(state.fileContent, state.delimiter);
      dispatch({ type: BulkUploadActionType.SET_PARSED_DATA, payload: parsed });
    }
  }, [state.delimiter, state.fileContent, state.currentStep]);

  // Validation mutation
  const validateMutation = useMutation({
    mutationFn: (file: File) => validateBulkUpload(submissionId!, moduleId!, file),
    onSuccess: (response) => {
      dispatch({ type: BulkUploadActionType.SET_VALIDATION_RESULT, payload: response.data });
      dispatch({ type: BulkUploadActionType.SET_STAGING_ID, payload: response.data.stagingId });

      // Convert validation errors to cell errors
      if (state.parsedData) {
        const errors = convertValidationErrorsToCellErrors(response.data, state.parsedData.headers, state.parsedData);
        errors.forEach((e) => trackValidationFailed(`row_${e.rowIndex}_${e.fieldCode}`, 'cell_invalid'));
        dispatch({ type: BulkUploadActionType.SET_CELL_ERRORS, payload: errors });

        // Update required fields from metadata
        const updatedRequiredFields = updateRequiredFieldsFromMetadata(state.parsedData.headers, response.data);
        dispatch({
          type: BulkUploadActionType.SET_PARSED_DATA,
          payload: { ...state.parsedData, requiredFields: updatedRequiredFields },
        });
      }

      dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'check' });
    },
    onError: () => {
      dispatch({ type: BulkUploadActionType.SET_FILE_ERROR, payload: 'Failed to validate file. Please try again.' });
      dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'preview' });
    },
  });

  // Process mutation
  const processMutation = useMutation({
    mutationFn: (payload: { stagingId: string; selectedServiceNames: string[]; cellEdits: typeof state.editedCells }) =>
      processStagedBulkUpload(submissionId!, moduleId!, payload),
    onSuccess: (response) => {
      trackSubmitted();
      dispatch({ type: BulkUploadActionType.SET_PROCESS_RESULT, payload: response.data });
      dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'complete' });
      queryClient.invalidateQueries(['service-users-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['outcome-scores-module', submissionId, moduleId]);
    },
    onError: () => {
      dispatch({ type: BulkUploadActionType.SET_FILE_ERROR, payload: 'Failed to process file. Please try again.' });
      dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'upload' });
    },
  });

  // Handle file selection
  const handleFileSelect = useCallback((file: File) => {
    const error = validateFile(file);
    if (error) {
      dispatch({ type: BulkUploadActionType.SET_FILE_ERROR, payload: error });
      return;
    }

    dispatch({ type: BulkUploadActionType.SET_FILE, payload: file });

    // Read file content
    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      dispatch({ type: BulkUploadActionType.SET_FILE_CONTENT, payload: content });
    };
    reader.readAsText(file);
  }, []);

  // Handle download template
  const handleDownloadTemplate = useCallback(
    async (format: TemplateFormat) => {
      try {
        dispatch({ type: BulkUploadActionType.SET_IS_DOWNLOADING, payload: true });
        const blob = await downloadBulkUploadTemplate(submissionId!, moduleId!, format);
        const filename = getBulkUploadTemplateFileName(format, moduleType);
        downloadFile(blob, filename);
      } catch (error) {
        console.error('Failed to download template:', error);
      } finally {
        dispatch({ type: BulkUploadActionType.SET_IS_DOWNLOADING, payload: false });
      }
    },
    [submissionId, moduleId, moduleType]
  );

  // Handle continue to preview
  const handleContinueToPreview = useCallback(() => {
    if (!state.file || !state.fileContent) {
      dispatch({ type: BulkUploadActionType.SET_FILE_ERROR, payload: 'Please select a file to upload' });
      return;
    }
    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'preview' });
  }, [state.file, state.fileContent]);

  // Handle validate
  const handleValidate = useCallback(() => {
    if (!state.file) {
      dispatch({ type: BulkUploadActionType.SET_FILE_ERROR, payload: 'Please select a file to upload' });
      return;
    }
    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'processing' });
    validateMutation.mutate(state.file);
  }, [state.file, validateMutation]);

  // Handle start over
  const handleStartOver = useCallback(() => {
    dispatch({ type: BulkUploadActionType.RESET });
  }, []);

  // Handle back to upload
  const handleBackToUpload = useCallback(() => {
    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'upload' });
    dispatch({ type: BulkUploadActionType.SET_PARSED_DATA, payload: null });
  }, []);

  // Handle continue to select
  const handleContinueToSelect = useCallback(() => {
    if (!state.validationResult || !state.parsedData) return;

    // Extract unique service names from validation result
    const uniqueServices = new Map<string, ServiceSelection>();
    state.validationResult.rowValidations?.forEach((row) => {
      if (row.serviceName && !uniqueServices.has(row.serviceName)) {
        uniqueServices.set(row.serviceName, {
          serviceName: row.serviceName,
          updateFromFile: true,
          status: 'In progress',
          lastUpdated: undefined,
        });
      }
    });

    dispatch({ type: BulkUploadActionType.SET_SERVICE_SELECTIONS, payload: Array.from(uniqueServices.values()) });
    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'select' });
  }, [state.validationResult, state.parsedData]);

  // Handle toggle service selection
  const handleToggleService = useCallback((serviceName: string, updateFromFile: boolean) => {
    dispatch({ type: BulkUploadActionType.TOGGLE_SERVICE_SELECTION, payload: { serviceName, updateFromFile } });
  }, []);

  // Handle cell edit
  const handleCellEdit = useCallback(
    (rowIndex: number, cellIndex: number, value: string) => {
      dispatch({ type: BulkUploadActionType.UPDATE_CELL, payload: { rowIndex, cellIndex, value } });

      // Build the updated row locally — state hasn't flushed yet
      const updatedRow = [...(state.parsedData?.rows[rowIndex] ?? [])];
      updatedRow[cellIndex] = value;

      const headers = state.parsedData?.headers ?? [];
      const fieldMetadata = state.validationResult?.fieldMetadata;

      // Identify field codes that participate in sum-group validation so we can
      // discard their stale errors before replacing them with fresh ones below
      const sumGroupFieldCodes = new Set(
        (fieldMetadata ?? [])
          .filter((f) => {
            try {
              const config = f.configuration ? JSON.parse(f.configuration) : null;
              return config?.sumGroup || config?.maxSumField;
            } catch {
              return false;
            }
          })
          .map((f) => f.fieldCode)
      );

      // Remove the edited cell's error and any stale sum-group errors for this row
      const withoutStaleErrors = state.cellErrors.filter(
        (e) =>
          !(e.rowIndex === rowIndex && e.cellIndex === cellIndex) &&
          !(e.rowIndex === rowIndex && sumGroupFieldCodes.has(e.fieldCode))
      );

      // Re-run sum-group validation against the updated row so sibling fields
      // in the same group are cleared or re-flagged immediately
      const freshSumErrors = validateSumGroups(updatedRow, headers, rowIndex, fieldMetadata);

      dispatch({ type: BulkUploadActionType.SET_CELL_ERRORS, payload: [...withoutStaleErrors, ...freshSumErrors] });
      dispatch({ type: BulkUploadActionType.SET_EDITING_CELL, payload: null });
    },
    [state.cellErrors, state.parsedData, state.validationResult]
  );

  // Handle revalidate
  const handleRevalidate = useCallback(() => {
    if (!state.parsedData) return;

    // Reconstruct CSV from parsed data
    const csvLines = [
      state.parsedData.headers.join(state.delimiter),
      state.parsedData.labels.join(state.delimiter),
      ...state.parsedData.rows.map((row) => row.join(state.delimiter)),
    ];
    const csvContent = csvLines.join('\n');

    // Create new file from edited content
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const newFile = new File([blob], state.file?.name || 'upload.csv', { type: 'text/csv' });

    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'processing' });
    validateMutation.mutate(newFile);
  }, [state.parsedData, state.delimiter, state.file, validateMutation]);

  // Handle confirm upload
  const handleConfirmUpload = useCallback(() => {
    if (!state.stagingId || !state.parsedData) return;

    const selectedServiceNames = state.serviceSelections
      .filter((s) => s.updateFromFile)
      .map((s) => s.serviceName);

    dispatch({ type: BulkUploadActionType.SET_STEP, payload: 'processing' });
    processMutation.mutate({
      stagingId: state.stagingId,
      selectedServiceNames,
      cellEdits: state.editedCells,
    });
  }, [state.stagingId, state.parsedData, state.serviceSelections, state.editedCells, processMutation]);

  // Handle return after complete
  const handleReturn = useCallback(() => {
    navigate(backUrl, {
      state: {
        bulkUploadSuccess: {
          successfulRows: state.processResult?.successfulRows || 0,
          totalRows: state.processResult?.totalRows || 0,
        },
      },
    });
  }, [navigate, backUrl, state.processResult]);

  // Handle delimiter change
  const handleDelimiterChange = useCallback((delimiter: Delimiter) => {
    dispatch({ type: BulkUploadActionType.SET_DELIMITER, payload: delimiter });
  }, []);

  // Handle full screen toggle
  const handleFullScreenToggle = useCallback((open: boolean) => {
    dispatch({ type: BulkUploadActionType.SET_FULL_SCREEN_OPEN, payload: open });
  }, []);

  // Handle show errors only toggle
  const handleShowErrorsToggle = useCallback((show: boolean) => {
    dispatch({ type: BulkUploadActionType.SET_SHOW_ERRORS_ONLY, payload: show });
  }, []);

  // Handle start editing
  const handleStartEditing = useCallback((rowIndex: number, cellIndex: number) => {
    dispatch({ type: BulkUploadActionType.SET_EDITING_CELL, payload: { rowIndex, cellIndex } });
  }, []);

  const isLoading = validateMutation.isLoading || processMutation.isLoading || state.isDownloading;

  // Render processing state
  if (state.currentStep === 'processing') {
    return (
      <GeneralLayout backLink={{ href: backUrl }}>
        <div className="bulk-upload-container">
          <div className="govuk-!-width-two-thirds">
            <span className="govuk-caption-l">Step 2 of 5</span>
            <h1 className="govuk-heading-l">Processing your file</h1>
          </div>
          <LoadingSpinner loading label="Processing your file...">
            <div className="bulk-upload__processing">
              <p className="govuk-body">Please wait while we validate your file. This may take a few moments.</p>
            </div>
          </LoadingSpinner>
        </div>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout backLink={{ href: backUrl }}>
      <div className="bulk-upload-container">
        {state.currentStep === 'upload' && (
          <StepUpload
            file={state.file}
            error={state.fileError}
            isLoading={isLoading}
            onFileSelect={handleFileSelect}
            onDownloadTemplate={handleDownloadTemplate}
            onContinue={handleContinueToPreview}
            moduleName={moduleName}
          />
        )}

        {state.currentStep === 'preview' && state.parsedData && (
          <StepPreview
            parsedData={state.parsedData}
            delimiter={state.delimiter}
            moduleName={moduleName}
            onDelimiterChange={handleDelimiterChange}
            onContinue={handleValidate}
            onBack={handleBackToUpload}
            onFullScreen={() => handleFullScreenToggle(true)}
          />
        )}

        {state.currentStep === 'check' && state.parsedData && (
          <StepCheckData
            parsedData={state.parsedData}
            cellErrors={state.cellErrors}
            showErrorsOnly={state.showErrorsOnly}
            editingCell={state.editingCell}
            validationResult={state.validationResult}
            onToggleShowErrors={handleShowErrorsToggle}
            onCellEdit={handleCellEdit}
            onStartEditing={handleStartEditing}
            onCancelEditing={() => dispatch({ type: BulkUploadActionType.SET_EDITING_CELL, payload: null })}
            onContinue={handleContinueToSelect}
            onRevalidate={handleRevalidate}
            onStartOver={handleStartOver}
            onFullScreen={() => handleFullScreenToggle(true)}
          />
        )}

        {state.currentStep === 'select' && (
          <StepSelect serviceSelections={state.serviceSelections} isLoading={isLoading} onToggleService={handleToggleService} onConfirm={handleConfirmUpload} />
        )}

        {state.currentStep === 'complete' && state.processResult && (
          <StepComplete processResult={state.processResult} moduleName={moduleName} onReturn={handleReturn} onUploadMore={handleStartOver} />
        )}
      </div>

      {/* Full screen modal */}
      {state.parsedData && (
        <FullScreenTableModal
          open={state.fullScreenOpen}
          title={state.currentStep === 'check' ? 'Check your data' : 'Preview data'}
          headers={state.parsedData.headers}
          labels={state.parsedData.labels}
          requiredFields={state.parsedData.requiredFields}
          rows={state.parsedData.rows}
          editable={state.currentStep === 'check'}
          cellErrors={state.currentStep === 'check' ? state.cellErrors : []}
          fieldMetadata={state.currentStep === 'check' ? state.validationResult?.fieldMetadata : undefined}
          onCellEdit={state.currentStep === 'check' ? handleCellEdit : undefined}
          onClose={() => handleFullScreenToggle(false)}
        />
      )}
    </GeneralLayout>
  );
};

export default BulkUpload;

