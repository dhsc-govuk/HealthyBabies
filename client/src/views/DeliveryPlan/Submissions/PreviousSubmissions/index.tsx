import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { getSubmissionVersions } from '../queries';
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

/**
 * List of a submission's previous rounds - each one a snapshot frozen at the moment DHSC rejected
 * it. Shared across organisation-admin and delivery-plan-lead: both are local authority users who
 * can view their own submission history, distinguished only by which area's route they're under.
 */
const PreviousSubmissions = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { submissionId } = useParams<{ submissionId: string }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';

  const { data, isLoading } = useQuery({
    queryKey: ['submission-versions', submissionId],
    queryFn: () => getSubmissionVersions(submissionId!, apiBase),
    enabled: !!submissionId,
  });

  const versions = data?.data ?? [];

  usePageTitle('Previous submissions');

  return (
    <LoadingSpinner loading={isLoading} label="Loading previous submissions">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.areaHome(apiBase) },
          { label: 'Submission', link: ROUTES.areaSubmissions.view(apiBase, submissionId) },
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
                      to={ROUTES.areaSubmissions.versions.view(apiBase, submissionId, version.versionNumber)}>
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
          <Link className="govuk-link govuk-link--no-visited-state" to={ROUTES.areaSubmissions.view(apiBase, submissionId)}>
            Return to the most recent submission
          </Link>
        </p>
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default PreviousSubmissions;
