import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKFieldset } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import usePageTitle from '../../../../hooks/usePageTitle';
import { getSection, getSubmission, saveSection, SectionFieldDto } from '../queries';

const SectionForm = (): React.ReactElement => {
  usePageTitle('Complete section');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, sectionId } = useParams<{
    submissionId: string;
    moduleId: string;
    sectionId: string;
  }>();

  const [fieldValues, setFieldValues] = useState<Record<string, string | null>>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);

  const { data: submissionData } = useQuery({
    queryKey: ['organisation-admin-submission', submissionId],
    queryFn: () => getSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const { data: sectionData, isLoading } = useQuery({
    queryKey: ['organisation-admin-section', submissionId, sectionId],
    queryFn: () => getSection(submissionId!, sectionId!),
    enabled: !!submissionId && !!sectionId,
    onSuccess: (data) => {
      trackStarted();
      const initialValues: Record<string, string | null> = {};
      data.data.fields.forEach((field) => {
        initialValues[field.code] = field.value;
      });
      setFieldValues(initialValues);
    },
  });

  const saveMutation = useMutation({
    mutationFn: (markComplete: boolean) => saveSection(submissionId!, sectionId!, { fieldValues, markComplete }),
    onSuccess: (_data, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      queryClient.invalidateQueries(['organisation-admin-section', submissionId, sectionId]);
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}`);
    },
    onError: () => {
      setSubmitAttempts((n) => n + 1);
    },
  });

  useErrorSummaryFocus(submitAttempts, saveMutation.isError);

  const { trackStarted, trackSubmitted, trackDraftSaved } = useFormTelemetry(
    'section_form',
    submissionData?.data?.formModules.find((m) => m.id === moduleId)?.name
  );

  const submission = submissionData?.data;
  const formModule = submission?.formModules.find((m) => m.id === moduleId);
  const section = sectionData?.data;

  const handleFieldChange = (code: string, value: string | null) => {
    setFieldValues((prev) => ({ ...prev, [code]: value }));
  };

  const handleSaveAndContinue = () => {
    saveMutation.mutate(true);
  };

  const handleSaveAsDraft = () => {
    saveMutation.mutate(false);
  };

  const renderField = (field: SectionFieldDto) => {
    const value = fieldValues[field.code] ?? '';

    switch (field.fieldType.toLowerCase()) {
      case 'text':
        return (
          <GovUKFieldset.Input
            id={field.code}
            name={field.code}
            label={field.label}
            hint={field.helpText ?? undefined}
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
      case 'textarea':
        return (
          <GovUKFieldset.Textarea
            id={field.code}
            name={field.code}
            label={field.label}
            hint={field.helpText ?? undefined}
            value={value}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
      case 'radio':
        return (
          <GovUKFieldset.RadioGroup
            name={field.code}
            legend={field.label}
            hint={field.helpText ?? undefined}
            options={field.options.map((opt) => ({
              value: opt.value,
              text: opt.label,
            }))}
            value={value}
            onChange={(val: string) => handleFieldChange(field.code, val)}
          />
        );
      case 'select':
        return (
          <GovUKFieldset.Select
            id={field.code}
            name={field.code}
            label={field.label}
            hint={field.helpText ?? undefined}
            options={field.options.map((opt) => ({
              value: opt.value,
              label: opt.label,
            }))}
            value={value}
            onChange={(e: React.ChangeEvent<HTMLSelectElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
      default:
        return (
          <GovUKFieldset.Input
            id={field.code}
            name={field.code}
            label={field.label}
            hint={field.helpText ?? undefined}
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
    }
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading section" />
      </GeneralLayout>
    );
  }

  if (!section) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Section not found</h1>
        <p className="govuk-body">The section you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}`)}>Back to module</GovUKButton>
      </GeneralLayout>
    );
  }

  const sortedFields = [...section.fields].sort((a, b) => a.displayOrder - b.displayOrder);

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
        {
          label: submission?.name ?? 'Submission',
          link: `/organisation-admin/submissions/${submissionId}`,
        },
        {
          label: formModule?.name ?? 'Module',
          link: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}`,
        },
        {
          label: `Section ${section.number}`,
          link: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}/sections/${sectionId}`,
        },
      ]}
      currentPage=""
      backLink={{ href: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}` }}>
      <h1 className="govuk-heading-l">
        Section {section.number}: {section.title}
      </h1>

      {section.description && <p className="govuk-body">{section.description}</p>}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      {sortedFields.length > 0 ? (
        <div>
          {sortedFields.map((field) => (
            <div key={field.id}>{renderField(field)}</div>
          ))}
        </div>
      ) : (
        <div className="govuk-inset-text">
          <p>No form fields have been configured for this section yet. Please contact an administrator to set up the form.</p>
        </div>
      )}

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleSaveAndContinue} isLoading={saveMutation.isLoading}>
          Save and continue
        </GovUKButton>
        <GovUKButton onClick={handleSaveAsDraft} disabled={saveMutation.isLoading} className="govuk-button--secondary">
          Save as draft
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

export default SectionForm;
