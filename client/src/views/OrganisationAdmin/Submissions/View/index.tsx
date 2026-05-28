import React, { useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKNotificationBanner, GovUKButton, GovUKTaskList } from '../../../../components/GovUKComponents';
import type { TaskListItem } from '../../../../components/GovUKComponents/GovUKTaskList';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import { getSubmission, submitSubmission, SubmissionFormModuleDto } from '../queries';
import usePageTitle from '../../../../hooks/usePageTitle';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

interface LocationState {
  successMessage?: string;
  successTitle?: string;
}

const ViewSubmission = (): React.ReactElement => {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { submissionId } = useParams<{ submissionId: string }>();
  const locationState = location.state as LocationState | null;
  const [submitError, setSubmitError] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['organisation-admin-submission', submissionId],
    queryFn: () => getSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitSubmission(submissionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      navigate(`/organisation-admin/submissions/${submissionId}/submitted`, {
        state: { submissionName: submission?.name },
      });
    },
    onError: (error: Error) => {
      setSubmitError(error.message || 'Failed to submit. Please try again.');
    },
  });

  const submission = data?.data;

  usePageTitle(submission?.name ?? 'View submission');

  const handleFormModuleClick = (moduleId: string, moduleCode: string) => {
    if (moduleCode === 'service-users') {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`);
    } else if (moduleCode === 'wider-service-users') {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services`);
    } else if (moduleCode === 'outcome-scores') {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`);
    } else {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}`);
    }
  };

  const handleSubmit = () => {
    setSubmitError(null);
    submitMutation.mutate();
  };

  const allModulesComplete = submission?.formModules.every((m) => m.status.toLowerCase() === 'completed');

  const taskListItems: TaskListItem[] = (submission?.formModules ?? []).map((module: SubmissionFormModuleDto) => {
    const colour = getSubmissionStatusTagColour(module.status);
    return {
      id: module.id,
      title: `Section ${module.sectionNumber}: ${module.name}`,
      hint: undefined,
      status: {
        text: module.status,
        tag: colour !== 'white' ? { text: module.status, colour } : undefined,
      },
      onClick: () => handleFormModuleClick(module.id, module.code),
    };
  });

  return (
    <LoadingSpinner loading={isLoading} label="Loading submission">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: submission?.name ?? 'Submission', link: `/organisation-admin/submissions/${submission?.id}` },
        ]}
        currentPage="">
        {submission && (
          <>
            <h1 className="govuk-heading-l">{submission.name}</h1>
            <p className="govuk-body">Submit by {formatDate(submission.endDate)}</p>

            {locationState?.successMessage && (
              <GovUKNotificationBanner type="success" title={locationState.successTitle || 'Success'}>
                <p className="govuk-body">{locationState.successMessage}</p>
              </GovUKNotificationBanner>
            )}

            {submission.daysRemaining <= 7 && submission.status !== 'Submitted' && (
              <GovUKNotificationBanner type="important" title="Important">
                <p className="govuk-body">
                  <strong>You have {submission.daysRemaining} days left to send your submission.</strong>
                </p>
                <p className="govuk-body">
                  Contact{' '}
                  <a href="mailto:healthybabies.dataanddigital@dhsc.gov.uk" className="govuk-link">
                    healthybabies.dataanddigital@dhsc.gov.uk
                  </a>{' '}
                  if you think there's a problem.
                </p>
              </GovUKNotificationBanner>
            )}

            <GovUKTaskList items={taskListItems} idPrefix="submission-modules" />

            {submitError && (
              <GovUKNotificationBanner type="important" title="Error">
                <p className="govuk-body">{submitError}</p>
              </GovUKNotificationBanner>
            )}

            <div className="govuk-button-group" style={{ marginTop: '30px' }}>
              <GovUKButton onClick={handleSubmit} disabled={!allModulesComplete} isLoading={submitMutation.isLoading}>
                Submit
              </GovUKButton>
            </div>
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewSubmission;
