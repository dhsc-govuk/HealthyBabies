import React, { useState } from 'react';
import { Link, useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKNotificationBanner, GovUKButton, GovUKTaskList, GovUKInsetText, SummaryList } from '../../../../components/GovUKComponents';
import type { TaskListItem } from '../../../../components/GovUKComponents/GovUKTaskList';
import { SubmissionFileCard } from '../../../../components/Global/SubmissionFileCard';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import { getDeliveryPlanSubmission, submitDeliveryPlanSubmission, downloadSubmission, getSubmissionDownloadFileName, DeliveryPlanFormModuleDto } from '../queries';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

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

  const { data, isLoading } = useQuery({
    queryKey: ['delivery-plan-submission', submissionId],
    queryFn: () => getDeliveryPlanSubmission(submissionId!),
    enabled: !!submissionId,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitDeliveryPlanSubmission(submissionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['delivery-plan-submission', submissionId]);
      navigate(ROUTES.deliveryPlan.submissions.list, {
        state: { successMessage: `${submission?.name} has been submitted.` },
      });
    },
    onError: (error: Error) => {
      setSubmitError(error.message || 'Failed to submit. Please try again.');
    },
  });

  const { mutate: download, isLoading: downloading } = useMutation({
    mutationFn: () => downloadSubmission(submissionId!, 'delivery-plan'),
    onSuccess: (blob) => downloadFile(blob, getSubmissionDownloadFileName(submissionId!)),
  });

  const submission = data?.data;

  usePageTitle(submission?.name ?? 'View submission');

  const handleSubmit = () => {
    setSubmitError(null);
    submitMutation.mutate();
  };

  const isReopened = submission?.status === 'Reopened';
  const isComplete = submission?.status === 'Approved' || submission?.status === 'Complete';
  // Once submitted, the plan is locked for editing until DHSC accepts or reopens it - there is
  // nothing left to resubmit, so the button must not linger just because every section still
  // reads "Submitted" from the round that's already been sent.
  const isAwaitingReview = submission?.status === 'Submitted';

  // A section is ready to send when it is Completed, or already Submitted from a previous
  // round that DHSC has since reopened. This mirrors the server-side gate in
  // SubmissionsControllerService.SubmitSubmission, which counts both; without "submitted"
  // here a reopened plan could be edited but never resubmitted. The exception is a section
  // DHSC specifically flagged for review — that one must actually be marked Completed before
  // Resubmit is enabled, the same as a first-time submission requires of every section.
  const READY_STATUSES = ['completed', 'submitted'];
  const allModulesComplete = submission?.formModules.every((m) => {
    const isFlagged = isReopened && m.isFlaggedForReview;
    return isFlagged ? m.status.toLowerCase() === 'completed' : READY_STATUSES.includes(m.status.toLowerCase());
  });

  const taskListItems: TaskListItem[] = (submission?.formModules ?? []).map((module: DeliveryPlanFormModuleDto) => {
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
      onClick: () =>
        navigate(
          module.allowsMultipleActivities
            ? ROUTES.deliveryPlan.submissions.activities.list(submissionId, module.id)
            : ROUTES.deliveryPlan.submissions.module.view(submissionId, module.id)
        ),
    };
  });

  const breadcrumbs = [
    { label: 'Home', link: ROUTES.deliveryPlan.home },
    { label: 'Submissions', link: ROUTES.deliveryPlan.submissions.list },
    { label: user?.organisation_name ?? 'Local Authority', link: ROUTES.deliveryPlan.submissions.list },
    { label: submission?.name ?? 'Submission', link: ROUTES.deliveryPlan.submissions.view(submission?.id) },
  ];

  // Once DHSC has accepted the plan there is nothing left to do or resubmit, so the page becomes
  // a read-only record instead of the editable task list - matching the admin's own read-only
  // review of an accepted submission.
  if (submission && isComplete) {
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
                  <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.deliveryPlan.submissions.review.section(submissionId, section.formModuleId)}>
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
            <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.versions.list('delivery-plan', submission.id)}>
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

            {isAwaitingReview && (
              <GovUKNotificationBanner title="Submitted">
                <p className="govuk-body">This delivery plan has been submitted and is locked from further changes until DHSC accepts it or reopens it for correction.</p>
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
              <GovUKButton onClick={handleSubmit} disabled={!allModulesComplete || isAwaitingReview} isLoading={submitMutation.isLoading}>
                {isReopened ? 'Resubmit' : 'Submit'}
              </GovUKButton>
            </div>

            {submission.previousVersionCount > 0 && (
              <p className="govuk-body">
                <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.versions.list('delivery-plan', submission.id)}>
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
