import React, { useState } from 'react';
import axios from 'axios';
import { Link, useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKNotificationBanner, GovUKButton, GovUKTaskList, GovUKInsetText, SummaryList } from '../../../../components/GovUKComponents';
import type { TaskListItem } from '../../../../components/GovUKComponents/GovUKTaskList';
import { SubmissionFileCard } from '../../../../components/Global/SubmissionFileCard';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import { getDaysRemainingMessage } from '../../../../helpers/datetime';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { getSubmission, submitSubmission, reopenSubmission, downloadSubmission, getSubmissionDownloadFileName, SubmissionFormModuleDto } from '../queries';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

const UNCONFIRMED_SUBMISSION_MESSAGE =
  "We couldn't confirm your submission went through. Please wait a moment, refresh this page, and check whether it shows as submitted before trying again.";

const isUnconfirmedSubmissionError = (error: unknown): boolean => axios.isAxiosError(error) && (!error.response || [502, 503, 504].includes(error.response.status));

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const formatDateTime = (dateString: string | null): string => {
  if (!dateString) return '';
  const date = new Date(dateString);
  const day = date.getDate();
  const month = date.toLocaleDateString('en-GB', { month: 'long' });
  const year = date.getFullYear();
  const hours = date.getHours().toString().padStart(2, '0');
  const minutes = date.getMinutes().toString().padStart(2, '0');
  return `${day} ${month} ${year}, ${hours}:${minutes}`;
};

const downloadFile = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

interface LocationState {
  successMessage?: string;
  successTitle?: string;
}

const ViewSubmission = (): React.ReactElement => {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { user } = useAuthProvider();
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
      navigate(ROUTES.organisationAdmin.submissions.submitted(submissionId), {
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

  const { mutate: download, isLoading: downloading } = useMutation({
    mutationFn: () => downloadSubmission(submissionId!),
    onSuccess: (blob) => downloadFile(blob, getSubmissionDownloadFileName(submissionId!)),
  });

  const submission = data?.data;

  usePageTitle(submission?.name ?? 'View submission');

  const handleFormModuleClick = (moduleId: string, moduleCode: string, allowsMultipleActivities: boolean) => {
    if (allowsMultipleActivities) {
      navigate(ROUTES.organisationAdmin.submissions.activities.list(submissionId, moduleId));
    } else if (moduleCode === 'service-users') {
      navigate(ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId));
    } else if (moduleCode === 'wider-service-users') {
      navigate(ROUTES.organisationAdmin.submissions.widerServices.list(submissionId, moduleId));
    } else if (moduleCode === 'outcome-scores') {
      navigate(ROUTES.organisationAdmin.submissions.outcomeScores.list(submissionId, moduleId));
    } else {
      navigate(ROUTES.organisationAdmin.submissions.module.view(submissionId, moduleId));
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

  const isWindowClosed = submission?.isWindowClosed ?? false;
  // Self-service "Reopen for correction" is the management information quarterly flow only - a
  // Delivery Plan can only be reopened by DHSC (DeliveryPlanApprovalController), never by the
  // local authority itself.
  const canReopen = submission?.status === 'Submitted' && !isWindowClosed && submission?.type !== 'delivery-plan';
  const isReopened = submission?.status === 'Reopened';
  const isComplete = submission?.status === 'Approved' || submission?.status === 'Complete';
  const isAwaitingReview = submission?.status === 'Submitted';

  // A section is ready to send when it is Completed, or already Submitted from a previous
  // round that DHSC has since reopened — mirrors the equivalent gate on the delivery-plan-lead
  // view (DeliveryPlan/Submissions/View), since a reopened plan's sections stay "submitted".
  // The exception is a section DHSC specifically flagged for review — that one must actually be
  // marked Completed before Resubmit is enabled, the same as a first-time submission requires.
  const READY_STATUSES = ['completed', 'submitted'];
  const allModulesComplete = submission?.formModules.every((m) => {
    const isFlagged = isReopened && m.isFlaggedForReview;
    return isFlagged ? m.status.toLowerCase() === 'completed' : READY_STATUSES.includes(m.status.toLowerCase());
  });

  const taskListItems: TaskListItem[] = (submission?.formModules ?? []).map((module: SubmissionFormModuleDto) => {
    const isFlagged = isReopened && module.isFlaggedForReview && module.status.toLowerCase() !== 'completed';
    const colour = isFlagged ? 'light-blue' : getSubmissionStatusTagColour(module.status);
    const statusText = isFlagged ? 'Review required' : module.status;
    return {
      id: module.id,
      title: `Section ${module.sectionNumber}: ${module.name}`,
      hint: undefined,
      status: {
        text: statusText,
        tag: colour !== 'white' ? { text: statusText, colour } : undefined,
      },
      onClick: () => handleFormModuleClick(module.id, module.code, module.allowsMultipleActivities),
    };
  });

  const breadcrumbs = [
    { label: 'Home', link: ROUTES.organisationAdmin.home },
    { label: 'Submissions', link: ROUTES.organisationAdmin.submissions.list },
    { label: user?.organisation_name ?? 'Local Authority', link: ROUTES.organisationAdmin.submissions.list },
    { label: submission?.name ?? 'Submission', link: ROUTES.organisationAdmin.submissions.view(submission?.id) },
  ];

  // Once DHSC has accepted the plan there is nothing left to do or resubmit, so the page becomes
  // a read-only record instead of the editable task list. Management information quarters keep
  // their existing "Complete" task-list view - only a Delivery Plan gets this summary.
  if (submission && isComplete && submission.type === 'delivery-plan') {
    const detailsItems = [
      { label: 'Status', value: 'Accepted' },
      ...(submission.submittedBy
        ? [
            {
              label: 'Submitted by',
              value: (
                <span>
                  {submission.submittedBy}
                  {submission.submittedByEmail && (
                    <>
                      {'     '}
                      <a href={`mailto:${submission.submittedByEmail}`} className="govuk-link">
                        {submission.submittedByEmail}
                      </a>
                    </>
                  )}
                </span>
              ),
            },
          ]
        : []),
      ...(submission.submittedAt ? [{ label: 'Submitted on', value: formatDateTime(submission.submittedAt) }] : []),
      ...(submission.reviewedBy ? [{ label: 'Accepted by', value: submission.reviewedBy }] : []),
      ...(submission.reviewedAt ? [{ label: 'Accepted on', value: formatDateTime(submission.reviewedAt) }] : []),
      ...(submission.reviewNotes ? [{ label: 'Comment', value: submission.reviewNotes }] : []),
    ];

    return (
      <GeneralLayout breadcrumbs={breadcrumbs} currentPage="">
        <h1 className="govuk-heading-l">{submission.name}</h1>
        <p className="govuk-body-l">View your submission details, including its status. Download the submitted data for further analysis.</p>

        <h2 className="govuk-heading-m">Details</h2>
        <SummaryList items={detailsItems} />

        {submission.sections.length > 0 && (
          <>
            <h2 className="govuk-heading-m govuk-!-margin-top-6">Sections</h2>
            <ul className="govuk-list">
              {submission.sections.map((section) => (
                <li key={section.formModuleId}>
                  <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.organisationAdmin.submissions.review.section(submissionId, section.formModuleId)}>
                    Section {section.sectionNumber}: {section.name}
                  </Link>
                </li>
              ))}
            </ul>
          </>
        )}

        <h2 className="govuk-heading-m govuk-!-margin-top-6">Files</h2>
        <SubmissionFileCard
          name={`${user?.organisation_name ?? 'Local Authority'} Delivery Plan`}
          subtitle={submission.submittedBy && submission.submittedAt ? `Submitted by ${submission.submittedBy} on ${formatDateTime(submission.submittedAt)}` : undefined}
          downloads={[{ label: downloading ? 'Downloading...' : 'Download in ZIP', onClick: () => download(), disabled: downloading }]}
        />

        {submission.previousVersionCount > 0 && (
          <p className="govuk-body govuk-!-margin-top-4">
            <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.versions.list('organisation-admin', submission.id)}>
              View previous submissions
            </Link>
          </p>
        )}
      </GeneralLayout>
    );
  }

  return (
    <LoadingSpinner loading={isLoading} label="Loading submission">
      <GeneralLayout breadcrumbs={breadcrumbs} currentPage="">
        {submission && (
          <>
            <h1 className="govuk-heading-l">{submission.name}</h1>
            <p className="govuk-body">
              {isReopened ? 'Resubmit' : 'Submit'} by {formatDate(submission.endDate)}
            </p>

            {locationState?.successMessage && (
              <GovUKNotificationBanner type="success" title={locationState.successTitle || 'Success'}>
                <p className="govuk-body">{locationState.successMessage}</p>
              </GovUKNotificationBanner>
            )}

            {isReopened && (
              <GovUKNotificationBanner type="important" title="Important">
                <h3 className="govuk-notification-banner__heading">This data collection has been reopened</h3>
                <p className="govuk-body">
                  Your submission requires review. Contact{' '}
                  <a href="mailto:healthybabies.dataanddigital@dhsc.gov.uk" className="govuk-link">
                    healthybabies.dataanddigital@dhsc.gov.uk
                  </a>{' '}
                  if you think there&apos;s a problem.
                </p>
              </GovUKNotificationBanner>
            )}

            {isReopened && submission.reviewNotes && (
              <GovUKInsetText>
                <h3 className="govuk-heading-s">Reason for reopening</h3>
                <p className="govuk-body">{submission.reviewNotes}</p>
              </GovUKInsetText>
            )}

            {submission.status === 'Submitted' && (
              <GovUKNotificationBanner title="Submitted">
                <p className="govuk-body">This submission has been submitted and is locked from further changes.</p>

                {isWindowClosed && <p className="govuk-body">The submission window has closed, so it can no longer be reopened.</p>}

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
              <GovUKButton onClick={handleSubmit} disabled={!allModulesComplete || isAwaitingReview || isComplete} isLoading={submitMutation.isLoading}>
                {isReopened ? 'Resubmit' : 'Submit'}
              </GovUKButton>
            </div>

            {submission.previousVersionCount > 0 && (
              <p className="govuk-body">
                <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.versions.list('organisation-admin', submission.id)}>
                  View previous submissions
                </Link>
              </p>
            )}
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewSubmission;
