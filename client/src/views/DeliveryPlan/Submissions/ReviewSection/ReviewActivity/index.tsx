import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKPagination, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../../components/Global/ReviewAnswerGroups';
import { getReviewActivity } from '../../queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

/**
 * Read-only view of one activity's answers within the caller's own accepted Delivery Plan
 * submission - the live current data. Shared by organisation-admin and delivery-plan-lead.
 */
const ReviewActivity = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { submissionId, formModuleId, activityId } = useParams<{
    submissionId: string;
    formModuleId: string;
    activityId: string;
  }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const submissionsListHref = apiBase === 'organisation-admin' ? ROUTES.organisationAdmin.submissions.list : ROUTES.deliveryPlan.submissions.list;

  const { data, isLoading, error } = useQuery({
    queryKey: ['review-activity', submissionId, formModuleId, activityId],
    queryFn: () => getReviewActivity(submissionId!, formModuleId!, activityId!, apiBase),
    enabled: !!submissionId && !!formModuleId && !!activityId,
  });

  usePageTitle(data?.data?.activityName ?? 'Submission');

  const sectionHref = ROUTES.areaSubmissions.review.section(apiBase, submissionId, formModuleId);

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

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.areaHome(apiBase) },
        { label: activity.localAuthorityName, link: submissionsListHref },
        { label: activity.dataCollectionName, link: ROUTES.areaSubmissions.view(apiBase, submissionId) },
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
                href: ROUTES.areaSubmissions.review.activity(apiBase, submissionId, formModuleId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next activity',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.review.activity(apiBase, submissionId, formModuleId, navigation.next.id),
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

export default ReviewActivity;
