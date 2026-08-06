import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKNotificationBanner, GovUKButton, GovUKTaskList } from '../../../../components/GovUKComponents';
import type { TaskListItem } from '../../../../components/GovUKComponents/GovUKTaskList';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import { getDaysRemainingMessage } from '../../../../helpers/datetime';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { getSubmission, submitSubmission, reopenSubmission, SubmissionFormModuleDto } from '../queries';
import usePageTitle from '../../../../hooks/usePageTitle';

const UNCONFIRMED_SUBMISSION_MESSAGE =
  "We couldn't confirm your submission went through. Please wait a moment, refresh this page, and check whether it shows as submitted before trying again.";

const isUnconfirmedSubmissionError = (error: unknown): boolean =>
  axios.isAxiosError(error) && (!error.response || [502, 503, 504].includes(error.response.status));

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
  const [reopenError, setReopenError] = useState<string | null>(null);
  const [showReopenConfirm, setShowReopenConfirm] = useState(false);

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
      if (isUnconfirmedSubmissionError(error)) {
        setSubmitError(UNCONFIRMED_SUBMISSION_MESSAGE);
        return;
      }

      if (axios.isAxiosError(error)) {
        processError(error, (message) => setSubmitError(message || 'Failed to submit. Please try again.'));
        return;
      }

      setSubmitError(error.message || 'Failed to submit. Please try again.');
    },
  });

  const reopenMutation = useMutation({
    mutationFn: () => reopenSubmission(submissionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      setShowReopenConfirm(false);
    },
    onError: (error: Error) => {
      setReopenError(error.message || 'Failed to reopen. Please try again.');
    },
  });

  const submission = data?.data;

  usePageTitle(submission?.name ?? 'View submission');

  const handleFormModuleClick = (moduleId: string, moduleCode: string, allowsMultipleActivities: boolean) => {
    if (allowsMultipleActivities) {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/activities`);
    } else if (moduleCode === 'service-users') {
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

  const handleReopen = () => {
    setReopenError(null);
    reopenMutation.mutate();
  };

  const allModulesComplete = submission?.formModules.every((m) => m.status.toLowerCase() === 'completed');
  const isWindowClosed = submission?.isWindowClosed ?? false;
  const canReopen = submission?.status === 'Submitted' && !isWindowClosed;

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
      onClick: () => handleFormModuleClick(module.id, module.code, module.allowsMultipleActivities),
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

            {submission.status === 'Submitted' && (
              <GovUKNotificationBanner title="Submitted">
                <p className="govuk-body">This quarter has been submitted and is locked from further changes.</p>

                {isWindowClosed && <p className="govuk-body">The submission window for this quarter has closed, so it can no longer be reopened.</p>}

                {canReopen && !showReopenConfirm && (
                  <GovUKButton onClick={() => setShowReopenConfirm(true)} className="govuk-button govuk-button--secondary">
                    Reopen for correction
                  </GovUKButton>
                )}

                {canReopen && showReopenConfirm && (
                  <>
                    <p className="govuk-body">Reopening will let you make changes again. You'll need to submit this quarter again before the deadline.</p>
                    {reopenError && <p className="govuk-error-message">{reopenError}</p>}
                    <div className="govuk-button-group">
                      <GovUKButton onClick={handleReopen} isLoading={reopenMutation.isLoading}>
                        Yes, reopen this quarter
                      </GovUKButton>
                      <GovUKButton onClick={() => setShowReopenConfirm(false)} className="govuk-button govuk-button--secondary" disabled={reopenMutation.isLoading}>
                        Cancel
                      </GovUKButton>
                    </div>
                  </>
                )}
              </GovUKNotificationBanner>
            )}

            {(submission.isWindowClosed || submission.daysRemaining <= 7) && submission.status !== 'Submitted' && (
              <GovUKNotificationBanner type="important" title="Important">
                <p className="govuk-body">
                  <strong>{getDaysRemainingMessage(submission.daysRemaining, 'your submission', submission.isWindowClosed)}</strong>
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
