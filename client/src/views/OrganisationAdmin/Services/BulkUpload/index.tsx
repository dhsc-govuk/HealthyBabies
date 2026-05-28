import React, { useReducer, useCallback, useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { usePapaParse } from 'react-papaparse';
import axios from 'axios';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import { getServiceFormQuestions, serviceFormQuestionsCacheKey } from '../../../../components/Global/Services';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSubmissions } from '../../Submissions/queries';
import {
  downloadServicesTemplate,
  matchServicesByName,
  bulkUpdateServices,
  TemplateFormat,
  getServicesTemplateFileName,
  getServicesTemplateMimeType,
} from '../../../../components/Global/Services/mutations';
import { processError } from '../../../../helpers/axiosErrorFallback';

import { BulkUploadState, Delimiter, RowData, ServiceUpdateSelection, BulkUpdateServicesResult, initialBulkUploadState } from './types';
import { bulkUploadReducer, BulkUploadActionType } from './reducer';
import { validateAllRows, getValidRows, normalizeRowData } from './validation';
import StepUpload from './StepUpload';
import StepPreview from './StepPreview';
import StepCheckData from './StepCheckData';
import StepUpdateServices from './StepUpdateServices';
import FullScreenTableModal from './FullScreenTableModal';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

interface LocationDto {
  id: string;
  name: string;
  isActive: boolean;
}

const BulkUploadServices: React.FC = () => {
  usePageTitle('Bulk upload services');
  const navigate = useNavigate();
  const { readString } = usePapaParse();
  const { setNotification } = useGovUKNotification();
  const { organisationId } = useAuthProvider();

  const [state, dispatch] = useReducer(bulkUploadReducer, initialBulkUploadState);
  const [fullScreenOpen, setFullScreenOpen] = useState(false);
  const [updateResult, setUpdateResult] = useState<BulkUpdateServicesResult | null>(null);
  const [submitAttempts, setSubmitAttempts] = useState(0);

  const totalRowErrors = useMemo(() => state.rows.reduce((sum, row) => sum + row.errors.length, 0), [state.rows]);
  const hasCheckErrors = state.currentStep === 'check' && totalRowErrors > 0;
  useErrorSummaryFocus(submitAttempts, hasCheckErrors);

  useEffect(() => {
    if (hasCheckErrors) {
      setSubmitAttempts((n) => n + 1);
    }
  }, [hasCheckErrors, totalRowErrors]);

  // Fetch questions
  const { data: questionsData, isLoading: questionsLoading } = useQuery({
    queryKey: serviceFormQuestionsCacheKey(),
    queryFn: () => getServiceFormQuestions(),
    onSuccess: (data) => {
      dispatch({ type: BulkUploadActionType.SET_QUESTIONS, payload: data.data });
    },
  });

  // Fetch locations so SMD19 (dynamic delivery-site checkbox) can be validated and edited inline.
  // Mirrors the ServiceForm Create flow where StepTwo injects the org's locations as SMD19 options.
  const { data: locationsData } = useQuery(
    ['locations', organisationId],
    () => axios.get<LocationDto[]>(`/organisations/${organisationId}/locations`),
    {
      enabled: !!organisationId,
      staleTime: 5 * 60 * 1000,
    }
  );

  const rawQuestions = useMemo(() => questionsData?.data || [], [questionsData?.data]);

  // Inject the org's active locations as SMD19 options so the existing validation,
  // CellEditor and normalizeRowData (label → value) machinery works for that field.
  const questions = useMemo(() => {
    const locations = locationsData?.data ?? [];
    const activeLocations = locations.filter((loc) => loc.isActive);

    return rawQuestions.map((q) => {
      if (q.code !== 'SMD19') return q;

      if (activeLocations.length === 0) {
        return {
          ...q,
          options: [],
          hint: 'No sites have been registered for your organisation. Please add sites first.',
        };
      }

      return {
        ...q,
        options: activeLocations.map((loc, index) => ({
          id: loc.id,
          value: loc.id,
          label: loc.name,
          displayOrder: index + 1,
        })),
      };
    });
  }, [rawQuestions, locationsData?.data]);

  // Fetch submissions to get active data collection ID
  const { data: submissionsData } = useQuery({
    queryKey: ['organisation-admin-submissions'],
    queryFn: getSubmissions,
  });

  // Get the first active (non-closed) data collection ID
  const activeDataCollectionId = useMemo(() => {
    const submissions = submissionsData?.data ?? [];
    const activeSubmission = submissions.find((s) => new Date(s.endDate) >= new Date() && s.status !== 'Closed');
    return activeSubmission?.id ?? null;
  }, [submissionsData?.data]);

  // Download template mutation
  const { mutateAsync: downloadTemplate, isLoading: downloadingTemplate } = useMutation({
    mutationKey: ['download-services-template'],
    mutationFn: (format: TemplateFormat) => downloadServicesTemplate(format),
    onSuccess: (data, format) => {
      const blob = new Blob([data.data], { type: getServicesTemplateMimeType(format) });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getServicesTemplateFileName(format);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  // Match services mutation
  const { mutateAsync: matchServices, isLoading: matchingServices } = useMutation({
    mutationKey: ['match-services'],
    mutationFn: (names: string[]) => matchServicesByName(names),
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  // Bulk update mutation
  const { mutateAsync: bulkUpdate, isLoading: updatingServices } = useMutation({
    mutationKey: ['bulk-update-services'],
    mutationFn: bulkUpdateServices,
    onSuccess: (data) => {
      if (data.data.errorCount === 0) {
        // Success - redirect to services list with notification
        setNotification({
          type: 'success',
          title: 'User data saved.',
          message: `User data for ${data.data.successCount} service${data.data.successCount !== 1 ? 's' : ''} has been successfully saved.`,
        });
        navigate('/organisation-admin/core-data/services');
      } else {
        // Has errors - show results page
        setUpdateResult(data.data);
        setNotification({ type: 'important', title: 'Warning', message: `Update completed with ${data.data.errorCount} errors` });
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
      // Re-read file with new delimiter
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
        payload: { rowIndex, questionCode, value },
      });

      // Re-validate after edit
      setTimeout(() => {
        const updatedRows = state.rows.map((row) => {
          if (row.rowIndex === rowIndex) {
            return { ...row, data: { ...row.data, [questionCode]: value } };
          }
          return row;
        });
        const validatedRows = validateAllRows(updatedRows, questions, state.matchResults);
        dispatch({ type: BulkUploadActionType.SET_ROWS, payload: validatedRows });
      }, 0);
    },
    [state.rows, questions, state.matchResults]
  );

  const handleToggleSelection = useCallback((serviceName: string, updateFromFile: boolean) => {
    dispatch({
      type: BulkUploadActionType.TOGGLE_UPDATE_SELECTION,
      payload: { serviceName, updateFromFile },
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
      // Get service names from rows
      const serviceNames = state.rows.map((row) => row.data['SMD01']).filter(Boolean);

      // Match services
      const result = await matchServices(serviceNames);
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
  }, [state.rows, questions, matchServices, setNotification]);

  const handleCheckContinue = useCallback(async () => {
    dispatch({ type: BulkUploadActionType.SET_LOADING, payload: true });

    try {
      // Re-match services with potentially edited names
      const serviceNames = state.rows.map((row) => row.data['SMD01']).filter(Boolean);
      const result = await matchServices(serviceNames);
      const matchResults = result.data;

      dispatch({ type: BulkUploadActionType.SET_MATCH_RESULTS, payload: matchResults });

      // Re-validate rows with new match results
      const validatedRows = validateAllRows(state.rows, questions, matchResults);
      dispatch({ type: BulkUploadActionType.SET_ROWS, payload: validatedRows });

      // Get valid rows (no errors)
      const validRows = getValidRows(validatedRows);

      // Create update selections
      const selections: ServiceUpdateSelection[] = validRows.map((row) => {
        const matchResult = row.matchResult;
        const isNew = !matchResult?.serviceId;

        return {
          serviceId: matchResult?.serviceId,
          serviceName: row.data['SMD01'],
          updateFromFile: true, // Default to update from file
          hasExistingData: matchResult?.hasExistingData || false,
          status: matchResult?.status,
          lastUpdated: matchResult?.lastUpdated,
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
  }, [state.rows, questions, matchServices, setNotification]);

  const handleSubmit = useCallback(async () => {
    const servicesToUpdate = state.updateSelections.filter((s) => s.updateFromFile);

    if (servicesToUpdate.length === 0) {
      setNotification({ type: 'important', title: 'Warning', message: 'No services selected for update' });
      return;
    }

    // Build request
    const request = {
      dataCollectionId: activeDataCollectionId,
      services: servicesToUpdate.map((selection) => {
        const row = state.rows.find((r) => r.data['SMD01'] === selection.serviceName);

        // Normalize labels to values before submission
        const normalizedData = row ? normalizeRowData(row.data, questions) : {};

        return {
          serviceId: selection.serviceId || null,
          name: normalizedData['SMD01'] || selection.serviceName,
          answers: state.headers
            .filter((h) => h !== 'SMD01') // SMD01 is handled separately
            .map((code) => ({
              questionCode: code,
              value: normalizedData[code] || undefined,
            })),
        };
      }),
    };

    try {
      await bulkUpdate(request);
    } catch (error) {
      // Error is handled by the mutation's onError callback
    }
  }, [state.updateSelections, state.rows, state.headers, questions, bulkUpdate, activeDataCollectionId, setNotification]);

  const handleFinish = useCallback(() => {
    setNotification({ type: 'success', title: 'Services updated', message: `${updateResult?.successCount || 0} services were updated successfully.` });
    navigate('/organisation-admin/core-data/services');
  }, [navigate, updateResult, setNotification]);

  const isLoading = questionsLoading || downloadingTemplate || matchingServices || updatingServices || state.isLoading;

  return (
    <>
      <GeneralLayout backLink={{ href: '/organisation-admin/core-data/services' }}>
        <div style={{ maxWidth: '960px', margin: '0 auto', paddingBottom: '40px' }}>
          {/* Step content */}
          {state.currentStep === 'upload' && (
            <StepUpload
              file={state.file}
              error={state.error}
              isLoading={isLoading}
              onFileSelect={handleFileSelect}
              onDownloadTemplate={(format) => downloadTemplate(format)}
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
            <StepUpdateServices
              selections={state.updateSelections}
              updateResult={updateResult}
              isLoading={updatingServices}
              onToggleSelection={handleToggleSelection}
              onBack={() => goToStep('check')}
              onSubmit={handleSubmit}
              onFinish={handleFinish}
            />
          )}
        </div>
      </GeneralLayout>

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

export default BulkUploadServices;