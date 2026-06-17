import React, { useMemo, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, SummaryList, GovUKSummaryCard } from '../../../../components/GovUKComponents';
import { getOutcomeScoreRecord, getOutcomeScoresModule, getSubmission, FormModuleFieldDto } from '../queries';

const OutcomeScoreView = (): React.ReactElement => {
  const navigate = useNavigate();
  const { submissionId, moduleId, recordId } = useParams<{
    submissionId: string;
    moduleId: string;
    recordId: string;
  }>();

  const { data: formData, isLoading: isLoadingRecord } = useQuery({
    queryKey: ['outcome-score-record', submissionId, moduleId, recordId],
    queryFn: () => getOutcomeScoreRecord(submissionId!, moduleId!, recordId!),
    enabled: !!submissionId && !!moduleId && !!recordId,
  });

  const { data: moduleData, isLoading: isLoadingModule } = useQuery({
    queryKey: ['outcome-scores-module', submissionId, moduleId],
    queryFn: () => getOutcomeScoresModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const { data: submissionData, isLoading: isLoadingSubmission } = useQuery({
    queryKey: ['organisation-admin-submission', submissionId],
    queryFn: () => getSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const isLoading = isLoadingRecord || isLoadingModule || isLoadingSubmission;
  const outcomeScoreForm = formData?.data;

  // Get anonymisedId from module data
  const anonymisedId = useMemo(() => {
    if (!moduleData?.data?.records || !recordId) return '';
    const record = moduleData.data.records.find((r) => r.recordId === recordId);
    return record?.anonymisedId || '';
  }, [moduleData, recordId]);

  // Get data collection info from submission data
  const dataCollectionName = submissionData?.data?.name || '';
  const dataCollectionStartDate = submissionData?.data?.startDate
    ? new Date(submissionData.data.startDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
    : '';
  const dataCollectionEndDate = submissionData?.data?.endDate
    ? new Date(submissionData.data.endDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
    : '';

  const fieldValues = useMemo(() => {
    if (!outcomeScoreForm) return {};
    const values: Record<string, string | null> = {};
    outcomeScoreForm.fields.forEach((field: FormModuleFieldDto) => {
      values[field.code] = field.value;
    });
    if (outcomeScoreForm.serviceId) {
      values['PPS01'] = outcomeScoreForm.serviceId;
    }
    return values;
  }, [outcomeScoreForm]);

  const isFieldVisible = useCallback(
    (field: FormModuleFieldDto): boolean => {
      if (!field.conditionalRules) return true;
      try {
        const rules = JSON.parse(field.conditionalRules);
        if (!rules?.showWhen) return true;
        const { showWhen } = rules;
        if (showWhen.fieldKey) {
          const parentValue = fieldValues[showWhen.fieldKey];
          if (showWhen.equals !== undefined) {
            return parentValue === showWhen.equals;
          }
          if (showWhen.notEquals !== undefined) {
            return parentValue !== showWhen.notEquals;
          }
        }
        return true;
      } catch {
        return true;
      }
    },
    [fieldValues]
  );

  const handleBack = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`);
  };

  const handleChange = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}/delete`);
  };

  // Build summary items for SummaryList component
  const summaryItems = useMemo(() => {
    if (!outcomeScoreForm) return [];

    const items: Array<{ label: string; value: string; isSubAnswer?: boolean; isGroupHeader?: boolean }> = [];
    const groups: Record<string, { label: string; fields: Array<{ field: FormModuleFieldDto; displayValue: string }> }> = {};
    const fieldOrder: string[] = [];

    outcomeScoreForm.fields
      .filter(isFieldVisible)
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .forEach((field) => {
        const value = fieldValues[field.code];

        // Skip fields with no value selected
        if (value === null || value === undefined || value === '') {
          return;
        }

        let displayValue: string;
        if (field.code === 'PPS01' && outcomeScoreForm.availableServices) {
          const service = outcomeScoreForm.availableServices.find((s) => s.serviceId === value);
          displayValue = service?.serviceName || value;
        } else if (field.fieldType.toLowerCase() === 'checkbox' && field.options?.length) {
          // Handle checkbox fields - value is comma-separated
          const selectedValues = value.split(',').map((v) => v.trim());
          const labels = selectedValues.map((v) => {
            const option = field.options?.find((o) => o.value === v);
            return option?.label || v;
          });
          displayValue = labels.join(', ');
        } else {
          const option = field.options?.find((o) => o.value === value);
          displayValue = option?.label || value;
        }

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

        if (!groups[groupKey]) {
          groups[groupKey] = { label: groupLabel, fields: [] };
          fieldOrder.push(groupKey);
        }
        groups[groupKey].fields.push({ field, displayValue });
      });

    // Convert groups to summary items
    fieldOrder.forEach((groupKey) => {
      const group = groups[groupKey];
      if (group.fields.length > 1 && group.fields[0].field.configuration) {
        try {
          const config = JSON.parse(group.fields[0].field.configuration);
          if (config.group) {
            // Add parent label as a header row
            items.push({ label: group.label, value: '', isGroupHeader: true });
            // Add child fields as sub-answers
            group.fields.forEach(({ field, displayValue }) => {
              items.push({ label: field.label, value: displayValue, isSubAnswer: true });
            });
            return;
          }
        } catch {
          // Fall through
        }
      }
      // Single field
      const { field, displayValue } = group.fields[0];
      items.push({ label: field.label, value: displayValue });
    });

    return items;
  }, [outcomeScoreForm, fieldValues, isFieldVisible]);

  // Format date range for display
  const dateRange = dataCollectionStartDate && dataCollectionEndDate ? `${dataCollectionStartDate} to ${dataCollectionEndDate}` : '';

  return (
    <LoadingSpinner loading={isLoading} label="Loading outcome score record">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: dataCollectionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
          { label: 'Outcome scores', link: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores` },
        ]}
        currentPage=""
        backLink={{ href: '#', onClick: handleBack }}>
        {outcomeScoreForm && (
          <>
            <span className="govuk-caption-l">Section 4: Outcome scores</span>
            <h1 className="govuk-heading-l">{anonymisedId || 'Outcome Score Record'}</h1>

            <p className="govuk-body">
              This page shows saved information you've provided about outcome scores for a service user who used one of your services from {dataCollectionStartDate} to{' '}
              {dataCollectionEndDate}.
            </p>
            <p className="govuk-body">
              You can update this outcome score record at any time before submitting the quarterly Management Information data collection. To update the information, select
              'Change'. To delete the outcome scores record, select 'Delete'.
            </p>

            <GovUKSummaryCard
              title={
                <>
                  <strong>{dataCollectionName}</strong>. {dateRange}
                </>
              }
              actions={[
                { label: 'Change', onClick: handleChange },
                { label: 'Delete', onClick: handleDelete },
              ]}>
              <SummaryList items={summaryItems} noOuterBorder halfWidthColumns />
            </GovUKSummaryCard>
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default OutcomeScoreView;
