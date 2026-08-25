import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKNotificationBanner, LoadingSpinner, SummaryList } from '../../../../../components/GovUKComponents';
import { SubmissionFileCard } from '../../../../../components/Global/SubmissionFileCard';
import { useAuthProvider } from '../../../../../components/AuthProvider';
import { getSubmissionVersionDetail, downloadSubmissionVersion, getSubmissionVersionDownloadFileName } from '../../queries';
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

/**
 * Header for one previous round of a submission - who submitted it, why DHSC rejected it, and the
 * sections it contains, all read from the frozen snapshot rather than the live submission.
 */
const VersionDetail = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { user } = useAuthProvider();
  const { submissionId, versionNumber } = useParams<{ submissionId: string; versionNumber: string }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const versionNum = Number(versionNumber);

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-detail', submissionId, versionNum],
    queryFn: () => getSubmissionVersionDetail(submissionId!, versionNum, apiBase),
    enabled: !!submissionId && !!versionNumber,
  });

  const { mutate: download, isLoading: downloading } = useMutation({
    mutationFn: () => downloadSubmissionVersion(submissionId!, versionNum, apiBase),
    onSuccess: (blob) => downloadFile(blob, getSubmissionVersionDownloadFileName(versionNum)),
  });

  usePageTitle('Previous submission');

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
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.areaHome(apiBase) },
        { label: 'Submission', link: ROUTES.areaSubmissions.view(apiBase, submissionId) },
        { label: 'Previous submissions', link: ROUTES.areaSubmissions.versions.list(apiBase, submissionId) },
      ]}
      currentPage="">
      <GovUKNotificationBanner title="Previous submission">
        <p className="govuk-body">This is a previous submission. It is shown for reference only and cannot be edited.</p>
      </GovUKNotificationBanner>

      <h1 className="govuk-heading-l">Version {version.versionNumber}</h1>

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
                  to={ROUTES.areaSubmissions.versions.section(apiBase, submissionId, versionNum, section.formModuleId)}>
                  Section {section.sectionNumber}: {section.name}
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}

      <h2 className="govuk-heading-m govuk-!-margin-top-6">Files</h2>
      <SubmissionFileCard
        name={`${user?.organisation_name ?? 'Local Authority'} Delivery Plan (${version.versionNumber})`}
        subtitle={version.submittedBy && version.submittedAt ? `Submitted by ${version.submittedBy} on ${formatDateTime(version.submittedAt)}` : undefined}
        downloads={[{ label: downloading ? 'Downloading...' : 'Download in ZIP', onClick: () => download(), disabled: downloading }]}
      />

      <p className="govuk-body govuk-!-margin-top-4">
        <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.versions.list(apiBase, submissionId)}>
          Return to previous submissions
        </Link>
      </p>
    </GeneralLayout>
  );
};

export default VersionDetail;
