import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, FormFieldRenderer, SummaryList } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import { getOutcomeScoreRecord, getSubmission, saveOutcomeScoreRecord, FormModuleFieldDto } from '../queries';
import { validateOutcomeScore } from '../validationUtils';

type ViewMode = 'form' | 'summary';

interface ConditionalRule {
  fundingRequired?: boolean;
  showWhen?: {
    fieldKey?: string;
    equals?: string;
    notEquals?: string;
    in?: string[];
    greaterThan?: number;
    allOf?: Array<{
      fieldKey: string;
      equals?: string;
      greaterThan?: number;
    }>;
  };
}

const OutcomeScoreForm = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('outcome_score_submission');
  const { submissionId, moduleId, recordId } = useParams<{
    submissionId: string;
    moduleId: string;
    recordId: string;
  }>();

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode>('form');
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.keys(fieldErrors).length > 0);

  const { data: submissionData } = useQuery({
    queryKey: ['organisation-admin-submission', submissionId],
    queryFn: () => getSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const dataCollectionName = submissionData?.data?.name || '';

  const { data: formData, isLoading } = useQuery({
    queryKey: ['outcome-score-record', submissionId, moduleId, recordId],
    queryFn: () => getOutcomeScoreRecord(submissionId!, moduleId!, recordId!),
    enabled: !!submissionId && !!moduleId && !!recordId,
    onSuccess: (data) => {
      trackStarted();
      const initialValues: Record<string, string | string[] | null> = {};
      data.data.fields.forEach((field: FormModuleFieldDto) => {
        if (field.fieldType.toLowerCase() === 'checkbox') {
          initialValues[field.code] = field.value ? field.value.split(',') : [];
        } else {
          initialValues[field.code] = field.value;
        }
      });
      if (data.data.serviceId) {
        initialValues['PPS01'] = data.data.serviceId;
      }
      setFieldValues(initialValues);
    },
  });

  // Save mutation
  const saveMutation = useMutation({
    mutationFn: (markComplete: boolean) => {
      const serializedValues: Record<string, string | null> = {};
      Object.entries(fieldValues).forEach(([key, value]) => {
        if (Array.isArray(value)) {
          serializedValues[key] = value.join(',');
        } else {
          serializedValues[key] = value;
        }
      });
      return saveOutcomeScoreRecord(submissionId!, moduleId!, recordId!, {
        fieldValues: serializedValues,
        markComplete,
      });
    },
    onSuccess: (_data, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['outcome-scores-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['outcome-score-record', submissionId, moduleId, recordId]);
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);

      // Get the selected service name from availableServices
      const selectedServiceId = fieldValues['PPS01'] as string;
      const selectedService = formData?.data?.availableServices?.find((s) => s.serviceId === selectedServiceId);
      const serviceName = selectedService?.serviceName || 'the service';

      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`, {
        state: {
          notification: {
            type: 'success',
            title: 'Outcome score record added',
            message: `The outcome score record for '${serviceName}' has been added to your list of outcome scores. You can view, change, or delete the record at any time.`,
          },
        },
      });
    },
  });

  const outcomeScoreForm = formData?.data;

  const sections = useMemo((): Array<{ id: string; sectionNumber: number; title: string; description: string | null }> => {
    if (!outcomeScoreForm) return [];
    return [...outcomeScoreForm.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [outcomeScoreForm]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 2;

  const currentSection = useMemo(() => {
    if (currentStep > sections.length) return null;
    return sections[currentStep - 1];
  }, [sections, currentStep]);

  const parseConditionalRules = (rules: string | null): ConditionalRule | null => {
    if (!rules) return null;
    try {
      return JSON.parse(rules);
    } catch {
      return null;
    }
  };

  const isFieldVisible = useCallback(
    (field: FormModuleFieldDto): boolean => {
      const rules = parseConditionalRules(field.conditionalRules);
      if (!rules?.showWhen) return true;

      const { showWhen } = rules;

      // Helper to check if a value matches (handles both string and array parent values)
      const valueMatches = (parentValue: string | string[] | null, targetValue: string): boolean => {
        if (Array.isArray(parentValue)) {
          return parentValue.includes(targetValue);
        }
        return parentValue === targetValue;
      };

      const valueNotMatches = (parentValue: string | string[] | null, targetValue: string): boolean => {
        if (Array.isArray(parentValue)) {
          return !parentValue.includes(targetValue);
        }
        return parentValue !== targetValue;
      };

      if (showWhen.allOf) {
        return showWhen.allOf.every((condition: { fieldKey: string; equals?: string; greaterThan?: number }) => {
          const parentValue = fieldValues[condition.fieldKey];
          if (condition.equals !== undefined) {
            return valueMatches(parentValue, condition.equals);
          }
          if (condition.greaterThan !== undefined) {
            const numValue = parseFloat(parentValue as string);
            return !isNaN(numValue) && numValue > condition.greaterThan;
          }
          return true;
        });
      }

      if (showWhen.fieldKey) {
        const parentValue = fieldValues[showWhen.fieldKey];

        if (showWhen.equals !== undefined) {
          return valueMatches(parentValue, showWhen.equals);
        }
        if (showWhen.notEquals !== undefined) {
          return valueNotMatches(parentValue, showWhen.notEquals);
        }
        if (showWhen.in !== undefined && Array.isArray(showWhen.in)) {
          if (Array.isArray(parentValue)) {
            return showWhen.in.some((v: string) => parentValue.includes(v));
          }
          return showWhen.in.includes(parentValue as string);
        }
        if (showWhen.greaterThan !== undefined) {
          const numValue = parseFloat(parentValue as string);
          return !isNaN(numValue) && numValue > showWhen.greaterThan;
        }
      }

      return true;
    },
    [fieldValues]
  );

  const currentFields = useMemo(() => {
    if (!outcomeScoreForm || !currentSection) return [];
    return outcomeScoreForm.fields
      .filter((f) => f.stepNumber === currentSection.sectionNumber)
      .filter(isFieldVisible)
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((field) => {
        // Handle dynamic options for service selection field
        if (field.configuration) {
          try {
            const config = JSON.parse(field.configuration);
            if (config.dynamicOptions === 'services' && outcomeScoreForm.availableServices) {
              return {
                ...field,
                options: outcomeScoreForm.availableServices.map((s, index) => ({
                  value: s.serviceId,
                  label: s.serviceName,
                  displayOrder: index + 1,
                })),
              };
            }
          } catch {
            // Ignore parse errors
          }
        }
        return field;
      });
  }, [outcomeScoreForm, currentSection, isFieldVisible]);

  const handleFieldChange = (code: string, value: string | string[] | null) => {
    setFieldValues((prev) => ({ ...prev, [code]: value }));
    // Clear error when field is changed
    if (fieldErrors[code]) {
      setFieldErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[code];
        return newErrors;
      });
    }
  };

  // Validate outcome score fields on step 4 (intervention scores)
  const validateCurrentStep = useCallback((): boolean => {
    if (!outcomeScoreForm) return true;
    const errors: Record<string, string> = {};

    // Only validate on step 4 (intervention scores)
    if (currentStep === 4) {
      currentFields.forEach((field) => {
        if (!isFieldVisible(field)) return;

        const value = fieldValues[field.code] as string;
        if (!value) return;

        // Check if this is an outcome score field - pass field for config-based validation
        const error = validateOutcomeScore(field.code, value, field);
        if (error) {
          errors[field.code] = error;
        }
      });
    }

    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [outcomeScoreForm, currentFields, fieldValues, currentStep, isFieldVisible, trackValidationFailed]);

  const handleContinue = () => {
    if (currentStep < totalSteps) {
      trackSectionCompleted(String(currentStep));
      setCurrentStep(currentStep + 1);
    }
  };

  const handleBack = () => {
    if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    } else {
      trackAbandoned();
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`);
    }
  };

  const handleSaveAndContinue = () => {
    // Validate before continuing
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }

    if (currentStep === sections.length) {
      trackReviewReached();
      setViewMode('summary');
    } else {
      handleContinue();
    }
  };

  const handleSubmit = () => {
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  const handleSaveAsDraft = () => {
    saveMutation.mutate(false);
  };

  const renderSummary = () => {
    if (!outcomeScoreForm) return null;

    // Build summary items for SummaryList component
    const summaryItems: Array<{ label: string; value: string; isSubAnswer?: boolean; isGroupHeader?: boolean }> = [];

    // Group fields by their configuration group
    const groupedFields: Record<string, { label: string; fields: Array<{ field: FormModuleFieldDto; displayValue: string }> }> = {};
    const fieldOrder: string[] = [];

    outcomeScoreForm.fields
      .filter(isFieldVisible)
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .forEach((field) => {
        const value = fieldValues[field.code];
        let displayValue = '';
        if (value !== null && value !== undefined && value !== '') {
          if (Array.isArray(value)) {
            const labels = value.map((v) => {
              const option = field.options.find((o) => o.value === v);
              return option?.label || v;
            });
            displayValue = labels.join(', ');
          } else {
            // Check if it's a service selection field
            if (field.code === 'PPS01' && outcomeScoreForm.availableServices) {
              const service = outcomeScoreForm.availableServices.find((s) => s.serviceId === value);
              displayValue = service?.serviceName || value;
            } else {
              const option = field.options.find((o) => o.value === value);
              displayValue = option?.label || value;
            }
          }
        }

        // Parse configuration to check for grouping
        let groupKey = field.code;
        let groupLabel = field.label;
        if (field.configuration) {
          try {
            const config = JSON.parse(field.configuration);
            if (config.group) {
              groupKey = config.group;
              groupLabel = config.groupLabel || field.label;
            }
          } catch {
            // Ignore
          }
        }

        if (!groupedFields[groupKey]) {
          groupedFields[groupKey] = { label: groupLabel, fields: [] };
          fieldOrder.push(groupKey);
        }
        groupedFields[groupKey].fields.push({ field, displayValue });
      });

    // Convert grouped fields to summary items
    fieldOrder.forEach((groupKey) => {
      const group = groupedFields[groupKey];
      if (group.fields.length > 1 && group.fields[0].field.configuration) {
        try {
          const config = JSON.parse(group.fields[0].field.configuration);
          if (config.group) {
            // Add parent label as a header row (isGroupHeader = bold label, no value)
            summaryItems.push({ label: group.label, value: '', isGroupHeader: true });
            // Add child fields as sub-answers
            group.fields.forEach(({ field, displayValue }) => {
              summaryItems.push({ label: field.label, value: displayValue, isSubAnswer: true });
            });
            return;
          }
        } catch {
          // Fall through to default
        }
      }
      // Single field
      const { field, displayValue } = group.fields[0];
      summaryItems.push({ label: field.label, value: displayValue });
    });

    const isSaving = saveMutation.isLoading;

    // Review is always step 6 (sections.length + 1)
    const reviewStepNumber = sections.length + 1;

    return (
      <>
        <span className="govuk-caption-l">
          Step {reviewStepNumber} of {reviewStepNumber}
        </span>
        <h1 className="govuk-heading-l">Add an outcome score record</h1>

        <h2 className="govuk-heading-m">Check your answers before saving your data</h2>
        <p className="govuk-body">
          Check the information you've provided for this outcome score record. Make sure the details are accurate and complete. You can go back to make changes if anything needs
          updating.
        </p>

        <SummaryList noOuterBorder halfWidthColumns items={summaryItems} />

        <h2 className="govuk-heading-m">Now save your outcome score record</h2>
        <p className="govuk-body">
          By saving the outcome score record, you are confirming that, to the best of your knowledge, the data you are providing is correct. You may return and update these details
          before the quarterly Management Information submission.
        </p>

        <GovUKButton onClick={handleSubmit} disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Confirm and save'}
        </GovUKButton>
      </>
    );
  };

  const renderGroupedFields = () => {
    // Group fields by their group configuration
    const groups: Record<string, { groupLabel: string; groupCode: string; fields: typeof currentFields }> = {};
    const ungroupedFields: typeof currentFields = [];

    currentFields.forEach((field) => {
      if (field.configuration) {
        try {
          const config = JSON.parse(field.configuration);
          if (config.group) {
            if (!groups[config.group]) {
              groups[config.group] = {
                groupLabel: config.groupLabel || '',
                groupCode: config.group,
                fields: [],
              };
            }
            groups[config.group].fields.push(field);
            if (config.groupLabel) {
              groups[config.group].groupLabel = config.groupLabel;
            }
            return;
          }
        } catch {
          // Ignore parse errors
        }
      }
      ungroupedFields.push(field);
    });

    return (
      <>
        {/* Render ungrouped fields first */}
        {ungroupedFields.length > 0 && (
          <div className="govuk-grid-row">
            <div className="govuk-grid-column-two-thirds">
              {ungroupedFields.map((field) => (
                <div
                  key={field.id}
                  style={{
                    borderBottom: '1px solid #b1b4b6',
                    paddingTop: '20px',
                    paddingBottom: '20px',
                  }}>
                  <FormFieldRenderer field={field} value={fieldValues[field.code]} onChange={(code, value) => handleFieldChange(code, value)} />
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Render grouped fields */}
        {Object.entries(groups).map(([groupKey, group]) => (
          <div key={groupKey} style={{ marginBottom: '10px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', paddingBottom: '15px' }}>
              <span style={{ fontWeight: 'bold', fontSize: '19px', maxWidth: '500px' }}>{group.groupLabel}</span>
              <span style={{ color: '#505a5f', fontSize: '16px', flexShrink: 0 }}>{group.groupCode}</span>
            </div>
            {group.fields.map((field) => {
              const error = fieldErrors[field.code];
              return (
                <div key={field.id} style={{ borderTop: '1px solid #b1b4b6', paddingTop: '15px', paddingBottom: '15px' }}>
                  <div className={error ? 'govuk-form-group govuk-form-group--error' : ''} style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap' }}>
                    <label className="govuk-label" htmlFor={field.code} style={{ marginBottom: 0, width: '500px', flexShrink: 0 }}>
                      — {field.label}
                    </label>
                    <div>
                      {error && (
                        <p className="govuk-error-message" style={{ marginBottom: '5px' }}>
                          <span className="govuk-visually-hidden">Error:</span> {error}
                        </p>
                      )}
                      <input
                        className={`govuk-input ${error ? 'govuk-input--error' : ''}`}
                        id={field.code}
                        name={field.code}
                        type="number"
                        style={{ width: '75px', height: '40px', border: `2px solid ${error ? '#d4351c' : '#0b0c0c'}` }}
                        value={(fieldValues[field.code] as string) || ''}
                        onChange={(e) => handleFieldChange(field.code, e.target.value)}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        ))}
      </>
    );
  };

  const renderForm = () => {
    if (!currentSection || !outcomeScoreForm) return null;

    return (
      <>
        <span className="govuk-caption-l">
          Step {currentStep} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Add an outcome score record</h1>
        <p className="govuk-body" style={{ maxWidth: '600px' }}>
          Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management
          Information data collection.
        </p>

        <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

        {/* Validation Error Summary */}
        {Object.keys(fieldErrors).length > 0 && (
          <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
            <h2 className="govuk-error-summary__title" id="error-summary-title">
              There is a problem
            </h2>
            <div className="govuk-error-summary__body">
              <ul className="govuk-list govuk-error-summary__list">
                {Object.entries(fieldErrors).map(([fieldCode, message]) => (
                  <li key={fieldCode}>
                    <a href={`#${fieldCode}`}>{message}</a>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        )}

        {renderGroupedFields()}

        <div className="govuk-button-group">
          <GovUKButton onClick={handleSaveAndContinue}>{currentStep === sections.length ? 'Continue' : 'Save and continue'}</GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft}>
            Save as draft and exit
          </GovUKButton>
        </div>
      </>
    );
  };

  return (
    <LoadingSpinner loading={isLoading} label="Loading outcome score form">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: dataCollectionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
          { label: 'Outcome scores', link: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores` },
          { label: outcomeScoreForm?.serviceName || 'Service', link: '#' },
        ]}
        currentPage=""
        backLink={{ href: '#', onClick: handleBack }}>
        {viewMode === 'summary' ? renderSummary() : renderForm()}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default OutcomeScoreForm;
