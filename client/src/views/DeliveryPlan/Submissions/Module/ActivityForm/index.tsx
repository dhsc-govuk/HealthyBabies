import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKNotificationBanner, SummaryList, FormFieldRenderer, GovUKSummaryCard } from '../../../../../components/GovUKComponents';
import { computeFormula } from '../../../../../components/GovUKComponents/FormFieldRenderer';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { getActivityForm, getActivityTemplate, createActivity, saveActivityForm, ActivityFormDetailDto, DeliveryPlanFieldDto, DeliveryPlanFieldOptionDto } from '../../queries';
import { encodeNullableForWaf, decodeFromWaf } from '../../../../../helpers/stringUtils';
import { evaluateNumericOperators } from '../../../../../helpers/conditionalOperators';
import { ConditionalRule, FieldConfiguration } from '../../../../OrganisationAdmin/Submissions/queries';
import { getModuleContent } from '../moduleContent';

type ViewMode = 'form' | 'summary' | 'view';

const ActivityFormView = (): React.ReactElement => {
  const navigate = useNavigate();
  const routerLocation = useLocation();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, activityId } = useParams<{
    submissionId: string;
    moduleId: string;
    activityId: string;
  }>();

  const isViewOnly = routerLocation.pathname.endsWith('/view');
  const apiBase = routerLocation.pathname.startsWith('/organisation-admin') ? 'organisation-admin' : 'delivery-plan';
  const basePath = `/${apiBase}`;
  const isNew = activityId === 'new';

  const [currentStep, setCurrentStep] = useState(1);
  const [viewMode, setViewMode] = useState<ViewMode | null>(null);
  const [fieldValues, setFieldValues] = useState<Record<string, string | string[] | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.keys(fieldErrors).length > 0);

  const onDataLoaded = (data: { data: ActivityFormDetailDto }) => {
    const initialValues: Record<string, string | string[] | null> = {};
    data.data.fields.forEach((field) => {
      if (field.fieldType.toLowerCase() === 'checkbox') {
        const decoded = decodeFromWaf(field.value);
        initialValues[field.code] = decoded ? decoded.split(',') : [];
      } else {
        initialValues[field.code] = decodeFromWaf(field.value);
      }
    });
    setFieldValues(initialValues);

    if (isViewOnly || data.data.status.toLowerCase() === 'completed') {
      setViewMode('view');
    } else {
      setViewMode('form');
    }
  };

  const { data: activityData, isLoading: isLoadingForm } = useQuery({
    queryKey: ['activity-form', submissionId, moduleId, activityId],
    queryFn: () => getActivityForm(submissionId!, moduleId!, activityId!, apiBase),
    enabled: !!submissionId && !!moduleId && !!activityId && !isNew,
    onSuccess: onDataLoaded,
  });

  const { data: templateData, isLoading: isLoadingTemplate } = useQuery({
    queryKey: ['activity-template', submissionId, moduleId],
    queryFn: () => getActivityTemplate(submissionId!, moduleId!, apiBase),
    enabled: !!submissionId && !!moduleId && isNew,
    onSuccess: onDataLoaded,
  });

  const isLoading = isNew ? isLoadingTemplate : isLoadingForm;
  const activityData_ = isNew ? templateData : activityData;

  const saveMutation = useMutation({
    mutationFn: async (markComplete: boolean) => {
      // Exclude read_only fields — their values are set by the backend (e.g. auto-generated Activity ID).
      const readOnlyCodes = new Set(
        (activity?.fields ?? []).filter((f) => f.fieldType.toLowerCase() === 'read_only').map((f) => f.code)
      );
      const serializedValues: Record<string, string | null> = {};
      Object.entries(fieldValues).forEach(([key, value]) => {
        if (!readOnlyCodes.has(key)) {
          serializedValues[key] = Array.isArray(value) ? value.join(',') : value;
        }
      });
      const encodedValues = Object.fromEntries(
        Object.entries(serializedValues).map(([key, value]) => [key, encodeNullableForWaf(value)])
      );

      if (isNew) {
        const created = await createActivity(submissionId!, moduleId!, apiBase);
        const newActivityId = created.data.activityId;
        return saveActivityForm(submissionId!, moduleId!, newActivityId, { fieldValues: encodedValues, markComplete }, apiBase);
      }

      return saveActivityForm(submissionId!, moduleId!, activityId!, { fieldValues: encodedValues, markComplete }, apiBase);
    },
    onSuccess: (response, markComplete) => {
      queryClient.invalidateQueries(['activities-module', submissionId, moduleId]);
      if (!isNew) {
        queryClient.invalidateQueries(['activity-form', submissionId, moduleId, activityId]);
      }
      const savedActivity = response?.data;
      const noun = getModuleContent(savedActivity?.moduleCode ?? activityData_?.data?.moduleCode).activityNoun;
      const capitalisedNoun = noun.charAt(0).toUpperCase() + noun.slice(1);
      navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`, {
        state: markComplete
          ? {
              notification: {
                type: 'success',
                title: `${capitalisedNoun} saved`,
                message: `${decodeFromWaf(savedActivity?.activityName) ?? capitalisedNoun} has been successfully saved.`,
              },
            }
          : undefined,
      });
    },
  });

  const activity = activityData_?.data;
  const content = getModuleContent(activity?.moduleCode);
  const activityDisplayName = decodeFromWaf(activity?.activityName) || content.addButtonText;

  usePageTitle(activity ? `${activityDisplayName} – ${activity.moduleName}` : content.activityNoun);

  const sections = useMemo(() => {
    if (!activity) return [];
    return [...activity.sections].sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [activity]);

  const totalSteps = sections.length > 0 ? sections.length + 1 : 1;

  const currentSection = useMemo(() => {
    if (currentStep > sections.length) return null;
    return sections[currentStep - 1];
  }, [sections, currentStep]);

  const currentFields = useMemo(() => {
    if (!activity) return [];
    const fields = [...activity.fields].sort((a, b) => a.displayOrder - b.displayOrder);
    if (sections.length === 0) return fields;
    if (!currentSection) return [];
    return fields.filter((f) => f.stepNumber === currentSection.sectionNumber);
  }, [activity, sections, currentSection]);

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

      const valueMatches = (parentValue: string | string[] | null, targetValue: string) => {
        if (Array.isArray(parentValue)) return parentValue.includes(targetValue);
        return parentValue === targetValue;
      };
      const valueNotMatches = (parentValue: string | string[] | null, targetValue: string) => {
        if (Array.isArray(parentValue)) return !parentValue.includes(targetValue);
        return parentValue !== targetValue;
      };

      if (allOf && Array.isArray(allOf)) {
        return allOf.every((condition) => {
          const pv = fieldValues[condition.fieldKey];
          const numeric = evaluateNumericOperators(pv, condition);
          if (numeric !== null) return numeric;
          if (condition.equals !== undefined) return valueMatches(pv, condition.equals);
          if (condition.notEquals !== undefined) return valueNotMatches(pv, condition.notEquals);
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

  useEffect(() => {
    if (!activity) return;
    const calculationFields = activity.fields.filter((f) => f.fieldType.toLowerCase() === 'calculation');
    if (calculationFields.length === 0) return;

    setFieldValues((prev) => {
      const updates: Record<string, string | null> = {};
      calculationFields.forEach((field) => {
        const config = parseConfiguration(field.configuration);
        const formula = config?.formula ?? '';
        const result = computeFormula(formula, prev);
        const newVal = result !== null ? String(result) : '';
        if (prev[field.code] !== newVal) updates[field.code] = newVal;
      });
      if (Object.keys(updates).length === 0) return prev;
      return { ...prev, ...updates };
    });
  }, [activity, fieldValues]); // eslint-disable-line react-hooks/exhaustive-deps

  const validateCurrentStep = useCallback((): boolean => {
    if (!activity) return true;
    const errors: Record<string, string> = {};
    const visibleFields = currentFields.filter(isFieldVisible);
    const processedSumGroups = new Set<string>();

    visibleFields.forEach((field) => {
      const fieldTypeLower = field.fieldType.toLowerCase();
      if (fieldTypeLower === 'calculation' || fieldTypeLower === 'read_only') return;

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
        if (isNaN(numValue)) { errors[field.code] = 'Enter a valid number'; return; }
        if (numValue < 0) { errors[field.code] = 'Enter a number 0 or more'; return; }
        const config = parseConfiguration(field.configuration);
        if (config?.min !== undefined && numValue < config.min) { errors[field.code] = `Enter a number ${config.min} or more`; return; }
        if (config?.max !== undefined && numValue > config.max) { errors[field.code] = `Enter a number ${config.max} or less`; return; }
        if (config?.sumGroup && config?.maxSumField && !processedSumGroups.has(config.sumGroup)) {
          processedSumGroups.add(config.sumGroup);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(maxValue) && maxValue > 0) {
            const groupFields = visibleFields.filter((f) => parseConfiguration(f.configuration)?.sumGroup === config.sumGroup);
            const sum = groupFields.reduce((acc, f) => { const v = parseFloat((fieldValues[f.code] as string) || '0'); return acc + (isNaN(v) ? 0 : v); }, 0);
            if (sum > maxValue) {
              const groupLabel = config.groupLabel || config.sumGroup;
              groupFields.forEach((f) => { const fv = parseFloat((fieldValues[f.code] as string) || '0'); if (fv > 0) errors[f.code] = `The total for '${groupLabel}' (${sum}) cannot exceed the total (${maxValue})`; });
            }
          }
        }
      }
    });

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [activity, currentFields, fieldValues, isFieldVisible]);

  const validateAllFields = useCallback((): boolean => {
    if (!activity) return true;
    const errors: Record<string, string> = {};
    const allFields = activity.fields.sort((a, b) => a.displayOrder - b.displayOrder);
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
            const groupFields = visibleFields.filter((f) => parseConfiguration(f.configuration)?.sumGroup === config.sumGroup);
            const sum = groupFields.reduce((acc, f) => { const v = parseFloat((fieldValues[f.code] as string) || '0'); return acc + (isNaN(v) ? 0 : v); }, 0);
            if (sum > maxValue) {
              const groupLabel = config.groupLabel || config.sumGroup;
              groupFields.forEach((f) => { const fv = parseFloat((fieldValues[f.code] as string) || '0'); if (fv > 0) errors[f.code] = `The total for '${groupLabel}' (${sum}) cannot exceed the total (${maxValue})`; });
            }
          }
        } else if (config?.maxSumField && !config?.sumGroup) {
          const numValue = parseFloat(stringValue);
          const maxValue = parseFloat((fieldValues[config.maxSumField] as string) || '0');
          if (!isNaN(numValue) && !isNaN(maxValue) && maxValue > 0 && numValue > maxValue) errors[field.code] = `This value (${numValue}) cannot exceed the total (${maxValue})`;
        }
      }
    });

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }, [activity, fieldValues, isFieldVisible]);

  const handleContinue = () => {
    if (!validateCurrentStep()) { setSubmitAttempts((n) => n + 1); return; }
    if (currentStep === totalSteps - 1) {
      setViewMode('summary');
    } else if (currentStep < totalSteps) {
      setCurrentStep(currentStep + 1);
      window.scrollTo(0, 0);
    }
  };

  const handleBack = () => {
    if (isNew && currentStep === 1) {
      navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`);
      return;
    }
    if (viewMode === 'view' || viewMode === null) {
      navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`);
    } else if (viewMode === 'summary') {
      setViewMode('form');
      setCurrentStep(sections.length);
    } else if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      window.scrollTo(0, 0);
    } else {
      navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`);
    }
  };

  const handleSaveAsDraft = () => {
    if (!validateCurrentStep()) { setSubmitAttempts((n) => n + 1); return; }
    saveMutation.mutate(false);
  };

  const handleConfirmAndSave = () => {
    if (!validateAllFields()) { setSubmitAttempts((n) => n + 1); return; }
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

  const handleDeleteFromView = () => {
    navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}/delete`, {
      state: { activityName: activityDisplayName, moduleCode: activity?.moduleCode },
    });
  };

  const getFieldDisplayValue = (field: DeliveryPlanFieldDto): string => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';
    if (Array.isArray(value)) return value.map((v) => field.options.find((o: DeliveryPlanFieldOptionDto) => o.value === v)?.label ?? v).join(', ');
    const fieldTypeLower = field.fieldType.toLowerCase();
    if (fieldTypeLower === 'currency' || fieldTypeLower === 'calculation') {
      const num = parseFloat(value as string);
      return isNaN(num) ? (value as string) : `£${num.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    return field.options.find((o: DeliveryPlanFieldOptionDto) => o.value === value)?.label ?? (value as string);
  };

  const hasFieldValue = (field: DeliveryPlanFieldDto): boolean => {
    const value = fieldValues[field.code];
    if (value === null || value === undefined) return false;
    if (Array.isArray(value)) return value.length > 0;
    return value !== '';
  };

  const isSubAnswerField = (field: DeliveryPlanFieldDto): boolean => parseConditionalRules(field.conditionalRules)?.displayInline === true;

  const renderField = (field: DeliveryPlanFieldDto, isConditionalChild = false) => (
    <FormFieldRenderer
      field={field}
      value={fieldValues[field.code] ?? null}
      allFields={activity?.fields || []}
      allValues={fieldValues}
      allErrors={fieldErrors}
      showQuestionCode
      isConditionalChild={isConditionalChild}
      onChange={(code, value) => setFieldValues((prev) => ({ ...prev, [code]: value }))}
      onFileUpload={() => Promise.resolve()}
    />
  );

  const activitiesListUrl = `${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`;

  if (isLoading || viewMode === null) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading activity" />
      </GeneralLayout>
    );
  }

  if (!activity) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Activity not found</h1>
        <GovUKButton onClick={() => navigate(activitiesListUrl)}>Back to activities</GovUKButton>
      </GeneralLayout>
    );
  }

  const allFields = [...activity.fields].sort((a, b) => a.displayOrder - b.displayOrder);
  const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

  if (viewMode === 'view') {
    return (
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">Section {activity.sectionNumber} – {activity.moduleName}</span>
        <h1 className="govuk-heading-l">{activityDisplayName}</h1>
        <p className="govuk-body">This page shows the saved information for this {content.activityNoun}.</p>
        <p className="govuk-body">
          You can update this {content.activityNoun} at any time to keep your information accurate. Select 'Change' to update the information.
        </p>
        <GovUKSummaryCard
          title={activityDisplayName}
          actions={[
            { label: 'Change', onClick: handleChangeFromView, visuallyHiddenText: activityDisplayName },
            { label: 'Delete', onClick: handleDeleteFromView, visuallyHiddenText: activityDisplayName },
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
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        {Object.keys(fieldErrors).length > 0 && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There is a problem</h2>
            <div className="govuk-error-summary__body">
              <ul className="govuk-list govuk-error-summary__list">
                {Object.entries(fieldErrors).map(([fieldCode, errorMessage]) => {
                  const field = activity.fields.find((f) => f.code === fieldCode);
                  return (
                    <li key={fieldCode}>
                      <a href="#" onClick={(e) => { e.preventDefault(); handleChangeAnswer(field?.stepNumber ?? 1); }}>
                        {field ? `${field.label} – ${errorMessage}` : errorMessage}
                      </a>
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        )}
        <span className="govuk-caption-l">Section {activity.sectionNumber} – {activity.moduleName}</span>
        <h1 className="govuk-heading-l">{activityDisplayName}</h1>
        <h2 className="govuk-heading-m">Check your answers before saving</h2>
        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={fieldsWithValues.map((field) => {
            const isReadOnlyType = field.fieldType.toLowerCase() === 'read_only' || field.fieldType.toLowerCase() === 'calculation';
            return {
              label: field.label,
              value: getFieldDisplayValue(field),
              onAction: isReadOnlyType ? undefined : () => handleChangeAnswer(field.stepNumber ?? 1),
              actionLabel: isReadOnlyType ? undefined : 'Change',
              isSubAnswer: isSubAnswerField(field),
            };
          })}
        />
        <h2 className="govuk-heading-m">Now save this {content.activityNoun}</h2>
        <p className="govuk-body">By saving, you confirm that the details you are providing are correct to the best of your knowledge.</p>
        <GovUKButton onClick={handleConfirmAndSave} isLoading={saveMutation.isLoading}>Confirm and save {content.activityNoun}</GovUKButton>
        {saveMutation.isError && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There was a problem saving</h2>
            <div className="govuk-error-summary__body"><p>Please try again or contact support if the problem persists.</p></div>
          </div>
        )}
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">Section {activity.sectionNumber} – {activity.moduleName} (step {currentStep} of {totalSteps})</span>
      <h1 className="govuk-heading-l">{activityDisplayName}</h1>

      {currentStep === 1 && <p className="govuk-body">{content.formIntroText}</p>}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      {Object.keys(fieldErrors).length > 0 && (
        <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert" aria-labelledby="error-summary-title" tabIndex={-1}>
          <h2 className="govuk-error-summary__title" id="error-summary-title">There is a problem</h2>
          <div className="govuk-error-summary__body">
            <ul className="govuk-list govuk-error-summary__list">
              {Object.entries(fieldErrors).map(([fieldCode, message]) => {
                const field = activity.fields.find((f) => f.code === fieldCode);
                return <li key={fieldCode}><a href={`#${fieldCode}`}>{field ? `${field.label} – ${message}` : message}</a></li>;
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
              const parentField = activity.fields.find((f) => f.code === rules.showWhen?.fieldKey);
              if (parentField?.fieldType.toLowerCase() === 'radio' && parentField?.stepNumber === field.stepNumber) return null;
            }

            const isConditionalChild = !!rules?.displayInline && !!rules?.showWhen;
            const fieldConfig = parseConfiguration(field.configuration);
            const fieldVal = parseFloat((fieldValues[field.code] as string) || '0');
            const showOverspendWarning = field.fieldType.toLowerCase() === 'calculation' && fieldConfig?.warnIfNegative === true && !isNaN(fieldVal) && fieldVal < 0;

            return (
              <React.Fragment key={field.id}>
                <div style={{ marginLeft: isConditionalChild ? '33px' : 0, paddingLeft: isConditionalChild ? '15px' : 0, borderLeft: isConditionalChild ? '4px solid #b1b4b6' : 'none' }}>
                  {renderField(field, isConditionalChild)}
                </div>
                {showOverspendWarning && (
                  <GovUKNotificationBanner type="important" title="Warning">
                    <p className="govuk-body">
                      <strong>{field.label}</strong> is showing a negative value (£{Math.abs(fieldVal).toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} overspend). Please review your funding figures.
                    </p>
                  </GovUKNotificationBanner>
                )}
              </React.Fragment>
            );
          })}
        </div>
      ) : (
        <div className="govuk-inset-text"><p>No form fields have been configured for this section yet.</p></div>
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
          <div className="govuk-error-summary__body"><p>Please try again or contact support if the problem persists.</p></div>
        </div>
      )}
    </GeneralLayout>
  );
};

export default ActivityFormView;
