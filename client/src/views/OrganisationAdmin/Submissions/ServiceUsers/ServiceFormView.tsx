import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { Button } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, SummaryList } from '../../../../components/GovUKComponents';
import { getServiceForm, getSubmission, deleteServiceForm, FormModuleFieldDto } from '../queries';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import './styles.css';

interface ConditionalRule {
  displayInline?: boolean;
  showWhen?: {
    fieldKey?: string;
    equals?: string;
    notEquals?: string;
    in?: string[];
    allOf?: Array<{
      fieldKey: string;
      equals?: string;
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
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`, {
        state: { deletedServiceName: serviceForm?.serviceName },
      });
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
          const parentValue = fieldValues[condition.fieldKey];
          if (condition.equals !== undefined) {
            return valueMatches(parentValue, condition.equals);
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
            return showWhen.in.some((v) => parentValue.includes(v));
          }
          return showWhen.in.includes(parentValue as string);
        }
      }

      return true;
    },
    [fieldValues]
  );

  const hasFieldValue = (field: FormModuleFieldDto): boolean => {
    const value = fieldValues[field.code];
    if (value === null || value === undefined) return false;
    if (Array.isArray(value)) return value.length > 0;
    return value !== '';
  };

  const isSubAnswerField = (field: FormModuleFieldDto): boolean => {
    const rules = parseConditionalRules(field.conditionalRules);
    const config = field.configuration ? JSON.parse(field.configuration) : null;
    return !!config?.group || !!rules?.displayInline;
  };

  const getFieldDisplayValue = (field: FormModuleFieldDto): string => {
    const value = fieldValues[field.code];
    if (!value || (Array.isArray(value) && value.length === 0)) return '';

    if (value === 'n/a') return 'N/A';

    if (Array.isArray(value)) {
      return value.map((v) => field.options.find((o) => o.value === v)?.label ?? v).join(', ');
    }

    const option = field.options.find((o) => o.value === value);
    return option?.label ?? (value as string);
  };

  const processFieldLabel = (label: string) => label.replace(/\{serviceName\}/g, serviceForm?.serviceName || '');

  const summaryItems = useMemo(() => {
    if (!serviceForm) return [];

    const allFields = [...serviceForm.fields].sort((a, b) => a.displayOrder - b.displayOrder);
    const fieldsWithValues = allFields.filter((field) => hasFieldValue(field) && isFieldVisible(field));

    return fieldsWithValues.map((field) => ({
      label: processFieldLabel(field.label),
      value: getFieldDisplayValue(field),
      isSubAnswer: isSubAnswerField(field),
    }));
  }, [serviceForm, fieldValues, isFieldVisible]);

  const handleBack = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`);
  };

  const handleChange = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}`);
  };

  const handleDelete = () => {
    setShowDeleteConfirm(true);
  };

  const handleConfirmDelete = () => {
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
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`)}>Back to services</GovUKButton>
      </GeneralLayout>
    );
  }

  if (showDeleteConfirm) {
    return (
      <GeneralLayout currentPage="" backLink={{ href: '#', onClick: handleCancelDelete }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete service user data?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting service user data for &apos;{serviceForm.serviceName}&apos; will remove all saved information about users who accessed this service in the past 3 months, from{' '}
            {dataCollectionStartDate} to {dataCollectionEndDate}.
          </WarningPanelBody>
          <WarningPanelBody>
            This will not remove any saved service user data from previous Management Information data collections, or any saved information about the service itself.
          </WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>If you want to keep this service&apos;s user data for the past quarter, you can go back.</WarningPanelBody>
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
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
        { label: dataCollectionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
        { label: 'Section 2: Service users', link: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services` },
      ]}
      currentPage=""
      backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">Section 2: Service users</span>
      <h1 className="govuk-heading-l">{serviceForm.serviceName}</h1>

      <p className="govuk-body">
        This page shows saved information you've provided about users who accessed the service '{serviceForm.serviceName}' from {dataCollectionStartDate} to {dataCollectionEndDate}
        .
      </p>
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
            <a
              href="#"
              className="govuk-link govuk-!-font-weight-bold"
              onClick={(e) => {
                e.preventDefault();
                handleChange();
              }}>
              Change<span className="govuk-visually-hidden"> {serviceForm.serviceName} service user data</span>
            </a>
            <span style={{ color: '#b1b4b6' }}>|</span>
            <a
              href="#"
              className="govuk-link govuk-!-font-weight-bold"
              onClick={(e) => {
                e.preventDefault();
                handleDelete();
              }}>
              Delete<span className="govuk-visually-hidden"> {serviceForm.serviceName} service user data</span>
            </a>
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
