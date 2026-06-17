import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, SummaryList, FormFieldRenderer, GovUKSummaryCard } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import usePageTitle from '../../../../hooks/usePageTitle';
import { getFormModule, saveFormModule, uploadFile, FormModuleFieldDto, ConditionalRule, FieldConfiguration } from '../queries';
import { validateBreastfeedingRate } from '../validationUtils';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';

type ViewMode = 'form' | 'summary' | 'view';

const ViewModule = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode | null>(null);
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.keys(fieldErrors).length > 0);

  const { data: moduleData, isLoading } = useQuery({
    queryKey: ['organisation-admin-module', submissionId, moduleId],
    queryFn: () => getFormModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
    onSuccess: (data) => {
      if (data.data.status.toLowerCase() !== 'completed') {
        trackStarted();
      }
      const initialValues: Record<string, string | string[] | null> = {};
      data.data.fields.forEach((field) => {
        if (field.fieldType.toLowerCase() === 'checkbox') {
          initialValues[field.code] = field.value ? field.value.split(',') : [];
        } else {
          initialValues[field.code] = field.value;
        }
      });
      setFieldValues(initialValues);

      // Set initial view mode based on status
      if (data.data.status.toLowerCase() === 'completed') {
        setViewMode('view');
      } else {
        setViewMode('form');
      }
    },
  });

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
      const encodedValues = Object.fromEntries(
        Object.entries(serializedValues).map(([key, value]) => [key, encodeNullableForWaf(value)])
      );
      return saveFormModule(submissionId!, moduleId!, { fieldValues: encodedValues, markComplete, currentStep });
    },
    onSuccess: (_, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      queryClient.invalidateQueries(['organisation-admin-module', submissionId, moduleId]);
      if (markComplete) {
        navigate(`/organisation-admin/submissions/${submissionId}`, {
          state: {
            successTitle: `${formModule?.name} data saved`,
            successMessage: `${formModule?.name} data has been successfully saved.`,
          },
        });
      } else {
        navigate(`/organisation-admin/submissions/${submissionId}`);
      }
    },
  });

  const formModule = moduleData?.data;

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('module_form', formModule?.name);

  usePageTitle(formModule?.name ?? 'View module');

  const sections = useMemo(() => {
    if (!formModule) return [];
    return [...formModule.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [formModule]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 1;

  const currentSection = useMemo(() => {
    if (currentStep > sections.length) return null;
    return sections[currentStep - 1];
  }, [sections, currentStep]);

  const currentFields = useMemo(() => {
    if (!formModule) return [];
    const fields = [...formModule.fields].sort((a, b) => a.displayOrder - b.displayOrder);
    if (sections.length === 0) return fields;
    if (!currentSection) return [];
    return fields.filter((f) => f.stepNumber === currentSection.sectionNumber);
  }, [formModule, sections, currentSection]);

  const handleFieldChange = (code: string, value: string | string[] | null) => {
    setFieldValues((prev) => ({ ...prev, [code]: value }));
  };

  const handleFileUpload = async (code: string, file: File) => {
    if (!submissionId || !moduleId) return;

    try {
      const response = await uploadFile(submissionId, moduleId, file);
      handleFieldChange(code, response.data.fileName || file.name);
    } catch (error) {
      console.error('File upload error:', error);
    }
  };

  const parseConditionalRules = (rulesJson: string | null): ConditionalRule | null => {
    if (!rulesJson) return null;
    try {
      return JSON.parse(rulesJson) as ConditionalRule;
    } catch {
      return null;
    }
  };

  const isFieldVisible = useCallback(
    (field: FormModuleFieldDto): boolean => {
      const rules = parseConditionalRules(field.conditionalRules);
      if (!rules?.showWhen) return true;

      const { fieldKey, equals, notEquals, in: inValues, allOf } = rules.showWhen;

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

      // Handle allOf conditions
      if (allOf && Array.isArray(allOf)) {
        return allOf.every((condition) => {
          const parentValue = fieldValues[condition.fieldKey];
          if (condition.equals !== undefined) {
            return valueMatches(parentValue, condition.equals);
          }
          if (condition.notEquals !== undefined) {
            return valueNotMatches(parentValue, condition.notEquals);
          }
          return true;
        });
      }

      // Handle simple fieldKey condition
      if (fieldKey) {
        const parentValue = fieldValues[fieldKey];

        if (equals !== undefined) {
          return valueMatches(parentValue, equals);
        }
        if (notEquals !== undefined) {
          return valueNotMatches(parentValue, notEquals);
        }
        if (inValues !== undefined && Array.isArray(inValues)) {
          if (Array.isArray(parentValue)) {
            return inValues.some((v) => parentValue.includes(v));
          }
          return inValues.includes(parentValue as string);
        }
      }

      return true;
    },
    [fieldValues]
  );

  const parseConfiguration = (configJson: string | null | undefined): FieldConfiguration | null => {
    if (!configJson) return null;
    try {
      return JSON.parse(configJson) as FieldConfiguration;
    } catch {
      return null;
    }
  };

  // Validate current step fields
  const validateCurrentStep = useCallback((): boolean => {
    if (!formModule) return true;
    const errors: Record<string, string> = {};
    const visibleFields = currentFields.filter(isFieldVisible);
    const processedSumGroups = new Set<string>();

    visibleFields.forEach((field) => {
      const value = fieldValues[field.code];
      const stringValue = typeof value === 'string' ? value : '';
      const fieldTypeLower = field.fieldType.toLowerCase();
      const isCheckboxEmpty = fieldTypeLower === 'checkbox' && Array.isArray(value) && value.length === 0;
      const isEmpty = fieldTypeLower === 'checkbox' ? isCheckboxEmpty : !stringValue;

      // Required field check
      if (field.isRequired && isEmpty) {
        if (fieldTypeLower === 'radio' || fieldTypeLower === 'select') {
          errors[field.code] = 'Select an option';
        } else if (fieldTypeLower === 'checkbox') {
          errors[field.code] = 'Select at least one option';
        } else {
          errors[field.code] = 'Enter a value';
        }
        return;
      }

      // Skip non-required empty fields
      if (isEmpty) return;

      // Validate number fields
      if (fieldTypeLower === 'number' && stringValue) {
        const numValue = parseFloat(stringValue);
        if (isNaN(numValue)) {
          errors[field.code] = 'Enter a valid number';
          return;
        }

        const config = parseConfiguration(field.configuration);

        // Check for percentage fields (breastfeeding rates)
        if (config?.suffix === '%') {
          const error = validateBreastfeedingRate(stringValue);
          if (error) {
            errors[field.code] = error;
            return;
          }
        }

        // Check min/max from configuration
        if (config?.min !== undefined && numValue < config.min) {
          errors[field.code] = `Enter a number ${config.min} or more`;
          return;
        }
        if (config?.max !== undefined && numValue > config.max) {
          errors[field.code] = `Enter a number ${config.max} or less`;
          return;
        }

        // Default: ensure non-negative for number fields
        if (numValue < 0) {
          errors[field.code] = 'Enter a number 0 or more';
          return;
        }

        // Sum group validation
        if (config?.sumGroup && config?.maxSumField && !processedSumGroups.has(config.sumGroup)) {
          processedSumGroups.add(config.sumGroup);

          // Get the max value from the reference field
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0) {
            // Find all visible fields in this sum group
            const groupFields = visibleFields.filter((f) => {
              const fConfig = parseConfiguration(f.configuration);
              return fConfig?.sumGroup === config.sumGroup;
            });

            // Calculate the sum
            const sum = groupFields.reduce((acc, f) => {
              const val = parseFloat((fieldValues[f.code] as string) || '0');
              return acc + (isNaN(val) ? 0 : val);
            }, 0);

            // Check if sum exceeds max
            if (sum > maxValue) {
              const groupLabel = config.groupLabel || config.sumGroup;
              groupFields.forEach((f) => {
                const fVal = parseFloat((fieldValues[f.code] as string) || '0');
                if (fVal > 0) {
                  errors[f.code] = `The total for '${groupLabel}' (${sum}) cannot exceed the total (${maxValue})`;
                }
              });
            }
          }
        }
        // Single field max validation (for fields like QSU14 that have maxSumField but no sumGroup)
        else if (config?.maxSumField && !config?.sumGroup) {
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0 && numValue > maxValue) {
            errors[field.code] = `This value (${numValue}) cannot exceed the total number of service users (${maxValue})`;
          }
        }
      }
    });

    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, currentFields, fieldValues, isFieldVisible, trackValidationFailed]);

  const handleContinue = () => {
    // Validate before continuing
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }

    if (currentStep === totalSteps - 1) {
      trackReviewReached();
      setViewMode('summary');
    } else if (currentStep < totalSteps) {
      trackSectionCompleted(String(currentStep));
      setCurrentStep(currentStep + 1);
      window.scrollTo(0, 0);
    }
  };

  const handleBack = () => {
    if (viewMode === 'view') {
      navigate(`/organisation-admin/submissions/${submissionId}`);
    } else if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(`/organisation-admin/submissions/${submissionId}`);
    }
  };

  const handleSaveAsDraft = () => {
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    saveMutation.mutate(false);
  };

  // Validate all fields across all sections (for summary page validation)
  const validateAllFields = useCallback((): boolean => {
    if (!formModule) return true;
    const errors: Record<string, string> = {};
    const allFields = formModule.fields.sort((a, b) => a.displayOrder - b.displayOrder);
    const visibleFields = allFields.filter(isFieldVisible);
    const processedSumGroups = new Set<string>();

    visibleFields.forEach((field) => {
      const value = fieldValues[field.code];
      const stringValue = typeof value === 'string' ? value : '';

      // Validate number fields with sum group
      if (field.fieldType.toLowerCase() === 'number' && stringValue) {
        const config = parseConfiguration(field.configuration);

        // Sum group validation
        if (config?.sumGroup && config?.maxSumField && !processedSumGroups.has(config.sumGroup)) {
          processedSumGroups.add(config.sumGroup);

          // Get the max value from the reference field
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0) {
            // Find all visible fields in this sum group
            const groupFields = visibleFields.filter((f) => {
              const fConfig = parseConfiguration(f.configuration);
              return fConfig?.sumGroup === config.sumGroup;
            });

            // Calculate the sum
            const sum = groupFields.reduce((acc, f) => {
              const val = parseFloat((fieldValues[f.code] as string) || '0');
              return acc + (isNaN(val) ? 0 : val);
            }, 0);

            // Check if sum exceeds max
            if (sum > maxValue) {
              const groupLabel = config.groupLabel || config.sumGroup;
              groupFields.forEach((f) => {
                const fVal = parseFloat((fieldValues[f.code] as string) || '0');
                if (fVal > 0) {
                  errors[f.code] = `The total for '${groupLabel}' (${sum}) cannot exceed the total (${maxValue})`;
                }
              });
            }
          }
        }
        // Single field max validation (for fields like QSU14 that have maxSumField but no sumGroup)
        else if (config?.maxSumField && !config?.sumGroup) {
          const numValue = parseFloat(stringValue);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(numValue) && !isNaN(maxValue) && maxValue > 0 && numValue > maxValue) {
            errors[field.code] = `This value (${numValue}) cannot exceed the total number of service users (${maxValue})`;
          }
        }
      }
    });

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, fieldValues, isFieldVisible]);

  const handleConfirmAndSave = () => {
    // Validate all fields before saving
    if (!validateAllFields()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  const handleChangeAnswer = (stepNumber: number) => {
    setCurrentStep(stepNumber);
    setViewMode('form');
    window.scrollTo(0, 0);
  };

  const handleChangeFromView = () => {
    setCurrentStep(1);
    setViewMode('form');
    window.scrollTo(0, 0);
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  };

  const getFieldDisplayValue = (field: FormModuleFieldDto): string | React.ReactNode => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';

    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o) => o.value === v)?.label ?? v).join(', ');
    }

    if (field.fieldType.toLowerCase() === 'file' && value) {
      return <span className="govuk-link">{value}</span>;
    }

    const option = field.options.find((o) => o.value === value);
    return option?.label ?? value;
  };

  const renderField = (field: FormModuleFieldDto, showQuestionCode = true, isConditionalChild = false) => {
    return (
      <FormFieldRenderer
        field={field}
        value={fieldValues[field.code] ?? null}
        allFields={formModule?.fields || []}
        allValues={fieldValues}
        allErrors={fieldErrors}
        showQuestionCode={showQuestionCode}
        isConditionalChild={isConditionalChild}
        onChange={handleFieldChange}
        onFileUpload={handleFileUpload}
      />
    );
  };

  if (isLoading || viewMode === null) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading form" />
      </GeneralLayout>
    );
  }

  if (!formModule) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Form not found</h1>
        <p className="govuk-body">The form you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}`)}>Back to submission</GovUKButton>
      </GeneralLayout>
    );
  }

  const allFields = [...formModule.fields].sort((a, b) => a.displayOrder - b.displayOrder);

  const hasFieldValue = (field: FormModuleFieldDto): boolean => {
    const value = fieldValues[field.code];
    if (value === null || value === undefined) return false;
    if (Array.isArray(value)) return value.length > 0;
    return value !== '';
  };

  const isSubAnswerField = (field: FormModuleFieldDto): boolean => {
    const rules = parseConditionalRules(field.conditionalRules);
    // A field is a sub-answer if it has displayInline: true (grouped under a parent radio)
    return rules?.displayInline === true;
  };

  const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

  if (viewMode === 'view') {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: formModule.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
        ]}
        currentPage={formModule.name}
        backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">Section {formModule.sectionNumber}</span>

        <p className="govuk-body">
          This page shows saved information you've provided about {formModule.name.toLowerCase()} from {formatDate(formModule.startDate)} to {formatDate(formModule.endDate)}.
        </p>
        <p className="govuk-body">
          You can update {formModule.name.toLowerCase()} data at any time before submitting the quarterly Management Information data collection. To update the information, select
          'Change'. To delete {formModule.name.toLowerCase()} data, select 'Delete'.
        </p>

        <GovUKSummaryCard
          title={
            <>
              <strong>{formModule.submissionName}</strong>. {formatDate(formModule.startDate)}–{formatDate(formModule.endDate)}
            </>
          }
          actions={[
            { label: 'Change', onClick: handleChangeFromView },
            { label: 'Delete', onClick: () => navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/delete`) },
          ]}>
          <SummaryList
            noOuterBorder
            halfWidthColumns
            items={fieldsWithValues.map((field) => ({
              label: field.label,
              value: getFieldDisplayValue(field),
              isSubAnswer: isSubAnswerField(field),
            }))}
          />
        </GovUKSummaryCard>
      </GeneralLayout>
    );
  }

  if (viewMode === 'summary') {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: formModule.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
        ]}
        currentPage={formModule.name}
        backLink={{ href: '#', onClick: handleBack }}>
        {/* Validation Error Summary */}
        {Object.keys(fieldErrors).length > 0 && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There is a problem</h2>
            <div className="govuk-error-summary__body">
              <ul className="govuk-list govuk-error-summary__list">
                {Object.entries(fieldErrors).map(([fieldCode, errorMessage]) => {
                  const field = formModule.fields.find((f) => f.code === fieldCode);
                  const summaryMessage = field ? `${field.label} – ${errorMessage}` : errorMessage;
                  return (
                    <li key={fieldCode}>
                      <a
                        href="#"
                        onClick={(e) => {
                          e.preventDefault();
                          handleChangeAnswer(field?.stepNumber ?? 1);
                        }}>
                        {summaryMessage}
                      </a>
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        )}

        <span className="govuk-caption-l">
          Step {totalSteps} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Provide data for {formModule.name}</h1>

        <h2 className="govuk-heading-m">Check your answers before saving your data</h2>
        <p className="govuk-body">
          Check the information you've entered before saving the {formModule.name} user data. Make sure the details are accurate and complete. You can go back to make changes if
          anything needs updating.
        </p>

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => ({
            label: field.label,
            value: getFieldDisplayValue(field),
            onAction: () => handleChangeAnswer(field.stepNumber ?? 1),
            actionLabel: 'Change',
            isSubAnswer: isSubAnswerField(field),
          }))}
        />

        <h2 className="govuk-heading-m">Now save your {formModule.name} data</h2>
        <p className="govuk-body">
          By saving the {formModule.name} data, you are confirming that, to the best of your knowledge, the details you are providing are correct. You may return and update these
          details before the quarterly Management Information submission.
        </p>

        <GovUKButton onClick={handleConfirmAndSave} isLoading={saveMutation.isLoading}>
          Confirm and save
        </GovUKButton>

        {saveMutation.isError && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There was a problem saving</h2>
            <div className="govuk-error-summary__body">
              <p>Please try again or contact support if the problem persists.</p>
            </div>
          </div>
        )}
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
        { label: formModule.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
      ]}
      currentPage={formModule.name}
      backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">
        Step {currentStep} of {totalSteps}
      </span>

      {formModule.description && <p className="govuk-body">{formModule.description}</p>}

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

      {currentFields.length > 0 ? (
        <div>
          {currentFields.map((field) => {
            if (!isFieldVisible(field)) return null;

            const rules = parseConditionalRules(field.conditionalRules);
            const isInlineField = rules?.displayInline === true;

            // Skip inline fields only if their parent is a RADIO on the same step
            // (they will be rendered inside the parent radio's options)
            // Checkbox inline fields are rendered here as regular conditional children for proper question code alignment
            if (isInlineField && rules?.showWhen?.fieldKey) {
              const parentField = formModule?.fields.find((f) => f.code === rules.showWhen?.fieldKey);
              const parentFieldType = parentField?.fieldType.toLowerCase();
              const parentIsOnSameStep = parentField && parentField.stepNumber === field.stepNumber;
              // Only skip for radio fields, not checkboxes
              if (parentIsOnSameStep && parentFieldType === 'radio') return null;
            }

            // Only apply conditional child styling (grey border) to fields with displayInline: true
            // Fields without displayInline should be standalone even if they have showWhen rules
            const isConditionalChild = !!rules?.displayInline && !!rules?.showWhen;

            return (
              <div
                key={field.id}
                style={{
                  marginLeft: isConditionalChild ? '33px' : 0,
                  paddingLeft: isConditionalChild ? '15px' : 0,
                  borderLeft: isConditionalChild ? '4px solid #b1b4b6' : 'none',
                }}>
                {renderField(field, true, isConditionalChild)}
              </div>
            );
          })}
        </div>
      ) : (
        <div className="govuk-inset-text">
          <p>No form fields have been configured for this section yet.</p>
        </div>
      )}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleContinue}>Continue</GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} isLoading={saveMutation.isLoading}>
          Save as draft and exit
        </GovUKButton>
      </div>

      {saveMutation.isError && (
        <div className="govuk-error-summary" data-module="govuk-error-summary">
          <h2 className="govuk-error-summary__title">There was a problem saving</h2>
          <div className="govuk-error-summary__body">
            <p>Please try again or contact support if the problem persists.</p>
          </div>
        </div>
      )}
    </GeneralLayout>
  );
};

export default ViewModule;
