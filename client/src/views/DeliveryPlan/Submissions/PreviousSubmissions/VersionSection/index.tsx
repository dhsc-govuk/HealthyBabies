import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKPagination, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../../components/Global/ReviewAnswerGroups';
import { ActivitiesTable } from '../../../../../components/Global/ReviewActivitiesTable';
import { useAuthProvider } from '../../../../../components/AuthProvider';
import { getSubmissionVersionSection } from '../../queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

/**
 * One section's answers within a previous submission, read from the frozen snapshot. Renders one
 * summary card per activity/record for a multi-record module rather than a separate drill-down page
 * per activity - there is no add/edit/delete concern driving the live UI's one-at-a-time navigation
 * here, since the view is read-only.
 */
const VersionSection = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { user } = useAuthProvider();
  const { submissionId, versionNumber, formModuleId } = useParams<{
    submissionId: string;
    versionNumber: string;
    formModuleId: string;
  }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const submissionsListHref = apiBase === 'organisation-admin' ? ROUTES.organisationAdmin.submissions.list : ROUTES.deliveryPlan.submissions.list;
  const versionNum = Number(versionNumber);

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-section', submissionId, versionNum, formModuleId],
    queryFn: () => getSubmissionVersionSection(submissionId!, versionNum, formModuleId!, apiBase),
    enabled: !!submissionId && !!versionNumber && !!formModuleId,
  });

  usePageTitle(data?.data?.name ?? 'Previous submission');

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
        <p className="govuk-body">Failed to load this section.</p>
      </GeneralLayout>
    );
  }

  const section = data.data;
  const { navigation } = section;
  const detailHref = ROUTES.areaSubmissions.versions.view(apiBase, submissionId, versionNum);

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.areaHome(apiBase) },
        { label: user?.organisation_name ?? 'Local Authority', link: submissionsListHref },
        { label: section.dataCollectionName, link: ROUTES.areaSubmissions.view(apiBase, submissionId) },
        { label: 'Previous submissions', link: ROUTES.areaSubmissions.versions.list(apiBase, submissionId) },
        { label: `Version ${versionNum}`, link: detailHref },
        { label: section.name, link: '' },
      ]}
      currentPage="">
      <h1 className="govuk-heading-l">{section.name}</h1>

      {section.allowsMultipleActivities ? (
        <ActivitiesTable
          activities={section.activities}
          activityHref={(activityId) => ROUTES.areaSubmissions.versions.activity(apiBase, submissionId, versionNum, formModuleId, activityId)}
        />
      ) : (
        <AnswerGroups groups={section.answerGroups} />
      )}

      <GovUKPagination
        ariaLabel="Section"
        previous={
          navigation.previous
            ? {
                title: 'Previous section',
                label: `${navigation.previous.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.versions.section(apiBase, submissionId, versionNum, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next section',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.versions.section(apiBase, submissionId, versionNum, navigation.next.id),
              }
            : undefined
        }
      />

      <p className="govuk-body govuk-!-margin-top-4">
        <Link className="govuk-link govuk-link--no-visited-state" to={detailHref}>
          Return to previous submission
        </Link>
      </p>
    </GeneralLayout>
  );
};

export default VersionSection;
