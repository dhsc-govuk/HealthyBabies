import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKPagination, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../../components/Global/ReviewAnswerGroups';
import { useAuthProvider } from '../../../../../components/AuthProvider';
import { getSubmissionVersionActivity } from '../../queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

/** One activity's own answers within a previous submission's activity-based section. */
const VersionActivity = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { user } = useAuthProvider();
  const { submissionId, versionNumber, formModuleId, activityId } = useParams<{
    submissionId: string;
    versionNumber: string;
    formModuleId: string;
    activityId: string;
  }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const submissionsListHref = apiBase === 'organisation-admin' ? ROUTES.organisationAdmin.submissions.list : ROUTES.deliveryPlan.submissions.list;
  const versionNum = Number(versionNumber);

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-activity', submissionId, versionNum, formModuleId, activityId],
    queryFn: () => getSubmissionVersionActivity(submissionId!, versionNum, formModuleId!, activityId!, apiBase),
    enabled: !!submissionId && !!versionNumber && !!formModuleId && !!activityId,
  });

  usePageTitle(data?.data?.activityName ?? 'Previous submission');

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
        <p className="govuk-body">Failed to load this activity.</p>
      </GeneralLayout>
    );
  }

  const activity = data.data;
  const { navigation } = activity;
  const sectionHref = ROUTES.areaSubmissions.versions.section(apiBase, submissionId, versionNum, formModuleId);

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.areaHome(apiBase) },
        { label: user?.organisation_name ?? 'Local Authority', link: submissionsListHref },
        { label: activity.dataCollectionName, link: ROUTES.areaSubmissions.view(apiBase, submissionId) },
        { label: 'Previous submissions', link: ROUTES.areaSubmissions.versions.list(apiBase, submissionId) },
        { label: `Version ${versionNum}`, link: ROUTES.areaSubmissions.versions.view(apiBase, submissionId, versionNum) },
        { label: activity.moduleName, link: sectionHref },
        { label: activity.activityName, link: '' },
      ]}
      currentPage="">
      <h1 className="govuk-heading-l">{activity.activityName}</h1>

      <AnswerGroups groups={activity.answerGroups} />

      <GovUKPagination
        ariaLabel="Activity"
        previous={
          navigation.previous
            ? {
                title: 'Previous activity',
                label: `${navigation.previous.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.versions.activity(apiBase, submissionId, versionNum, formModuleId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next activity',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.versions.activity(apiBase, submissionId, versionNum, formModuleId, navigation.next.id),
              }
            : undefined
        }
      />

      <p className="govuk-body govuk-!-margin-top-4">
        <Link className="govuk-link govuk-link--no-visited-state" to={sectionHref}>
          Return to section summary
        </Link>
      </p>
    </GeneralLayout>
  );
};

export default VersionActivity;
