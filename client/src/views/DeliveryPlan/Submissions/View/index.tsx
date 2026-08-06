import React, { useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKNotificationBanner, GovUKButton, GovUKTaskList } from '../../../../components/GovUKComponents';
import type { TaskListItem } from '../../../../components/GovUKComponents/GovUKTaskList';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import { getDeliveryPlanSubmission, submitDeliveryPlanSubmission, DeliveryPlanFormModuleDto } from '../queries';
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
    queryKey: ['delivery-plan-submission', submissionId],
    queryFn: () => getDeliveryPlanSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitDeliveryPlanSubmission(submissionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['delivery-plan-submission', submissionId]);
      navigate(`/delivery-plan/submissions`, {
        state: { successMessage: `${submission?.name} has been submitted.` },
      });
    },
    onError: (error: Error) => {
      setSubmitError(error.message || 'Failed to submit. Please try again.');
    },
  });

  const submission = data?.data;

  usePageTitle(submission?.name ?? 'View submission');

  const handleSubmit = () => {
    setSubmitError(null);
    submitMutation.mutate();
  };

  const allModulesComplete = submission?.formModules.every((m) => m.status.toLowerCase() === 'completed');

  const taskListItems: TaskListItem[] = (submission?.formModules ?? []).map((module: DeliveryPlanFormModuleDto) => {
    const colour = getSubmissionStatusTagColour(module.status);
    return {
      id: module.id,
      title: `Section ${module.sectionNumber}: ${module.name}`,
      hint: undefined,
      status: {
        text: module.status,
        tag: colour !== 'white' ? { text: module.status, colour } : undefined,
      },
      onClick: () =>
        navigate(
          module.allowsMultipleActivities
            ? `/delivery-plan/submissions/${submissionId}/modules/${module.id}/activities`
            : `/delivery-plan/submissions/${submissionId}/modules/${module.id}`
        ),
    };
  });

  return (
    <LoadingSpinner loading={isLoading} label="Loading submission">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/delivery-plan/home' },
          { label: 'Submissions', link: '/delivery-plan/submissions' },
          { label: submission?.name ?? 'Submission', link: `/delivery-plan/submissions/${submission?.id}` },
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
                  <a href="mailto:support@example.gov.uk" className="govuk-link">
                    support@example.gov.uk
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
