import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKNotificationBanner, LoadingSpinner, SummaryList } from '../../../../../components/GovUKComponents';
import { SubmissionFileCard } from '../../../../../components/Global/SubmissionFileCard';
import { getSubmissionVersionDetail, downloadSubmissionVersion, getSubmissionVersionDownloadFileName } from '../../reviewQueries';
import { getLocalAuthoritySubmission } from '../../LocalAuthorityView/queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

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

/** Admin-only header for one previous round of a local authority's Delivery Plan submission. */
function VersionDetail(): React.ReactElement {
  const { submissionId: dataCollectionId, localAuthorityId, versionNumber } = useParams<{
    submissionId: string;
    localAuthorityId: string;
    versionNumber: string;
  }>();
  const versionNum = Number(versionNumber);

  const { data: submissionData } = useQuery({
    queryKey: ['local-authority-submission', dataCollectionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(dataCollectionId!, localAuthorityId!),
    enabled: !!dataCollectionId && !!localAuthorityId,
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-detail', dataCollectionId, localAuthorityId, versionNum],
    queryFn: () => getSubmissionVersionDetail(dataCollectionId!, localAuthorityId!, versionNum),
    enabled: !!dataCollectionId && !!localAuthorityId && !!versionNumber,
  });

  const { mutate: download, isLoading: downloading } = useMutation({
    mutationFn: () => downloadSubmissionVersion(dataCollectionId!, localAuthorityId!, versionNum),
    onSuccess: (blob) => downloadFile(blob, getSubmissionVersionDownloadFileName(versionNum)),
  });

  const submission = submissionData?.data;
  const localAuthorityHref = ROUTES.admin.submissions.localAuthority(dataCollectionId, localAuthorityId);
  const versionsListHref = ROUTES.admin.submissions.versions.list(dataCollectionId, localAuthorityId);

  usePageTitle(submission ? submission.localAuthorityName : 'Previous submission');

  if (isLoading) {
    return (
      <GeneralLayout backLink={{ href: localAuthorityHref }}>
        <LoadingSpinner />
      </GeneralLayout>
    );
  }

  if (error || !data?.data) {
    return (
      <GeneralLayout backLink={{ href: localAuthorityHref }}>
        <p className="govuk-body">Failed to load this previous submission.</p>
      </GeneralLayout>
    );
  }

  const version = data.data;

  const detailsItems = [
    { label: 'Status', value: `Previous submission (Version ${version.versionNumber})` },
    ...(version.submittedBy
      ? [
          {
            label: 'Submitted by',
            value: (
              <span>
                {version.submittedBy}
                {version.submittedByEmail && (
                  <>
                    {'     '}
                    <a href={`mailto:${version.submittedByEmail}`} className="govuk-link">
                      {version.submittedByEmail}
                    </a>
                  </>
                )}
              </span>
            ),
          },
        ]
      : []),
    ...(version.submittedAt ? [{ label: 'Submitted on', value: formatDateTime(version.submittedAt) }] : []),
    ...(version.reviewNotes ? [{ label: 'Reason for reopening', value: version.reviewNotes }] : []),
    ...(version.sectionsToReview.length > 0
      ? [{ label: 'Sections to review', value: version.sectionsToReview.map((section) => section.name).join(', ') }]
      : []),
  ];

  return (
    <GeneralLayout backLink={{ href: localAuthorityHref }} currentPage="">
      <GovUKNotificationBanner type="important" title="Important">
        <h3 className="govuk-notification-banner__heading">This is a previous submission</h3>
        <p className="govuk-body">
          To view the latest information,{' '}
          <Link className="govuk-link" to={localAuthorityHref}>
            return to the most recent submission
          </Link>
          .
        </p>
      </GovUKNotificationBanner>

      {submission && <span className="govuk-caption-l">{submission.dataCollectionName}</span>}
      <h1 className="govuk-heading-l">{submission?.localAuthorityName ?? `Version ${version.versionNumber}`}</h1>

      <p className="govuk-body-l">View the local authority's submission details, including its status and contact information. Download the submitted data for further analysis.</p>

      <h2 className="govuk-heading-m">Details</h2>
      <SummaryList items={detailsItems} />

      {version.sections.length > 0 && (
        <>
          <h2 className="govuk-heading-m govuk-!-margin-top-6">Sections</h2>
          <ul className="govuk-list">
            {version.sections.map((section) => (
              <li key={section.formModuleId}>
                <Link
                  className="govuk-link govuk-link--no-visited-state"
                  to={ROUTES.admin.submissions.versions.section(dataCollectionId, localAuthorityId, versionNum, section.formModuleId)}>
                  Section {section.sectionNumber}: {section.name}
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}

      <h2 className="govuk-heading-m govuk-!-margin-top-6">Files</h2>
      <SubmissionFileCard
        name={`Local Authority Delivery Plan (${version.versionNumber})`}
        subtitle={version.submittedBy && version.submittedAt ? `Submitted by ${version.submittedBy} on ${formatDateTime(version.submittedAt)}` : undefined}
        downloads={[{ label: downloading ? 'Downloading...' : 'Download in ZIP', onClick: () => download(), disabled: downloading }]}
      />

      <p className="govuk-body govuk-!-margin-top-4">
        <Link className="govuk-link govuk-link--no-visited-state" to={versionsListHref}>
          Return to previous submissions
        </Link>
      </p>
    </GeneralLayout>
  );
}

export default VersionDetail;
