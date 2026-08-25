import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKNotificationBanner, SummaryList, FormFieldRenderer, GovUKErrorSummary } from '../../../../components/GovUKComponents';
import { computeFormula } from '../../../../components/GovUKComponents/FormFieldRenderer';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import usePageTitle from '../../../../hooks/usePageTitle';
import useStepFocus from '../../../../hooks/useStepFocus';
import { getDeliveryPlanFormModule, saveDeliveryPlanFormModule, DeliveryPlanFieldDto, DeliveryPlanFieldOptionDto } from '../queries';
import { resolveSnapshotLabel } from '../../../../helpers/answerSnapshot';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { evaluateNumericOperators } from '../../../../helpers/conditionalOperators';
import { ConditionalRule, FieldConfiguration } from '../../../OrganisationAdmin/Submissions/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import focusErrorTarget from '../../../../helpers/focusErrorTarget';
import { ROUTES } from '../../../../constants/routes';

type ViewMode = 'form' | 'summary' | 'view';

const ViewModule = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  // Once the round is submitted (or its window has closed) the plan is a record, not a form.
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode | null>(null);
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);
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

  const isReadOnly = moduleData?.data?.isReadOnly === true;

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
        navigate(ROUTES.deliveryPlan.submissions.view(submissionId), {
          state: {
            successTitle: `${formModule?.name} data saved`,
            successMessage: `${formModule?.name} data has been successfully saved.`,
          },
        });
      } else {
        navigate(ROUTES.deliveryPlan.submissions.view(submissionId), {
          state: {
            successTitle: `${formModule?.name} draft saved`,
            successMessage: `Your progress on ${formModule?.name} has been saved as a draft.`,
          },
        });
      }
    },
    // BLUM-7520: surface the server's reason (e.g. the quarter is locked) rather than only the
    // generic fallback, matching the organisation-admin module page.
    onError: (error) => {
      processError(error, setSaveError);
    },
  });

  // BLUM-7520: reset() unmounts the save-error summary, which both clears a stale error before a
  // fresh attempt and lets GovUKErrorSummary re-run its mount focus effect on a repeat failure.
  // Skipped while a save is in flight: reset() drops currentMutation without detaching the
  // observer, so that save would later set saveError while isError stayed false — the failure
  // would be swallowed with no summary shown. Nothing to clear mid-flight anyway.
  const clearSaveError = () => {
    if (saveMutation.isLoading) return;
    saveMutation.reset();
    setSaveError(undefined);
  };

  const formModule = moduleData?.data;

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('module_form', formModule?.name);

  const sections = useMemo(() => {
    if (!formModule) return [];
    return [...formModule.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [formModule]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 1;

  // Step count is dynamic (one per section, plus the check-answers step), so derive the
  // caption from totalSteps rather than hard-coding it.
  const stepLabel =
    viewMode === 'view'
      ? undefined
      : viewMode === 'summary'
        ? `Step ${totalSteps} of ${totalSteps}: Check your answers`
        : `Step ${currentStep} of ${totalSteps}`;
  usePageTitle(formModule?.name ?? 'View module', Object.keys(fieldErrors).length > 0, stepLabel);

  // Keyed on viewMode too, so moving from the last section to the check-answers step also
  // moves focus (WCAG 2.4.3).
  const stepFocusRef = useStepFocus<HTMLDivElement>(`${viewMode}-${currentStep}`);

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

  // Shared by validateCurrentStep (per-step, on Continue and Save as draft and exit) and
  // validateAllFields (every field, on Confirm and save) so the two can't drift — validateAllFields
  // used to check only sumGroup limits, so a required field left blank and skipped via a direct
  // "Confirm and save" was never caught (BLUM-7053).
  const validateFields = useCallback(
    (fields: DeliveryPlanFieldDto[]): Record<string, string> => {
      const errors: Record<string, string> = {};
      const processedSumGroups = new Set<string>();

      fields.forEach((field) => {
        const fieldTypeLower = field.fieldType.toLowerCase();

        // Skip non-user-input field types
        if (fieldTypeLower === 'calculation' || fieldTypeLower === 'read_only') return;

        const value = fieldValues[field.code];
        const stringValue = typeof value === 'string' ? value : '';
        const isCheckboxEmpty = fieldTypeLower === 'checkbox' && Array.isArray(value) && value.length === 0;
        const isEmpty = fieldTypeLower === 'checkbox' ? isCheckboxEmpty : !stringValue;

        // A field revealed by a conditional rule is validated like any other: the caller has already
        // filtered to visible fields, so isRequired only bites once the controlling answer reveals it.
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
              const groupFields = fields.filter((f) => {
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

      return errors;
    },
    [fieldValues]
  );

  const validateCurrentStep = useCallback((): boolean => {
    if (!formModule) return true;
    const errors = validateFields(currentFields.filter(isFieldVisible));
    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, validateFields, currentFields, isFieldVisible, trackValidationFailed]);

  const handleContinue = () => {
    // Clear any previous save failure first — the summary now sits above the field-error summary,
    // so leaving a stale one there would steal the focus that belongs to the validation errors
    // raised just below (focusErrorSummary takes the first match).
    clearSaveError();
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
    // Leaving the step that failed — drop the summary, otherwise it renders at the top of the
    // next step claiming a save failed there.
    clearSaveError();
    if (viewMode === 'view') {
      navigate(ROUTES.deliveryPlan.submissions.view(submissionId));
    } else if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(ROUTES.deliveryPlan.submissions.view(submissionId));
    }
  };

  const handleSaveAsDraft = () => {
    clearSaveError();
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    saveMutation.mutate(false);
  };

  const validateAllFields = useCallback((): boolean => {
    if (!formModule) return true;
    const allFields = [...formModule.fields].sort((a, b) => a.displayOrder - b.displayOrder);
    const errors = validateFields(allFields.filter(isFieldVisible));
    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formModule, validateFields, isFieldVisible, trackValidationFailed]);

  // The summary on a form step must list only that step's errors. validateAllFields reports every
  // step at once, and handleChangeAnswer navigates without clearing fieldErrors, so an unfiltered
  // list renders entries for fields that aren't on this page — their "#code" links resolve to
  // nothing and silently do nothing when clicked.
  const currentStepErrors = useMemo(() => {
    const visibleCodes = new Set(currentFields.filter(isFieldVisible).map((f) => f.code));
    return Object.entries(fieldErrors).filter(([fieldCode]) => visibleCodes.has(fieldCode));
  }, [fieldErrors, currentFields, isFieldVisible]);

  const handleConfirmAndSave = () => {
    clearSaveError();
    if (!validateAllFields()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  const handleChangeAnswer = (stepNumber: number) => {
    clearSaveError();
    setCurrentStep(stepNumber);
    setViewMode('form');
    window.scrollTo(0, 0);
  };

  const handleChangeFromView = () => {
    clearSaveError();
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
        <GovUKButton onClick={() => navigate(ROUTES.deliveryPlan.submissions.view(submissionId))}>Back to submission</GovUKButton>
      </GeneralLayout>
    );
  }

  const allFields = [...formModule.fields].sort((a, b) => a.displayOrder - b.displayOrder);
  const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

  if (viewMode === 'view') {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.deliveryPlan.home },
          { label: 'Submissions', link: ROUTES.deliveryPlan.submissions.list },
          { label: formModule.submissionName || 'Submission', link: ROUTES.deliveryPlan.submissions.view(submissionId) },
        ]}
        currentPage={formModule.name}
        manageTitle={false}
        backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">Section {formModule.sectionNumber}</span>
        <p className="govuk-body">
          This page shows saved information you&apos;ve provided about {formModule.name.toLowerCase()}.
        </p>

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => {
            const isReadOnlyType = field.fieldType.toLowerCase() === 'read_only' || field.fieldType.toLowerCase() === 'calculation';
            return {
              label: resolveSnapshotLabel(field, fieldValues[field.code]),
              value: getFieldDisplayValue(field),
              onAction: isReadOnlyType || isReadOnly ? undefined : handleChangeFromView,
              actionLabel: isReadOnlyType || isReadOnly ? undefined : 'Change',
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
          { label: 'Home', link: ROUTES.deliveryPlan.home },
          { label: 'Submissions', link: ROUTES.deliveryPlan.submissions.list },
          { label: formModule.submissionName || 'Submission', link: ROUTES.deliveryPlan.submissions.view(submissionId) },
        ]}
        backLink={{ href: '#', onClick: handleBack }}>
        <div ref={stepFocusRef}>
        {saveMutation.isError && Object.keys(fieldErrors).length === 0 && (
          <GovUKErrorSummary heading="There was a problem saving" errors={[{ text: saveError ?? 'Please try again or contact support if the problem persists.' }]} />
        )}

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
                        href={`#${fieldCode.toLowerCase()}`}
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
            const isReadOnlyType = field.fieldType.toLowerCase() === 'read_only' || field.fieldType.toLowerCase() === 'calculation';
            return {
              label: resolveSnapshotLabel(field, fieldValues[field.code]),
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
        </div>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.deliveryPlan.home },
        { label: 'Submissions', link: ROUTES.deliveryPlan.submissions.list },
        { label: formModule.submissionName || 'Submission', link: ROUTES.deliveryPlan.submissions.view(submissionId) },
      ]}
      currentPage={formModule.name}
      manageTitle={false}
      backLink={{ href: '#', onClick: handleBack }}>
      <div ref={stepFocusRef}>
      {saveMutation.isError && Object.keys(fieldErrors).length === 0 && (
        <GovUKErrorSummary heading="There was a problem saving" errors={[{ text: saveError ?? 'Please try again or contact support if the problem persists.' }]} />
      )}

      <span className="govuk-caption-l">
        Step {currentStep} of {totalSteps}
      </span>

      {formModule.description && <p className="govuk-body">{formModule.description}</p>}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      {currentStepErrors.length > 0 && (
        <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
          <h2 className="govuk-error-summary__title" id="error-summary-title">
            There is a problem
          </h2>
          <div className="govuk-error-summary__body">
            <ul className="govuk-list govuk-error-summary__list">
              {currentStepErrors.map(([fieldCode, message]) => {
                const field = formModule.fields.find((f) => f.code === fieldCode);
                const summaryMessage = field ? `${field.label} – ${message}` : message;
                return (
                  <li key={fieldCode}>
                    <a href={`#${fieldCode}`} onClick={(e) => focusErrorTarget(e, fieldCode)}>{summaryMessage}</a>
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

      {isReadOnly ? (
        <div className="govuk-inset-text">
          This delivery plan has been submitted, so it can no longer be changed.
        </div>
      ) : (
        <div className="govuk-button-group">
          <GovUKButton onClick={handleContinue}>Continue</GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} isLoading={saveMutation.isLoading}>
            Save as draft and exit
          </GovUKButton>
        </div>
      )}
      </div>
    </GeneralLayout>
  );
};

export default ViewModule;
