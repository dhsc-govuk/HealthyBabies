import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, SummaryList, FormFieldRenderer, GovUKErrorSummary } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import usePageTitle from '../../../../hooks/usePageTitle';
import useStepFocus from '../../../../hooks/useStepFocus';
import { getWiderServiceCategoryForm, saveWiderServiceCategoryForm, FormModuleFieldDto, ConditionalRule, FieldConfiguration } from '../queries';
import { resolveSnapshotLabel } from '../../../../helpers/answerSnapshot';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { sortFieldsForDisplay } from '../../../../helpers/formFieldOrdering';
import { processError } from '../../../../helpers/axiosErrorFallback';
import './styles.css';
import focusErrorTarget from '../../../../helpers/focusErrorTarget';
import { ROUTES } from '../../../../constants/routes';

type ViewMode = 'form' | 'summary';

const WiderServiceForm = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('wider_service_submission');
  const { submissionId, moduleId, categoryId } = useParams<{
    submissionId: string;
    moduleId: string;
    categoryId: string;
  }>();

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode>('form');
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);

  const { data, isLoading } = useQuery({
    queryKey: ['wider-service-category-form', submissionId, moduleId, categoryId],
    queryFn: () => getWiderServiceCategoryForm(submissionId!, moduleId!, categoryId!),
    enabled: !!submissionId && !!moduleId && !!categoryId,
    onSuccess: (response) => {
      trackStarted();
      const initialValues: Record<string, string | string[] | null> = {};
      response.data.fields.forEach((field) => {
        if (field.fieldType.toLowerCase() === 'checkbox') {
          initialValues[field.code] = field.value ? field.value.split(',') : [];
        } else {
          initialValues[field.code] = field.value;
        }
      });
      setFieldValues(initialValues);
    },
  });

  const formData = data?.data;

  usePageTitle('Provide wider service data', Object.keys(fieldErrors).length > 0);
  const stepFocusRef = useStepFocus<HTMLDivElement>(`${viewMode}-${currentStep}`);
  useErrorSummaryFocus(submitAttempts, Object.keys(fieldErrors).length > 0);

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
      return saveWiderServiceCategoryForm(submissionId!, moduleId!, categoryId!, {
        fieldValues: encodedValues,
        markComplete,
      });
    },
    // BLUM-7520: no longer bumps submitAttempts — GovUKErrorSummary focuses itself on mount, and
    // useErrorSummaryFocus here is keyed on the field errors, not this one.
    onError: (error) => {
      processError(error, setSaveError);
    },
    onSuccess: (_data, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      queryClient.invalidateQueries(['wider-service-users-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['wider-service-category-form', submissionId, moduleId, categoryId]);
      navigate(ROUTES.organisationAdmin.submissions.widerServices.list(submissionId, moduleId), {
        state: {
          notification: {
            type: 'success',
            title: 'Wider services user data saved',
            message: `Wider services user data for '${formData?.categoryName}' has been successfully saved.`,
          },
        },
      });
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

  const sections = useMemo(() => {
    if (!formData) return [];
    return [...formData.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [formData]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 2;

  const currentSection = useMemo(() => {
    if (currentStep > sections.length) return null;
    return sections[currentStep - 1];
  }, [sections, currentStep]);

  const currentFields = useMemo(() => {
    if (!formData) return [];
    const fields = sortFieldsForDisplay(formData.fields);
    if (sections.length === 0) return fields;
    if (!currentSection) return [];
    return fields.filter((f) => f.stepNumber === currentSection.sectionNumber);
  }, [formData, sections, currentSection]);

  const parseConditionalRules = (rulesJson: string | null): ConditionalRule | null => {
    if (!rulesJson) return null;
    try {
      return JSON.parse(rulesJson) as ConditionalRule;
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

  const isFieldVisible = useCallback(
    (field: FormModuleFieldDto): boolean => {
      const rules = parseConditionalRules(field.conditionalRules);
      if (!rules?.showWhen) return true;

      const { showWhen } = rules;

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
        return showWhen.allOf.every((condition) => {
          const parentValue = fieldValues[condition.fieldKey!];
          if (condition.equals !== undefined) return valueMatches(parentValue, condition.equals);
          if (condition.notEquals !== undefined) return valueNotMatches(parentValue, condition.notEquals);
          return true;
        });
      }

      if (showWhen.fieldKey) {
        const parentValue = fieldValues[showWhen.fieldKey];
        if (showWhen.equals !== undefined) return valueMatches(parentValue, showWhen.equals);
        if (showWhen.notEquals !== undefined) return valueNotMatches(parentValue, showWhen.notEquals);
        if (showWhen.in !== undefined && Array.isArray(showWhen.in)) {
          if (Array.isArray(parentValue)) {
            return showWhen.in.some((v: string) => parentValue.includes(v));
          }
          return showWhen.in.includes(parentValue as string);
        }
      }

      return true;
    },
    [fieldValues]
  );

  const validateCurrentStep = useCallback((): boolean => {
    if (!formData) return true;
    const errors: Record<string, string> = {};
    const visibleFields = currentFields.filter(isFieldVisible);

    visibleFields.forEach((field) => {
      const value = fieldValues[field.code];
      const stringValue = typeof value === 'string' ? value : '';
      const fieldTypeLower = field.fieldType.toLowerCase();
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
        }
      }
    });

    Object.keys(errors).forEach((field) => trackValidationFailed(field, 'invalid'));
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [formData, currentFields, fieldValues, isFieldVisible, trackValidationFailed]);

  const handleFieldChange = (code: string, value: string | string[] | null) => {
    setFieldValues((prev) => ({ ...prev, [code]: value }));
  };

  const handleContinue = () => {
    // Clear any previous save failure first — the summary now sits above the field-error summary,
    // so leaving a stale one there would steal the focus that belongs to the validation errors
    // raised just below (focusErrorSummary takes the first match).
    clearSaveError();
    if (!validateCurrentStep()) {
      setSubmitAttempts((n) => n + 1);
      return;
    }

    const lastFormStep = totalSteps - 1;
    if (currentStep === lastFormStep) {
      trackReviewReached();
      setViewMode('summary');
      return;
    }

    trackSectionCompleted(String(currentStep));
    setCurrentStep(currentStep + 1);
    window.scrollTo(0, 0);
  };

  const handleBack = () => {
    // Leaving the step that failed — drop the summary, otherwise it renders at the top of the
    // next step claiming a save failed there.
    clearSaveError();
    if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length > 0 ? sections.length : 1);
      window.scrollTo(0, 0);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(ROUTES.organisationAdmin.submissions.widerServices.list(submissionId, moduleId));
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

  const handleConfirmAndSave = () => {
    clearSaveError();
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  const handleChangeAnswer = (stepNumber: number) => {
    clearSaveError();
    setCurrentStep(stepNumber);
    setViewMode('form');
    window.scrollTo(0, 0);
  };

  const getFieldDisplayValue = (field: FormModuleFieldDto): string | React.ReactNode => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';

    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o) => o.value === v)?.label ?? v).join(', ');
    }

    const option = field.options.find((o) => o.value === value);
    return option?.label ?? value;
  };

  const renderField = (field: FormModuleFieldDto, showQuestionCode = true, isConditionalChild = false) => (
    <FormFieldRenderer
      field={field}
      value={fieldValues[field.code] ?? null}
      allFields={formData?.fields || []}
      allValues={fieldValues}
      allErrors={fieldErrors}
      showQuestionCode={showQuestionCode}
      isConditionalChild={isConditionalChild}
      onChange={handleFieldChange}
    />
  );

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading form" />
      </GeneralLayout>
    );
  }

  if (!formData) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Category not found</h1>
        <p className="govuk-body">The category you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(ROUTES.organisationAdmin.submissions.widerServices.list(submissionId, moduleId))}>Back to wider services</GovUKButton>
      </GeneralLayout>
    );
  }

  if (viewMode === 'summary') {
    const allFields = sortFieldsForDisplay(formData.fields);

    const hasFieldValue = (field: FormModuleFieldDto): boolean => {
      const value = fieldValues[field.code];
      if (value === null || value === undefined) return false;
      if (Array.isArray(value)) return value.length > 0;
      return value !== '';
    };

    const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

    return (
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        <div ref={stepFocusRef}>
        {saveMutation.isError && (
          <GovUKErrorSummary heading="There was a problem saving" errors={[{ text: saveError ?? 'Please try again or contact support if the problem persists.' }]} />
        )}

        <span className="govuk-caption-l">
          Step {totalSteps} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Provide data for {formData.categoryName}</h1>

        <h2 className="govuk-heading-m">Check your answers before saving your data</h2>
        <p className="govuk-body">
          Check the information you've entered before saving the wider services user data. Make sure the details are accurate and complete. You can go back to make changes if
          anything needs updating.
        </p>

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => ({
            label: resolveSnapshotLabel(field, fieldValues[field.code]),
            value: getFieldDisplayValue(field),
            onAction: () => handleChangeAnswer(field.stepNumber ?? 1),
            actionLabel: 'Change',
          }))}
        />

        <h2 className="govuk-heading-m">Now save your wider service users data</h2>
        <p className="govuk-body">
          By saving the service user data for wider services in the '{formData.categoryName}' category, you are confirming that, to the best of your knowledge, the details you are
          providing are correct. You may return and update these details before the quarterly Management Information submission.
        </p>

        <GovUKButton onClick={handleConfirmAndSave} isLoading={saveMutation.isLoading}>
          Confirm and save
        </GovUKButton>
        </div>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
      <div ref={stepFocusRef}>
      {saveMutation.isError && Object.keys(fieldErrors).length === 0 && (
        <GovUKErrorSummary heading="There was a problem saving" errors={[{ text: saveError ?? 'Please try again or contact support if the problem persists.' }]} />
      )}

      <span className="govuk-caption-l">
        Step {currentStep} of {totalSteps}
      </span>
      <h1 className="govuk-heading-l">Provide data for {formData.categoryName}</h1>

      <p className="govuk-body">
        Tell us about the users who accessed the wider services in this category in the relevant quarter. This information will be included in your quarterly Management
        Information data collection.
      </p>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      {Object.keys(fieldErrors).length > 0 && (
        <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title">
          <h2 className="govuk-error-summary__title" id="error-summary-title">
            There is a problem
          </h2>
          <div className="govuk-error-summary__body">
            <ul className="govuk-list govuk-error-summary__list">
              {Object.entries(fieldErrors).map(([fieldCode, message]) => {
                const field = formData.fields.find((f) => f.code === fieldCode);
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
            const isConditionalChild = !!rules?.displayInline && !!rules?.showWhen;

            return <div key={field.id}>{renderField(field, true, isConditionalChild)}</div>;
          })}
        </div>
      ) : (
        <div className="govuk-inset-text">
          <p>No form fields have been configured for this category yet.</p>
        </div>
      )}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleContinue}>Continue</GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} isLoading={saveMutation.isLoading}>
          Save as draft and exit
        </GovUKButton>
      </div>
      </div>
    </GeneralLayout>
  );
};

export default WiderServiceForm;
