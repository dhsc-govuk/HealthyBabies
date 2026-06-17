import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, SummaryList, FormFieldRenderer } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import { getServiceForm, saveServiceForm, FormModuleFieldDto, FieldConfiguration } from '../queries';
import { ValidationError } from '../validationUtils';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

type ViewMode = 'form' | 'summary';

interface ServiceConditionalRule {
  fundingRequired?: boolean;
  displayInline?: boolean;
  parentOption?: string;
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

const ServiceForm = (): React.ReactElement => {
  usePageTitle('Provide service user data');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, serviceId } = useParams<{
    submissionId: string;
    moduleId: string;
    serviceId: string;
  }>();

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('service_user_submission');

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode>('form');
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [groupErrors, setGroupErrors] = useState<ValidationError[]>([]);
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.keys(fieldErrors).length > 0 || groupErrors.length > 0);

  const { data: formData, isLoading } = useQuery({
    queryKey: ['service-form', submissionId, moduleId, serviceId],
    queryFn: () => getServiceForm(submissionId!, moduleId!, serviceId!),
    enabled: !!submissionId && !!moduleId && !!serviceId,
    onSuccess: (data) => {
      trackStarted();
      const initialValues: Record<string, string | string[] | null> = {};
      data.data.fields.forEach((field) => {
        if (field.fieldType.toLowerCase() === 'checkbox') {
          initialValues[field.code] = field.value ? field.value.split(',') : [];
        } else {
          initialValues[field.code] = field.value;
        }
      });
      setFieldValues(initialValues);
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
      return saveServiceForm(submissionId!, moduleId!, serviceId!, {
        fieldValues: encodedValues,
        markComplete,
      });
    },
    onSuccess: (_data, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['service-users-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['service-form', submissionId, moduleId, serviceId]);
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`, {
        state: { savedServiceName: serviceForm?.serviceName },
      });
    },
  });

  const serviceForm = formData?.data;

  const sections = useMemo(() => {
    if (!serviceForm) return [];
    return [...serviceForm.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [serviceForm]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 2;

  const currentSection = useMemo(() => {
    if (currentStep > sections.length) return null;
    return sections[currentStep - 1];
  }, [sections, currentStep]);

  const parseConditionalRules = (rulesJson: string | null): ServiceConditionalRule | null => {
    if (!rulesJson) return null;
    try {
      return JSON.parse(rulesJson) as ServiceConditionalRule;
    } catch {
      return null;
    }
  };

  const parseConfiguration = (configJson: string | null | undefined): FieldConfiguration | null => {
    if (!configJson) return null;
    try {
      return JSON.parse(configJson) as FieldConfiguration;
    } catch {
      return null;
    }
  };

  // Validate breakdown fields don't exceed total - uses config from database.
  // Handles two cases:
  //   1. Grouped fields (sumGroup): sum of all fields in the group must not exceed maxSumField
  //   2. Single fields (no sumGroup): the individual value must not exceed maxSumField (e.g. QSU04 vs QSU03)
  const validateBreakdownSums = useCallback((): ValidationError[] => {
    if (!serviceForm) return [];
    const errors: ValidationError[] = [];
    const processedGroups = new Set<string>();

    serviceForm.fields.forEach((field) => {
      const config = parseConfiguration(field.configuration);
      if (!config?.maxSumField) return;

      const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
      if (isNaN(maxValue) || maxValue === 0) return;

      if (config.sumGroup) {
        if (processedGroups.has(config.sumGroup)) return;
        processedGroups.add(config.sumGroup);

        const groupFields = serviceForm.fields.filter((f) => {
          const fConfig = parseConfiguration(f.configuration);
          return fConfig?.sumGroup === config.sumGroup;
        });

        let sum = 0;
        groupFields.forEach((f) => {
          const val = parseFloat((fieldValues[f.code] as string) || '0');
          if (!isNaN(val)) sum += val;
        });

        if (sum > maxValue) {
          errors.push({
            fieldCode: groupFields[0]?.code || config.sumGroup,
            message: `The total for ${config.sumGroup} breakdown (${sum}) cannot exceed the total number of service users (${maxValue})`,
          });
        }
      } else {
        const value = parseFloat((fieldValues[field.code] as string) || '0');
        if (isNaN(value) || value === 0) return;

        if (value > maxValue) {
          errors.push({
            fieldCode: field.code,
            message: `This value (${value}) cannot exceed the total number of service users (${maxValue})`,
          });
        }
      }
    });

    return errors;
  }, [serviceForm, fieldValues]);

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

  const isServiceNotDelivered = fieldValues['QSU01'] === 'no';
  const hasUserData = fieldValues['QSU01'] === 'yes' && fieldValues['QSU02'] === 'yes';

  // Determine which steps to show based on QSU01 and QSU02 answers
  // Step 1: QSU01, QSU02 (always shown)
  // Step 2: User numbers (QSU03-QSU06) - only when QSU02=Yes
  // Step 3: Demographics (QSU07-QSU16) - only when QSU02=Yes
  // Step 4: Clarification (QSU17) - shown when QSU01=Yes
  // Step 5: Summary (always shown)
  const shouldSkipUserDataSteps = !hasUserData; // Skip steps 2 and 3 when QSU02=No

  const currentFields = useMemo(() => {
    if (!serviceForm) return [];
    const fields = [...serviceForm.fields].sort((a, b) => a.displayOrder - b.displayOrder);

    if (sections.length === 0) return fields;
    if (!currentSection) return [];
    return fields.filter((f) => f.stepNumber === currentSection.sectionNumber);
  }, [serviceForm, sections, currentSection]);

  // Validate current step fields
  const validateCurrentStep = useCallback((): boolean => {
    if (!serviceForm) return true;
    const errors: Record<string, string> = {};
    const visibleFields = currentFields.filter(isFieldVisible);

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
        if (numValue < 0) {
          errors[field.code] = 'Enter a number 0 or more';
          return;
        }

        const config = parseConfiguration(field.configuration);
        if (config?.min !== undefined && numValue < config.min) {
          errors[field.code] = `Enter a number ${config.min} or more`;
          return;
        }
        if (config?.max !== undefined && numValue > config.max) {
          errors[field.code] = `Enter a number ${config.max} or less`;
          return;
        }
      }
    });

    // Validate breakdown sums from step 2 onwards:
    // step 2 covers single-field checks (QSU04 online <= QSU03 total)
    // step 3 covers grouped demographic checks (ethnicity/IMD/sex sums <= QSU03)
    if (currentStep >= 2) {
      const sumErrors = validateBreakdownSums();
      sumErrors.forEach((err) => {
        errors[err.fieldCode] = err.message;
      });
      setGroupErrors(sumErrors);
    } else {
      setGroupErrors([]);
    }

    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [serviceForm, currentFields, fieldValues, currentStep, isFieldVisible, validateBreakdownSums, trackValidationFailed]);

  const handleFieldChange = (code: string, value: string | string[] | null) => {
    setFieldValues((prev) => ({ ...prev, [code]: value }));
  };

  const handleContinue = () => {
    // Validate before continuing
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }

    // If service not delivered (QSU01=No), skip directly to summary
    if (isServiceNotDelivered && currentStep === 1) {
      trackReviewReached();
      setViewMode('summary');
      return;
    }

    // Calculate next step based on conditional logic
    let nextStep = currentStep + 1;

    // If QSU02=No (no user data), skip Steps 2 and 3, go to Step 4 (Clarification)
    if (shouldSkipUserDataSteps && currentStep === 1) {
      nextStep = 4; // Jump to Step 4 (Clarification)
    }

    // totalSteps includes summary (sections.length + 1), so last form step is totalSteps - 1
    const lastFormStep = totalSteps - 1;

    // If we're on the last form step or next step would exceed form steps, go to summary
    if (currentStep === lastFormStep || nextStep > lastFormStep) {
      trackReviewReached();
      setViewMode('summary');
      return;
    }

    // Otherwise, navigate to the next step
    trackSectionCompleted(String(currentStep));
    setCurrentStep(nextStep);
    window.scrollTo(0, 0);
  };

  const handleBack = () => {
    if (viewMode === 'summary') {
      setViewMode('form');
      // If service not delivered, go back to step 1
      if (isServiceNotDelivered) {
        setCurrentStep(1);
      } else if (shouldSkipUserDataSteps) {
        // If QSU02=No, go back to Step 4 (Clarification)
        setCurrentStep(4);
      } else {
        // Go back to the last form step
        setCurrentStep(sections.length > 0 ? sections.length : 1);
      }
    } else if (currentStep > 1) {
      // Handle back navigation with step skipping
      let prevStep = currentStep - 1;

      // If QSU02=No and we're on Step 4, go back to Step 1
      if (shouldSkipUserDataSteps && currentStep === 4) {
        prevStep = 1;
      }

      setCurrentStep(prevStep);
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`);
    }
  };

  const handleSaveAsDraft = () => {
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    saveMutation.mutate(false);
  };

  const handleConfirmAndSave = () => {
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  const handleChangeAnswer = (stepNumber: number) => {
    setCurrentStep(stepNumber);
    setViewMode('form');
    window.scrollTo(0, 0);
  };

  const getFieldDisplayValue = (field: FormModuleFieldDto): string | React.ReactNode => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';

    if (value === 'n/a') return 'N/A';

    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o) => o.value === v)?.label ?? v).join(', ');
    }

    const option = field.options.find((o) => o.value === value);
    return option?.label ?? value;
  };

  const renderField = (field: FormModuleFieldDto, showQuestionCode = true, isConditionalChild = false) => {
    const value = fieldValues[field.code];

    // Replace placeholders in field label
    const processedField = {
      ...field,
      label: field.label.replace(/\{serviceName\}/g, serviceForm?.serviceName || ''),
    };

    if (value === 'n/a') {
      return (
        <div className="govuk-form-group">
          <label className="govuk-label govuk-label--s">{processedField.label}</label>
          <p className="govuk-body">N/A - Not applicable for non-programme funded services</p>
        </div>
      );
    }

    // Also process all fields for FormFieldRenderer (for inline conditional fields)
    const processedAllFields = (serviceForm?.fields || []).map((f) => ({
      ...f,
      label: f.label.replace(/\{serviceName\}/g, serviceForm?.serviceName || ''),
    }));

    return (
      <FormFieldRenderer
        field={processedField}
        value={fieldValues[field.code] ?? null}
        allFields={processedAllFields}
        allValues={fieldValues}
        allErrors={fieldErrors}
        showQuestionCode={showQuestionCode}
        isConditionalChild={isConditionalChild}
        onChange={handleFieldChange}
      />
    );
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading form" />
      </GeneralLayout>
    );
  }

  if (!serviceForm) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Service not found</h1>
        <p className="govuk-body">The service you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`)}>Back to services</GovUKButton>
      </GeneralLayout>
    );
  }

  if (viewMode === 'summary') {
    const allFields = [...serviceForm.fields].sort((a, b) => a.displayOrder - b.displayOrder);

    const hasFieldValue = (field: FormModuleFieldDto): boolean => {
      const value = fieldValues[field.code];
      if (value === null || value === undefined) return false;
      if (Array.isArray(value)) return value.length > 0;
      return value !== '';
    };

    const isSubAnswerField = (field: FormModuleFieldDto): boolean => {
      const rules = parseConditionalRules(field.conditionalRules);
      const config = field.configuration ? JSON.parse(field.configuration) : null;
      // A field is a sub-answer only if it has displayInline:true or is part of a group
      return !!config?.group || !!rules?.displayInline;
    };

    const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

    // Replace placeholders in field labels for summary display
    const processFieldLabel = (label: string) => label.replace(/\{serviceName\}/g, serviceForm.serviceName || '');

    return (
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">
          Step {totalSteps} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Provide service user data for '{serviceForm.serviceName}'</h1>

        <h2 className="govuk-heading-m">Check your answers before saving your data</h2>
        <p className="govuk-body">
          Check the information you've entered before saving the service user data. Make sure the details are accurate and complete. You can go back to make changes if anything
          needs updating.
        </p>

        {isServiceNotDelivered && (
          <div className="govuk-inset-text">
            <p className="govuk-body">You indicated that this service was not delivered this quarter. No further questions are required.</p>
          </div>
        )}

        <SummaryList
          halfWidthColumns
          noOuterBorder
          items={fieldsWithValues.map((field) => ({
            label: processFieldLabel(field.label),
            value: getFieldDisplayValue(field),
            onAction: () => handleChangeAnswer(field.stepNumber ?? 1),
            actionLabel: 'Change',
            isSubAnswer: isSubAnswerField(field),
          }))}
        />

        <h2 className="govuk-heading-m">Now save your service users data</h2>
        <p className="govuk-body">
          By saving the service user data for '{serviceForm.serviceName}', you are confirming that, to the best of your knowledge, the details you are providing are correct. You
          may return and update these details before the quarterly Management Information submission.
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
    <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
      <div className="service-form__content">
        <span className="govuk-caption-l">
          Step {currentStep} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Provide service user data for '{serviceForm.serviceName}'</h1>

        <p className="govuk-body">
          Tell us about the users who accessed the service over the past 3 months. This information will be included in your quarterly Management Information data collection.
        </p>
      </div>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="service-form__content">
        {/* Validation Error Summary */}
        {(Object.keys(fieldErrors).length > 0 || groupErrors.length > 0) && (
          <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
            <h2 className="govuk-error-summary__title" id="error-summary-title">
              There is a problem
            </h2>
            <div className="govuk-error-summary__body">
              <ul className="govuk-list govuk-error-summary__list">
                {Object.entries(fieldErrors).map(([fieldCode, message]) => {
                  const field = serviceForm?.fields.find((f) => f.code === fieldCode);
                  const summaryMessage = field ? `${field.label} – ${message}` : message;
                  return (
                    <li key={fieldCode}>
                      <a href={`#${fieldCode}`}>{summaryMessage}</a>
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        )}

        {currentFields.length > 0 ? (
          <div>
            {currentFields.map((field) => {
              if (!isFieldVisible(field)) return null;

              const rules = parseConditionalRules(field.conditionalRules);
              const config = field.configuration ? JSON.parse(field.configuration) : null;
              const isGroupedSubField = !!config?.group;

              // Skip inline fields only if their parent is a RADIO on the same step
              // (they will be rendered inside the parent radio's options)
              // Checkbox inline fields are rendered here as regular conditional children for proper question code alignment
              if (rules?.displayInline && rules?.showWhen?.fieldKey) {
                const parentField = serviceForm?.fields.find((f) => f.code === rules.showWhen?.fieldKey);
                const parentFieldType = parentField?.fieldType.toLowerCase();
                const parentIsOnSameStep = parentField && parentField.stepNumber === field.stepNumber;
                // Only skip for radio fields, not checkboxes
                if (parentIsOnSameStep && parentFieldType === 'radio') return null;
              }

              const hasParentDependency = (() => {
                if (!rules?.showWhen) return false;
                // Step 1 fields (QSU01, QSU02) should not trigger grey border styling for Step 2 fields
                const step1Fields = ['QSU01', 'QSU02'];
                if (rules.showWhen.allOf) {
                  return rules.showWhen.allOf.some((condition) => currentFields.some((f) => f.code === condition.fieldKey && !step1Fields.includes(f.code)));
                }
                if (rules.showWhen.fieldKey) {
                  return currentFields.some((f) => f.code === rules.showWhen!.fieldKey && !step1Fields.includes(f.code));
                }
                return false;
              })();

              const showGreyBorder = isGroupedSubField || hasParentDependency;

              return (
                <div key={field.id} className={showGreyBorder ? 'service-form__conditional-field' : ''}>
                  {renderField(field, true, showGreyBorder)}
                </div>
              );
            })}
          </div>
        ) : (
          <div className="govuk-inset-text">
            <p>No form fields have been configured for this section yet.</p>
          </div>
        )}
      </div>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="service-form__content">
        <div className="govuk-button-group">
          <GovUKButton onClick={handleContinue}>Continue</GovUKButton>
          <button type="button" className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} disabled={saveMutation.isLoading}>
            Save as draft and exit
          </button>
        </div>

        {saveMutation.isError && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There was a problem saving</h2>
            <div className="govuk-error-summary__body">
              <p>Please try again or contact support if the problem persists.</p>
            </div>
          </div>
        )}
      </div>
    </GeneralLayout>
  );
};

export default ServiceForm;
