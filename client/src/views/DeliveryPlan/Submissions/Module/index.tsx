import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKNotificationBanner, SummaryList, FormFieldRenderer } from '../../../../components/GovUKComponents';
import { computeFormula } from '../../../../components/GovUKComponents/FormFieldRenderer';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import usePageTitle from '../../../../hooks/usePageTitle';
import { getDeliveryPlanFormModule, saveDeliveryPlanFormModule, DeliveryPlanFieldDto, DeliveryPlanFieldOptionDto } from '../queries';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { evaluateNumericOperators } from '../../../../helpers/conditionalOperators';
import { ConditionalRule, FieldConfiguration } from '../../../OrganisationAdmin/Submissions/queries';

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
    queryKey: ['delivery-plan-module', submissionId, moduleId],
    queryFn: () => getDeliveryPlanFormModule(submissionId!, moduleId!),
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
      return saveDeliveryPlanFormModule(submissionId!, moduleId!, { fieldValues: encodedValues, markComplete, currentStep });
    },
    onSuccess: (_, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['delivery-plan-submission', submissionId]);
      queryClient.invalidateQueries(['delivery-plan-module', submissionId, moduleId]);
      if (markComplete) {
        navigate(`/delivery-plan/submissions/${submissionId}`, {
          state: {
            successTitle: `${formModule?.name} data saved`,
            successMessage: `${formModule?.name} data has been successfully saved.`,
          },
        });
      } else {
        navigate(`/delivery-plan/submissions/${submissionId}`);
      }
    },
  });

  const formModule = moduleData?.data;

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('module_form', formModule?.name);

  usePageTitle(formModule?.name ?? 'View module', Object.keys(fieldErrors).length > 0);

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

  const parseConditionalRules = (rulesJson: string | null): ConditionalRule | null => {
    if (!rulesJson) return null;
    try {
      return JSON.parse(rulesJson) as ConditionalRule;
    } catch {
      return null;
    }
  };

  const isFieldVisible = useCallback(
    (field: DeliveryPlanFieldDto): boolean => {
      const rules = parseConditionalRules(field.conditionalRules);
      if (!rules?.showWhen) return true;

      const { fieldKey, equals, notEquals, in: inValues, allOf } = rules.showWhen;

      const valueMatches = (parentValue: string | string[] | null, targetValue: string): boolean => {
        if (Array.isArray(parentValue)) return parentValue.includes(targetValue);
        return parentValue === targetValue;
      };

      const valueNotMatches = (parentValue: string | string[] | null, targetValue: string): boolean => {
        if (Array.isArray(parentValue)) return !parentValue.includes(targetValue);
        return parentValue !== targetValue;
      };

      if (allOf && Array.isArray(allOf)) {
        return allOf.every((condition) => {
          const parentValue = fieldValues[condition.fieldKey];
          const numeric = evaluateNumericOperators(parentValue, condition);
          if (numeric !== null) return numeric;
          if (condition.equals !== undefined) return valueMatches(parentValue, condition.equals);
          if (condition.notEquals !== undefined) return valueNotMatches(parentValue, condition.notEquals);
          return true;
        });
      }

      if (fieldKey) {
        const parentValue = fieldValues[fieldKey];
        const numeric = evaluateNumericOperators(parentValue, rules.showWhen);
        if (numeric !== null) return numeric;
        if (equals !== undefined) return valueMatches(parentValue, equals);
        if (notEquals !== undefined) return valueNotMatches(parentValue, notEquals);
        if (inValues !== undefined && Array.isArray(inValues)) {
          if (Array.isArray(parentValue)) return inValues.some((v) => parentValue.includes(v));
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

  // Keep calculation field values in sync whenever any input changes
  useEffect(() => {
    if (!formModule) return;
    const calculationFields = formModule.fields.filter((f) => f.fieldType.toLowerCase() === 'calculation');
    if (calculationFields.length === 0) return;

    setFieldValues((prev) => {
      const updates: Record<string, string | null> = {};
      calculationFields.forEach((field) => {
        const config = parseConfiguration(field.configuration);
        const formula = config?.formula ?? '';
        const result = computeFormula(formula, prev);
        const newVal = result !== null ? String(result) : '';
        if (prev[field.code] !== newVal) {
          updates[field.code] = newVal;
        }
      });
      if (Object.keys(updates).length === 0) return prev;
      return { ...prev, ...updates };
    });
  }, [formModule, fieldValues]); // eslint-disable-line react-hooks/exhaustive-deps

  const validateCurrentStep = useCallback((): boolean => {
    if (!formModule) return true;
    const errors: Record<string, string> = {};
    const visibleFields = currentFields.filter(isFieldVisible);
    const processedSumGroups = new Set<string>();

    visibleFields.forEach((field) => {
      const fieldTypeLower = field.fieldType.toLowerCase();

      // Skip non-user-input field types
      if (fieldTypeLower === 'calculation' || fieldTypeLower === 'readonly') return;

      const value = fieldValues[field.code];
      const stringValue = typeof value === 'string' ? value : '';
      const isCheckboxEmpty = fieldTypeLower === 'checkbox' && Array.isArray(value) && value.length === 0;
      const isEmpty = fieldTypeLower === 'checkbox' ? isCheckboxEmpty : !stringValue;

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

      if (isEmpty) return;

      if ((fieldTypeLower === 'number' || fieldTypeLower === 'currency') && stringValue) {
        const numValue = parseFloat(stringValue);
        if (isNaN(numValue)) {
          errors[field.code] = 'Enter a valid number';
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
        if (numValue < 0) {
          errors[field.code] = 'Enter a number 0 or more';
          return;
        }

        if (config?.sumGroup && config?.maxSumField && !processedSumGroups.has(config.sumGroup)) {
          processedSumGroups.add(config.sumGroup);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0) {
            const groupFields = visibleFields.filter((f) => {
              const fConfig = parseConfiguration(f.configuration);
              return fConfig?.sumGroup === config.sumGroup;
            });
            const sum = groupFields.reduce((acc, f) => {
              const val = parseFloat((fieldValues[f.code] as string) || '0');
              return acc + (isNaN(val) ? 0 : val);
            }, 0);
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
        } else if (config?.maxSumField && !config?.sumGroup) {
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0 && numValue > maxValue) {
            errors[field.code] = `This value (${numValue}) cannot exceed the total (${maxValue})`;
          }
        }
      }
    });

    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, currentFields, fieldValues, isFieldVisible, trackValidationFailed]);

  const handleContinue = () => {
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
      navigate(`/delivery-plan/submissions/${submissionId}`);
    } else if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(`/delivery-plan/submissions/${submissionId}`);
    }
  };

  const handleSaveAsDraft = () => {
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    saveMutation.mutate(false);
  };

  const validateAllFields = useCallback((): boolean => {
    if (!formModule) return true;
    const errors: Record<string, string> = {};
    const allFields = formModule.fields.sort((a, b) => a.displayOrder - b.displayOrder);
    const visibleFields = allFields.filter(isFieldVisible);
    const processedSumGroups = new Set<string>();

    visibleFields.forEach((field) => {
      const value = fieldValues[field.code];
      const stringValue = typeof value === 'string' ? value : '';

      if (field.fieldType.toLowerCase() === 'number' && stringValue) {
        const config = parseConfiguration(field.configuration);

        if (config?.sumGroup && config?.maxSumField && !processedSumGroups.has(config.sumGroup)) {
          processedSumGroups.add(config.sumGroup);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0) {
            const groupFields = visibleFields.filter((f) => {
              const fConfig = parseConfiguration(f.configuration);
              return fConfig?.sumGroup === config.sumGroup;
            });
            const sum = groupFields.reduce((acc, f) => {
              const val = parseFloat((fieldValues[f.code] as string) || '0');
              return acc + (isNaN(val) ? 0 : val);
            }, 0);
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
        } else if (config?.maxSumField && !config?.sumGroup) {
          const numValue = parseFloat(stringValue);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(numValue) && !isNaN(maxValue) && maxValue > 0 && numValue > maxValue) {
            errors[field.code] = `This value (${numValue}) cannot exceed the total (${maxValue})`;
          }
        }
      }
    });

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, fieldValues, isFieldVisible]);

  const handleConfirmAndSave = () => {
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


  const getFieldDisplayValue = (field: DeliveryPlanFieldDto): string => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';
    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o: DeliveryPlanFieldOptionDto) => o.value === v)?.label ?? v).join(', ');
    }
    const fieldTypeLower = field.fieldType.toLowerCase();
    if (fieldTypeLower === 'currency' || fieldTypeLower === 'calculation') {
      const num = parseFloat(value as string);
      return isNaN(num) ? (value as string) : `£${num.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    const option = field.options.find((o: DeliveryPlanFieldOptionDto) => o.value === value);
    return option?.label ?? (value as string);
  };

  const hasFieldValue = (field: DeliveryPlanFieldDto): boolean => {
    const value = fieldValues[field.code];
    if (value === null || value === undefined) return false;
    if (Array.isArray(value)) return value.length > 0;
    return value !== '';
  };

  const isSubAnswerField = (field: DeliveryPlanFieldDto): boolean => {
    const rules = parseConditionalRules(field.conditionalRules);
    return rules?.displayInline === true;
  };

  const renderField = (field: DeliveryPlanFieldDto, isConditionalChild = false) => (
    <FormFieldRenderer
      field={field}
      value={fieldValues[field.code] ?? null}
      allFields={formModule?.fields || []}
      allValues={fieldValues}
      allErrors={fieldErrors}
      showQuestionCode
      isConditionalChild={isConditionalChild}
      onChange={handleFieldChange}
      onFileUpload={() => Promise.resolve()}
    />
  );

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
        <GovUKButton onClick={() => navigate(`/delivery-plan/submissions/${submissionId}`)}>Back to submission</GovUKButton>
      </GeneralLayout>
    );
  }

  const allFields = [...formModule.fields].sort((a, b) => a.displayOrder - b.displayOrder);
  const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

  if (viewMode === 'view') {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/delivery-plan/home' },
          { label: 'Submissions', link: '/delivery-plan/submissions' },
          { label: formModule.submissionName || 'Submission', link: `/delivery-plan/submissions/${submissionId}` },
        ]}
        currentPage={formModule.name}
        backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">Section {formModule.sectionNumber}</span>
        <p className="govuk-body">
          This page shows saved information you&apos;ve provided about {formModule.name.toLowerCase()}.
        </p>

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => {
            const isReadOnlyType = field.fieldType.toLowerCase() === 'readonly' || field.fieldType.toLowerCase() === 'calculation';
            return {
              label: field.label,
              value: getFieldDisplayValue(field),
              onAction: isReadOnlyType ? undefined : handleChangeFromView,
              actionLabel: isReadOnlyType ? undefined : 'Change',
              isSubAnswer: isSubAnswerField(field),
            };
          })}
        />
      </GeneralLayout>
    );
  }

  if (viewMode === 'summary') {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/delivery-plan/home' },
          { label: 'Submissions', link: '/delivery-plan/submissions' },
          { label: formModule.submissionName || 'Submission', link: `/delivery-plan/submissions/${submissionId}` },
        ]}
        currentPage={formModule.name}
        backLink={{ href: '#', onClick: handleBack }}>
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

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => {
            const isReadOnlyType = field.fieldType.toLowerCase() === 'readonly' || field.fieldType.toLowerCase() === 'calculation';
            return {
              label: field.label,
              value: getFieldDisplayValue(field),
              onAction: isReadOnlyType ? undefined : () => handleChangeAnswer(field.stepNumber ?? 1),
              actionLabel: isReadOnlyType ? undefined : 'Change',
              isSubAnswer: isSubAnswerField(field),
            };
          })}
        />

        <h2 className="govuk-heading-m">Now save your {formModule.name} data</h2>
        <p className="govuk-body">
          By saving, you confirm that the details you are providing are correct to the best of your knowledge.
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
        { label: 'Home', link: '/delivery-plan/home' },
        { label: 'Submissions', link: '/delivery-plan/submissions' },
        { label: formModule.submissionName || 'Submission', link: `/delivery-plan/submissions/${submissionId}` },
      ]}
      currentPage={formModule.name}
      backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">
        Step {currentStep} of {totalSteps}
      </span>

      {formModule.description && <p className="govuk-body">{formModule.description}</p>}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      {Object.keys(fieldErrors).length > 0 && (
        <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
          <h2 className="govuk-error-summary__title" id="error-summary-title">
            There is a problem
          </h2>
          <div className="govuk-error-summary__body">
            <ul className="govuk-list govuk-error-summary__list">
              {Object.entries(fieldErrors).map(([fieldCode, message]) => {
                const field = formModule.fields.find((f) => f.code === fieldCode);
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
            const isInlineField = rules?.displayInline === true;

            if (isInlineField && rules?.showWhen?.fieldKey) {
              const parentField = formModule?.fields.find((f) => f.code === rules.showWhen?.fieldKey);
              const parentFieldType = parentField?.fieldType.toLowerCase();
              const parentIsOnSameStep = parentField && parentField.stepNumber === field.stepNumber;
              if (parentIsOnSameStep && parentFieldType === 'radio') return null;
            }

            const isConditionalChild = !!rules?.displayInline && !!rules?.showWhen;

            const fieldConfig = parseConfiguration(field.configuration);
            const fieldVal = parseFloat((fieldValues[field.code] as string) || '0');
            const showOverspendWarning =
              field.fieldType.toLowerCase() === 'calculation' &&
              fieldConfig?.warnIfNegative === true &&
              !isNaN(fieldVal) &&
              fieldVal < 0;

            return (
              <React.Fragment key={field.id}>
                <div
                  style={{
                    marginLeft: isConditionalChild ? '33px' : 0,
                    paddingLeft: isConditionalChild ? '15px' : 0,
                    borderLeft: isConditionalChild ? '4px solid #b1b4b6' : 'none',
                  }}>
                  {renderField(field, isConditionalChild)}
                </div>
                {showOverspendWarning && (
                  <GovUKNotificationBanner type="important" title="Warning">
                    <p className="govuk-body">
                      <strong>{field.label}</strong> is showing a negative value (£
                      {Math.abs(fieldVal).toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} overspend).
                      Please review your funding figures.
                    </p>
                  </GovUKNotificationBanner>
                )}
              </React.Fragment>
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
