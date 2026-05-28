import React, { useReducer, useCallback, useState, useEffect, useMemo } from 'react';
import { useMutation, useQuery } from 'react-query';
import { usePapaParse } from 'react-papaparse';
import { LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import {
  getSiteFormQuestions,
  siteFormQuestionsCacheKey,
} from '../../../SiteForms';
import {
  downloadTemplate,
  matchLocationsByName,
  bulkUpdateLocations,
  TemplateFormat,
  getTemplateFileName,
  getTemplateMimeType,
} from './mutations';
import { processError } from '../../../../../helpers/axiosErrorFallback';

import {
  BulkUploadState,
  Delimiter,
  RowData,
  LocationUpdateSelection,
  initialBulkUploadState,
} from './types';
import { BulkUpdateLocationsResult } from './mutations';
import { bulkUploadReducer, BulkUploadActionType } from './reducer';
import { validateAllRows, getValidRows, normalizeRowData } from './validation';
import StepUpload from './StepUpload';
import StepPreview from './StepPreview';
import StepCheckData from './StepCheckData';
import StepUpdateLocations from './StepUpdateLocations';
import FullScreenTableModal from './FullScreenTableModal';
import './styles.css';

const PREDEFINED_NAME_CODE = 'FHS01';

interface NotificationData {
  type: 'success' | 'important';
  title: string;
  message: string;
}

interface Props {
  organisationId: string;
  handleFinish: (notification?: NotificationData) => void;
  apiBasePath?: string;
}

const BulkUploadComponent = ({ organisationId, handleFinish }: Props): React.ReactElement => {
  const { readString } = usePapaParse();
  const { setNotification } = useGovUKNotification();

  const [state, dispatch] = useReducer(bulkUploadReducer, initialBulkUploadState);
  const [fullScreenOpen, setFullScreenOpen] = useState(false);
  const [updateResult, setUpdateResult] = useState<BulkUpdateLocationsResult | null>(null);
  const [submitAttempts, setSubmitAttempts] = useState(0);

  const totalRowErrors = useMemo(
    () => state.rows.reduce((sum, row) => sum + row.errors.length, 0),
    [state.rows]
  );
  const hasCheckErrors = state.currentStep === 'check' && totalRowErrors > 0;
  useErrorSummaryFocus(submitAttempts, hasCheckErrors);

  useEffect(() => {
    if (hasCheckErrors) {
      setSubmitAttempts((n) => n + 1);
    }
  }, [hasCheckErrors, totalRowErrors]);

  // Fetch questions
  const { data: questionsData, isLoading: questionsLoading } = useQuery({
    queryKey: siteFormQuestionsCacheKey(),
    queryFn: () => getSiteFormQuestions(),
    onSuccess: (data) => {
      dispatch({ type: BulkUploadActionType.SET_QUESTIONS, payload: data.data });
    },
  });

  const questions = useMemo(() => questionsData?.data || [], [questionsData?.data]);

  // Download template mutation
  const { mutateAsync: downloadTemplateMutation, isLoading: downloadingTemplate } = useMutation({
    mutationKey: ['download-locations-template'],
    mutationFn: (format: TemplateFormat) => downloadTemplate(organisationId, format),
    onSuccess: (data, format) => {
      const blob = new Blob([data.data], { type: getTemplateMimeType(format) });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getTemplateFileName(format);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  // Match locations mutation
  const { mutateAsync: matchLocations, isLoading: matchingLocations } = useMutation({
    mutationKey: ['match-locations'],
    mutationFn: (names: string[]) => matchLocationsByName(organisationId, names),
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  // Bulk update mutation
  const { mutateAsync: bulkUpdate, isLoading: updatingLocations } = useMutation({
    mutationKey: ['bulk-update-locations'],
    mutationFn: (request: { locations: { locationId?: string; name: string; answers: { questionCode: string; value?: string }[] }[] }) =>
      bulkUpdateLocations(organisationId, request),
    onSuccess: (response) => {
      const data = response.data;
      if (data.errorCount === 0) {
        // Success - redirect to sites list with notification
        handleFinish({
          type: 'success',
          title: 'Delivery location updated',
          message: `${data.successCount} site${data.successCount !== 1 ? 's' : ''} ${data.successCount !== 1 ? 'have' : 'has'} been successfully updated.`,
        });
      } else {
        // Has errors - show results page
        setUpdateResult(data);
        setNotification({ type: 'important', title: 'Warning', message: `Update completed with ${data.errorCount} errors` });
      }
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  // Parse CSV when file or delimiter changes
  const parseCSV = useCallback(
    (content: string, delimiter: Delimiter) => {
      readString(content, {
        delimiter,
        skipEmptyLines: true,
        complete: (results) => {
          const data = results.data as string[][];
          if (data.length < 3) {
            dispatch({
              type: BulkUploadActionType.SET_ERROR,
              payload: 'CSV file must have at least 3 rows (headers, labels, and data)',
            });
            return;
          }

          const headers = data[0];
          const labels = data[1];
          const dataRows = data.slice(2);

          // Create RowData objects
          const rows: RowData[] = dataRows.map((row, index) => {
            const rowData: Record<string, string> = {};
            headers.forEach((header, colIndex) => {
              rowData[header] = row[colIndex] || '';
            });
            return {
              rowIndex: index,
              data: rowData,
              errors: [],
            };
          });

          dispatch({
            type: BulkUploadActionType.SET_PARSED_DATA,
            payload: { headers, labels, rows },
          });
          dispatch({ type: BulkUploadActionType.SET_RAW_DATA, payload: data });
        },
        error: (error) => {
          dispatch({ type: BulkUploadActionType.SET_ERROR, payload: error.message });
        },
      });
    },
    [readString]
  );

  // Re-parse when delimiter changes
  useEffect(() => {
    if (state.file && state.rawData.length === 0) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        parseCSV(content, state.delimiter);
      };
      reader.readAsText(state.file);
    } else if (state.rawData.length > 0 && state.file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        parseCSV(content, state.delimiter);
      };
      reader.readAsText(state.file);
    }
  }, [state.delimiter, state.file, parseCSV, state.rawData.length]);

  // Handlers
  const handleFileSelect = useCallback((file: File) => {
    dispatch({ type: BulkUploadActionType.SET_FILE, payload: file });
    dispatch({ type: BulkUploadActionType.SET_RAW_DATA, payload: [] });
  }, []);

  const handleDelimiterChange = useCallback((delimiter: Delimiter) => {
    dispatch({ type: BulkUploadActionType.SET_DELIMITER, payload: delimiter });
  }, []);

  const handleCellEdit = useCallback(
    (rowIndex: number, questionCode: string, value: string) => {
      dispatch({
        type: BulkUploadActionType.UPDATE_CELL,
        payload: { rowIndex, questionCode, value, questions, matchResults: state.matchResults },
      });
    },
    [questions, state.matchResults]
  );

  const handleToggleSelection = useCallback((siteName: string, updateFromFile: boolean) => {
    dispatch({
      type: BulkUploadActionType.TOGGLE_UPDATE_SELECTION,
      payload: { siteName, updateFromFile },
    });
  }, []);

  // Step navigation
  const goToStep = (step: BulkUploadState['currentStep']) => {
    dispatch({ type: BulkUploadActionType.SET_STEP, payload: step });
  };

  const handleUploadContinue = useCallback(() => {
    if (state.file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        parseCSV(content, state.delimiter);
        goToStep('preview');
      };
      reader.readAsText(state.file);
    }
  }, [state.file, state.delimiter, parseCSV]);

  const handlePreviewContinue = useCallback(async () => {
    dispatch({ type: BulkUploadActionType.SET_LOADING, payload: true });

    try {
      // Get site names from rows
      const siteNames = state.rows.map((row) => row.data[PREDEFINED_NAME_CODE]).filter(Boolean);

      // Match locations
      const result = await matchLocations(siteNames);
      const matchResults = result.data;

      dispatch({ type: BulkUploadActionType.SET_MATCH_RESULTS, payload: matchResults });

      // Validate all rows
      const validatedRows = validateAllRows(state.rows, questions, matchResults);
      dispatch({ type: BulkUploadActionType.SET_ROWS, payload: validatedRows });

      goToStep('check');
    } catch (error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    } finally {
      dispatch({ type: BulkUploadActionType.SET_LOADING, payload: false });
    }
  }, [state.rows, questions, matchLocations, setNotification]);

  const handleCheckContinue = useCallback(async () => {
    dispatch({ type: BulkUploadActionType.SET_LOADING, payload: true });

    try {
      // Re-match locations with potentially edited names
      const siteNames = state.rows.map((row) => row.data[PREDEFINED_NAME_CODE]).filter(Boolean);
      const result = await matchLocations(siteNames);
      const matchResults = result.data;

      dispatch({ type: BulkUploadActionType.SET_MATCH_RESULTS, payload: matchResults });

      // Re-validate rows with new match results
      const validatedRows = validateAllRows(state.rows, questions, matchResults);
      dispatch({ type: BulkUploadActionType.SET_ROWS, payload: validatedRows });

      // Get valid rows (no errors)
      const validRows = getValidRows(validatedRows);

      // Create update selections
      const selections: LocationUpdateSelection[] = validRows.map((row) => {
        const matchResult = row.matchResult;
        const isNew = !matchResult?.locationId;

        return {
          locationId: matchResult?.locationId,
          siteName: row.data[PREDEFINED_NAME_CODE],
          updateFromFile: true, // Default to update from file
          hasExistingData: matchResult?.hasExistingData || false,
          isActive: matchResult?.isActive || false,
          isNew,
        };
      });

      dispatch({ type: BulkUploadActionType.SET_UPDATE_SELECTIONS, payload: selections });
      goToStep('update');
    } catch (error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    } finally {
      dispatch({ type: BulkUploadActionType.SET_LOADING, payload: false });
    }
  }, [state.rows, questions, matchLocations, setNotification]);

  const handleSubmit = useCallback(async () => {
    const locationsToUpdate = state.updateSelections.filter((s) => s.updateFromFile);

    // Build request
    const request = {
      locations: locationsToUpdate.map((selection) => {
        const row = state.rows.find(
          (r) => r.data[PREDEFINED_NAME_CODE] === selection.siteName
        );

        // Normalize labels to values before submission
        const normalizedData = row ? normalizeRowData(row.data, questions) : {};

        return {
          locationId: selection.locationId,
          name: normalizedData[PREDEFINED_NAME_CODE] || selection.siteName,
          answers: state.headers.map((code) => ({
            questionCode: code,
            value: normalizedData[code] || undefined,
          })),
        };
      }),
    };

    await bulkUpdate(request);
  }, [state.updateSelections, state.rows, state.headers, questions, bulkUpdate]);

  const handleFinishClick = useCallback(() => {
    // When finishing from error results page, pass notification about partial success
    if (updateResult) {
      handleFinish({
        type: 'important',
        title: 'Delivery location update completed with errors',
        message: `${updateResult.successCount} site${updateResult.successCount !== 1 ? 's' : ''} updated successfully. ${updateResult.errorCount} site${updateResult.errorCount !== 1 ? 's' : ''} failed to update.`,
      });
    } else {
      handleFinish();
    }
  }, [handleFinish, updateResult]);

  const isLoading =
    questionsLoading ||
    downloadingTemplate ||
    matchingLocations ||
    updatingLocations ||
    state.isLoading;

  return (
    <>
      <div style={{ maxWidth: '960px', margin: '0 auto', paddingBottom: '40px' }}>
        {/* Step content */}
        {state.currentStep === 'upload' && (
          <StepUpload
            file={state.file}
            error={state.error}
            isLoading={isLoading}
            onFileSelect={handleFileSelect}
            onDownloadTemplate={(format) => downloadTemplateMutation(format)}
            onContinue={handleUploadContinue}
          />
        )}

        {state.currentStep === 'preview' && (
          <StepPreview
            delimiter={state.delimiter}
            headers={state.headers}
            labels={state.labels}
            rows={state.rows}
            onDelimiterChange={handleDelimiterChange}
            onOpenFullScreen={() => setFullScreenOpen(true)}
            onBack={() => goToStep('upload')}
            onContinue={handlePreviewContinue}
          />
        )}

        {state.currentStep === 'check' && (
          <StepCheckData
            headers={state.headers}
            labels={state.labels}
            rows={state.rows}
            questions={questions}
            onCellEdit={handleCellEdit}
            onOpenFullScreen={() => setFullScreenOpen(true)}
            onBack={() => goToStep('preview')}
            onContinue={handleCheckContinue}
          />
        )}

        {state.currentStep === 'update' && (
          <StepUpdateLocations
            selections={state.updateSelections}
            updateResult={updateResult}
            isLoading={updatingLocations}
            onToggleSelection={handleToggleSelection}
            onBack={() => goToStep('check')}
            onSubmit={handleSubmit}
            onFinish={handleFinishClick}
          />
        )}
      </div>

      {/* Full screen modal */}
      <FullScreenTableModal
        open={fullScreenOpen}
        title={state.currentStep === 'check' ? 'Check your data' : 'Preview data'}
        headers={state.headers}
        labels={state.labels}
        rows={state.rows}
        questions={questions}
        editable={state.currentStep === 'check'}
        onCellEdit={state.currentStep === 'check' ? handleCellEdit : undefined}
        onClose={() => setFullScreenOpen(false)}
      />

      {isLoading && <LoadingSpinner label="Loading..." />}
    </>
  );
};

export default BulkUploadComponent;