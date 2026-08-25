import React from 'react';
import { useMutation, useQuery } from 'react-query';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';

import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, GovUKInsetText, GovUKNotificationBanner, LoadingSpinner, SummaryList, useGovUKNotification } from '../../../../components/GovUKComponents';
import { SubmissionFileCard } from '../../../../components/Global/SubmissionFileCard';
import { getLocalAuthoritySubmission, getStatusLabel, formatDateTime, formatDateOnly, downloadSubmission, getDownloadFileName, isAwaitingReview } from './queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { ROUTES } from '../../../../constants/routes';
import './styles.css';

function LocalAuthorityView() {
  const { submissionId, localAuthorityId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
  }>();
  const { setNotification } = useGovUKNotification();
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as { successTitle?: string; successMessage?: string } | null;

  const { data, isLoading, error } = useQuery({
    queryKey: ['local-authority-submission', submissionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(submissionId!, localAuthorityId!),
    enabled: !!submissionId && !!localAuthorityId,
  });

  const { mutate: handleDownload, isLoading: downloading } = useMutation({
    mutationKey: ['download-submission', submissionId, localAuthorityId],
    mutationFn: () => downloadSubmission(submissionId!, localAuthorityId!),
    onSuccess: (response) => {
      const blob = new Blob([response.data], { type: 'application/zip' });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getDownloadFileName(submissionId!, localAuthorityId!);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner />
      </GeneralLayout>
    );
  }

  if (error || !data?.data) {
    return (
      <GeneralLayout>
        <p className="govuk-body">Failed to load local authority submission details.</p>
      </GeneralLayout>
    );
  }

  const submission = data.data;

  const breadcrumbItems = [
    { label: 'Home', link: ROUTES.admin.home },
    { label: 'Submissions', link: ROUTES.admin.submissions.list },
    { label: submission.localAuthorityName, link: ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId) },
    { label: submission.dataCollectionName, link: ROUTES.admin.submissions.view(submissionId) },
  ];

  // Section links are a Delivery Plan review feature. Management information submissions keep the
  // existing details-and-downloads page.
  const isDeliveryPlan = submission.type === 'delivery-plan';

  const isReopened = submission.status === 'Rejected';
  const isComplete = submission.status === 'Approved';
  // Accept/Reopen are the Delivery Plan approval flow only - a management information quarter uses
  // its own self-service reopen, which doesn't allow reopening an Approved submission, so approving
  // an MI quarter here would lock it permanently with no recovery path.
  const canReview = isDeliveryPlan && isAwaitingReview(submission.status);

  const detailsItems = [
    {
      label: 'Status',
      value: getStatusLabel(submission.status),
    },
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
    ...(submission.submittedAt
      ? [
          {
            label: 'Submitted on',
            value: formatDateTime(submission.submittedAt),
          },
        ]
      : []),
    ...(submission.contact
      ? [
          {
            label: 'Contact',
            value: (
              <>
                {submission.contact.organisationName}
                {submission.contact.addressLine1 && (
                  <>
                    <br />
                    {submission.contact.addressLine1}
                  </>
                )}
                {submission.contact.addressLine2 && (
                  <>
                    <br />
                    {submission.contact.addressLine2}
                  </>
                )}
                {submission.contact.city && (
                  <>
                    <br />
                    {submission.contact.city}
                  </>
                )}
                {submission.contact.postcode && (
                  <>
                    <br />
                    {submission.contact.postcode}
                  </>
                )}
                {submission.contact.country && (
                  <>
                    <br />
                    {submission.contact.country}
                  </>
                )}
                {submission.contact.phone && (
                  <>
                    <br />
                    <br />
                    {submission.contact.phone}
                  </>
                )}
                {submission.contact.email && (
                  <>
                    <br />
                    <a href={`mailto:${submission.contact.email}`} className="govuk-link">
                      {submission.contact.email}
                    </a>
                  </>
                )}
              </>
            ),
          },
        ]
      : []),
    // After reopening, the new deadline replaces the submission details as the thing that matters.
    ...(isReopened && submission.dueDate
      ? [
          {
            label: 'Due date',
            value: formatDateOnly(submission.dueDate),
          },
        ]
      : []),
    ...(isComplete && submission.reviewedBy
      ? [
          {
            label: 'Accepted by',
            value: submission.reviewedBy,
          },
        ]
      : []),
    ...(isComplete && submission.reviewedAt
      ? [
          {
            label: 'Accepted on',
            value: formatDateTime(submission.reviewedAt),
          },
        ]
      : []),
    ...(isComplete && submission.reviewNotes
      ? [
          {
            label: 'Comment',
            value: submission.reviewNotes,
          },
        ]
      : []),
    ...(isReopened && submission.reviewNotes
      ? [
          {
            label: 'Reason for reopening',
            value: submission.reviewNotes,
          },
        ]
      : []),
    ...(isReopened && submission.sectionsToReview.length > 0
      ? [
          {
            label: 'Sections to review',
            value: submission.sectionsToReview.map((section) => section.name).join(', '),
          },
        ]
      : []),
  ];

  return (
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage={submission.localAuthorityName}>
      {locationState?.successMessage && (
        <GovUKNotificationBanner type="success" title="Success">
          <h3 className="govuk-notification-banner__heading">{locationState.successTitle ?? 'Success'}</h3>
          <p className="govuk-body">{locationState.successMessage}</p>
        </GovUKNotificationBanner>
      )}

      <span className="govuk-caption-l">{submission.dataCollectionName}</span>

      <p className="govuk-body-l">View the local authority's submission details, including its status and contact information. Download the submitted data for further analysis.</p>

      <h2 className="govuk-heading-m">Details</h2>
      <SummaryList items={detailsItems} />

      {isDeliveryPlan && submission.sections.length > 0 && (
        <>
          <h2 className="govuk-heading-m govuk-!-margin-top-6">Sections</h2>
          {isReopened ? (
            <GovUKInsetText>All sections will be available for review here once the local authority has completed their data submission.</GovUKInsetText>
          ) : (
            <ul className="govuk-list">
              {submission.sections.map((section) => (
                <li key={section.formModuleId}>
                  <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.admin.submissions.reviewSection(submissionId, localAuthorityId, section.formModuleId)}>
                    Section {section.sectionNumber}: {section.name}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {submission.previousVersionCount > 0 && (
        <p className="govuk-body govuk-!-margin-top-6">
          <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.admin.submissions.versions.list(submissionId, localAuthorityId)}>
            View previous submissions
          </Link>
        </p>
      )}

      {submission.files && submission.files.length > 0 && (
        <>
          <h2 className="govuk-heading-m govuk-!-margin-top-6">Files</h2>
          {submission.files.map((file: (typeof submission.files)[number], index: number) => (
            <SubmissionFileCard
              key={index}
              name={file.name}
              subtitle={
                file.status ??
                (submission.submittedBy && submission.submittedAt
                  ? `Submitted by ${submission.submittedBy} on ${formatDateTime(submission.submittedAt)}`
                  : file.description)
              }
              downloads={
                file.downloadUrl
                  ? [
                      {
                        label: downloading ? 'Downloading...' : `Download in ZIP ${file.downloadSize ? `(${file.downloadSize})` : ''}`,
                        onClick: () => handleDownload(),
                        disabled: downloading,
                      },
                    ]
                  : []
              }
            />
          ))}
        </>
      )}
      {canReview && (
        <div className="govuk-button-group govuk-!-margin-top-6">
          <GovUKButton onClick={() => navigate(ROUTES.admin.submissions.acceptLocalAuthority(submissionId, localAuthorityId))}>Accept submission</GovUKButton>
          <button
            type="button"
            className="govuk-button govuk-button--secondary"
            data-module="govuk-button"
            onClick={() => navigate(ROUTES.admin.submissions.reopenLocalAuthority(submissionId, localAuthorityId))}>
            Reopen submission
          </button>
        </div>
      )}
    </GeneralLayout>
  );
}

export default LocalAuthorityView;
