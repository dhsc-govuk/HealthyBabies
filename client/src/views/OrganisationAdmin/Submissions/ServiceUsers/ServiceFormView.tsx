import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { Button } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKLinkButton, SummaryList, GovUKErrorSummary } from '../../../../components/GovUKComponents';
import { getServiceForm, getSubmission, deleteServiceForm, FormModuleFieldDto } from '../queries';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import { isStoredAnswer, resolveSnapshotLabel } from '../../../../helpers/answerSnapshot';
import { evaluateNumericOperators } from '../../../../helpers/conditionalOperators';
import { groupFieldsForSummary, isInlineSubAnswer } from '../../../../helpers/formFieldOrdering';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { ROUTES } from '../../../../constants/routes';
import './styles.css';

interface ConditionalRule {
  displayInline?: boolean;
  showWhen?: {
    fieldKey?: string;
    serviceFieldKey?: string;
    equals?: string;
    notEquals?: string;
    in?: string[];
    gt?: string;
    gte?: string;
    lt?: string;
    lte?: string;
    eq?: string;
    neq?: string;
    allOf?: Array<{
      fieldKey?: string;
      serviceFieldKey?: string;
      equals?: string;
      in?: string[];
      gt?: string;
      gte?: string;
      lt?: string;
      lte?: string;
      eq?: string;
      neq?: string;
    }>;
  };
}

const ServiceFormView = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, serviceId } = useParams<{
    submissionId: string;
    moduleId: string;
    serviceId: string;
  }>();

  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);

  const { data: formData, isLoading: isLoadingForm } = useQuery({
    queryKey: ['service-form', submissionId, moduleId, serviceId],
    queryFn: () => getServiceForm(submissionId!, moduleId!, serviceId!),
    enabled: !!submissionId && !!moduleId && !!serviceId,
  });

  const { data: submissionData, isLoading: isLoadingSubmission } = useQuery({
    queryKey: ['organisation-admin-submission', submissionId],
    queryFn: () => getSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteServiceForm(submissionId!, moduleId!, serviceId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['service-users-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      navigate(ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId), {
        state: { deletedServiceName: serviceForm?.serviceName },
      });
    },
    // Without this a refused delete - most often because the quarter has been submitted and is
    // locked - looked like the button had simply done nothing.
    onError: (error) => {
      processError(error, setDeleteError);
    },
  });

  const isLoading = isLoadingForm || isLoadingSubmission;
  const serviceForm = formData?.data;

  const dataCollectionName = submissionData?.data?.name || '';
  const dataCollectionStartDate = submissionData?.data?.startDate
    ? new Date(submissionData.data.startDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
    : '';
  const dataCollectionEndDate = submissionData?.data?.endDate
    ? new Date(submissionData.data.endDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
    : '';
  const dateRange = dataCollectionStartDate && dataCollectionEndDate ? `${dataCollectionStartDate}–${dataCollectionEndDate}` : '';

  const fieldValues = useMemo(() => {
    if (!serviceForm) return {};
    const values: Record<string, string | string[] | null> = {};
    serviceForm.fields.forEach((field: FormModuleFieldDto) => {
      if (field.fieldType.toLowerCase() === 'checkbox') {
        values[field.code] = field.value ? field.value.split(',') : [];
      } else {
        values[field.code] = field.value;
      }
    });
    return values;
  }, [serviceForm]);

  const parseConditionalRules = useCallback((rulesJson: string | null): ConditionalRule | null => {
    if (!rulesJson) return null;
    try {
      return JSON.parse(rulesJson) as ConditionalRule;
    } catch {
      return null;
    }
  }, []);

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
        return showWhen.allOf.every((condition) => {
          if (condition.serviceFieldKey) {
            const serviceValue = serviceForm?.serviceAnswers?.[condition.serviceFieldKey] ?? null;
            const numeric = evaluateNumericOperators(serviceValue, condition);
            if (numeric !== null) return numeric;
            if (condition.equals !== undefined) return valueMatches(serviceValue, condition.equals);
            if (condition.in !== undefined && Array.isArray(condition.in)) return condition.in.includes(serviceValue ?? '');
            return true;
          }
          const parentValue = fieldValues[condition.fieldKey!];
          const numeric = evaluateNumericOperators(parentValue, condition);
          if (numeric !== null) return numeric;
          if (condition.equals !== undefined) {
            return valueMatches(parentValue, condition.equals);
          }
          return true;
        });
      }

      if (showWhen.serviceFieldKey) {
        const serviceValue = serviceForm?.serviceAnswers?.[showWhen.serviceFieldKey] ?? null;
        const numeric = evaluateNumericOperators(serviceValue, showWhen);
        if (numeric !== null) return numeric;
        if (showWhen.equals !== undefined) return valueMatches(serviceValue, showWhen.equals);
        if (showWhen.in !== undefined && Array.isArray(showWhen.in)) return showWhen.in.includes(serviceValue ?? '');
        return true;
      }

      if (showWhen.fieldKey) {
        const parentValue = fieldValues[showWhen.fieldKey];
        const numeric = evaluateNumericOperators(parentValue, showWhen);
        if (numeric !== null) return numeric;
        if (showWhen.equals !== undefined) {
          return valueMatches(parentValue, showWhen.equals);
        }
        if (showWhen.notEquals !== undefined) {
          return valueNotMatches(parentValue, showWhen.notEquals);
        }
        if (showWhen.in !== undefined && Array.isArray(showWhen.in)) {
          if (Array.isArray(parentValue)) {
            return showWhen.in.some((v) => parentValue.includes(v));
          }
          return showWhen.in.includes(parentValue as string);
        }
      }

      return true;
    },
    [fieldValues, parseConditionalRules, serviceForm]
  );

  const hasFieldValue = useCallback((field: FormModuleFieldDto): boolean => {
    const value = fieldValues[field.code];
    if (value === null || value === undefined) return false;
    if (Array.isArray(value)) return value.length > 0;
    return value !== '';
  }, [fieldValues]);

  const getFieldDisplayValue = useCallback((field: FormModuleFieldDto): string => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';

    if (value === 'n/a') return 'N/A';

    // Prefer the server's snapshot-resolved display, but only when rendering the stored answer
    // (not an in-progress edit), so historic answers survive later form/option changes.
    if (field.displayValue && isStoredAnswer(field, value)) {
      return field.displayValue;
    }

    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o) => o.value === v)?.label ?? v).join(', ');
    }

    const option = field.options.find((o) => o.value === value);
    return option?.label ?? (value as string);
  }, [fieldValues]);

  const processFieldLabel = useCallback((label: string) => label.replace(/\{serviceName\}/g, serviceForm?.serviceName || ''), [serviceForm?.serviceName]);

  const summaryItems = useMemo(() => {
    if (!serviceForm) return [];

    return groupFieldsForSummary(serviceForm.fields, (field) => hasFieldValue(field) && isFieldVisible(field)).flatMap((group) =>
      group.groupKey
        ? [
            { label: processFieldLabel(group.label), value: '', isGroupHeader: true },
            ...group.fields.map((field) => ({
              label: processFieldLabel(resolveSnapshotLabel(field, fieldValues[field.code])),
              value: getFieldDisplayValue(field),
              isSubAnswer: true,
            })),
          ]
        : [
            {
              label: processFieldLabel(resolveSnapshotLabel(group.fields[0], fieldValues[group.fields[0].code])),
              value: getFieldDisplayValue(group.fields[0]),
              isSubAnswer: isInlineSubAnswer(group.fields[0]),
            },
          ]
    );
  }, [serviceForm, fieldValues, getFieldDisplayValue, hasFieldValue, isFieldVisible, processFieldLabel]);

  const handleBack = () => {
    navigate(ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId));
  };

  const handleChange = () => {
    navigate(ROUTES.organisationAdmin.submissions.serviceUsers.form(submissionId, moduleId, serviceId));
  };

  const handleDelete = () => {
    setShowDeleteConfirm(true);
  };

  const handleConfirmDelete = () => {
    setDeleteError(undefined);
    deleteMutation.reset();
    deleteMutation.mutate();
  };

  const handleCancelDelete = () => {
    setShowDeleteConfirm(false);
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading service data" />
      </GeneralLayout>
    );
  }

  if (!serviceForm) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Service not found</h1>
        <p className="govuk-body">The service you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId))}>Back to services</GovUKButton>
      </GeneralLayout>
    );
  }

  if (showDeleteConfirm) {
    return (
      <GeneralLayout currentPage="" backLink={{ href: '#', onClick: handleCancelDelete }}>
        {deleteMutation.isError && (
          <GovUKErrorSummary heading="There was a problem deleting" errors={[{ text: deleteError ?? 'Please try again or contact support if the problem persists.' }]} />
        )}
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete service user data?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting service user data for &apos;{serviceForm.serviceName}&apos; will remove all saved information about users who accessed this service in the relevant quarter.
          </WarningPanelBody>
          <WarningPanelBody>
            This will not remove any saved service user data from previous Management Information data collections, or any saved information about the service itself.
          </WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>If you want to keep this service&apos;s user data for the relevant quarter, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleConfirmDelete} disabled={deleteMutation.isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelAnchor
              href="#"
              onClick={(e) => {
                e.preventDefault();
                handleCancelDelete();
              }}>
              Go back
            </WarningPanelAnchor>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.organisationAdmin.home },
        { label: 'Submissions', link: ROUTES.organisationAdmin.submissions.list },
        { label: dataCollectionName || 'Submission', link: ROUTES.organisationAdmin.submissions.view(submissionId) },
        { label: 'Section 2: Service users', link: ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId) },
      ]}
      currentPage=""
      backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">Section 2: Service users</span>
      <h1 className="govuk-heading-l">{serviceForm.serviceName}</h1>

      <p className="govuk-body">This page shows saved information you've provided about users who accessed the service '{serviceForm.serviceName}' in the relevant quarter.</p>
      <p className="govuk-body">
        You can update service user data at any time before submitting the quarterly Management Information data collection. To update the information, select 'Change'. To delete
        service users data, select 'Delete'.
      </p>

      <div
        style={{
          border: '1px solid #b1b4b6',
          marginTop: '30px',
          marginBottom: '20px',
        }}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            padding: '15px 20px',
            borderBottom: '1px solid #b1b4b6',
            backgroundColor: '#f3f2f1',
          }}>
          <span className="govuk-body govuk-!-margin-bottom-0">
            <strong>{dataCollectionName}</strong>. {dateRange}
          </span>
          <div style={{ display: 'flex', gap: '15px' }}>
            <GovUKLinkButton
              className="govuk-!-font-weight-bold"
              onClick={handleChange}>
              Change<span className="govuk-visually-hidden"> {serviceForm.serviceName} service user data</span>
            </GovUKLinkButton>
            <span style={{ color: '#b1b4b6' }}>|</span>
            <GovUKLinkButton
              className="govuk-!-font-weight-bold"
              onClick={handleDelete}>
              Delete<span className="govuk-visually-hidden"> {serviceForm.serviceName} service user data</span>
            </GovUKLinkButton>
          </div>
        </div>
        <div style={{ padding: '0 20px' }}>
          <SummaryList items={summaryItems} noOuterBorder halfWidthColumns />
        </div>
      </div>
    </GeneralLayout>
  );
};

export default ServiceFormView;
