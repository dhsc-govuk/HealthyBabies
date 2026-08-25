import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { getSubmissionVersions } from '../reviewQueries';
import { getLocalAuthoritySubmission } from '../LocalAuthorityView/queries';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

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

/** Admin-only list of a local authority's previous Delivery Plan submissions. */
function PreviousSubmissions(): React.ReactElement {
  const { submissionId: dataCollectionId, localAuthorityId } = useParams<{ submissionId: string; localAuthorityId: string }>();

  const { data: submissionData } = useQuery({
    queryKey: ['local-authority-submission', dataCollectionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(dataCollectionId!, localAuthorityId!),
    enabled: !!dataCollectionId && !!localAuthorityId,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['submission-versions', dataCollectionId, localAuthorityId],
    queryFn: () => getSubmissionVersions(dataCollectionId!, localAuthorityId!),
    enabled: !!dataCollectionId && !!localAuthorityId,
  });

  const submission = submissionData?.data;
  const versions = data?.data ?? [];

  const localAuthorityHref = ROUTES.admin.submissions.localAuthority(dataCollectionId, localAuthorityId);

  usePageTitle('Previous submissions');

  return (
    <LoadingSpinner loading={isLoading} label="Loading previous submissions">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Submissions', link: ROUTES.admin.submissions.list },
          ...(submission ? [{ label: submission.localAuthorityName, link: localAuthorityHref }] : []),
          ...(submission ? [{ label: submission.dataCollectionName, link: ROUTES.admin.submissions.view(dataCollectionId) }] : []),
        ]}
        currentPage="">
        <h1 className="govuk-heading-l">Previous submissions</h1>

        {versions.length === 0 ? (
          <p className="govuk-body">There are no previous submissions.</p>
        ) : (
          <table className="govuk-table">
            <caption className="govuk-visually-hidden">Previous submissions</caption>
            <thead className="govuk-table__head">
              <tr className="govuk-table__row">
                <th scope="col" className="govuk-table__header">
                  Version
                </th>
                <th scope="col" className="govuk-table__header">
                  Submitted by
                </th>
                <th scope="col" className="govuk-table__header">
                  Submitted on
                </th>
              </tr>
            </thead>
            <tbody className="govuk-table__body">
              {versions.map((version) => (
                <tr key={version.versionNumber} className="govuk-table__row">
                  <th scope="row" className="govuk-table__header">
                    <Link
                      className="govuk-link govuk-link--no-visited-state"
                      to={ROUTES.admin.submissions.versions.view(dataCollectionId, localAuthorityId, version.versionNumber)}>
                      Version {version.versionNumber}
                    </Link>
                  </th>
                  <td className="govuk-table__cell">{version.submittedBy ?? ''}</td>
                  <td className="govuk-table__cell">{formatDateTime(version.submittedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <p className="govuk-body govuk-!-margin-top-4">
          <Link className="govuk-link govuk-link--no-visited-state" to={localAuthorityHref}>
            Return to the most recent submission
          </Link>
        </p>
      </GeneralLayout>
    </LoadingSpinner>
  );
}

export default PreviousSubmissions;
