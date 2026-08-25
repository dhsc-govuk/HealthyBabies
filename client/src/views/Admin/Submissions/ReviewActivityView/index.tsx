import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../layouts';
import { GovUKInsetText, GovUKPagination, LoadingSpinner } from '../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../components/Global/ReviewAnswerGroups';
import { ROUTES } from '../../../../constants/routes';
import { getReviewActivity } from '../reviewQueries';
import usePageTitle from '../../../../hooks/usePageTitle';

/**
 * Read-only view of a single activity's answers within a Delivery Plan section, for admin reviewers.
 */
function ReviewActivityView(): React.ReactElement {
  const { submissionId, localAuthorityId, formModuleId, activityId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
    formModuleId: string;
    activityId: string;
  }>();

  const { data, isLoading, error } = useQuery({
    queryKey: ['review-activity', submissionId, localAuthorityId, formModuleId, activityId],
    queryFn: () => getReviewActivity(submissionId!, localAuthorityId!, formModuleId!, activityId!),
    enabled: !!submissionId && !!localAuthorityId && !!formModuleId && !!activityId,
  });

  const sectionHref = ROUTES.admin.submissions.reviewSection(submissionId, localAuthorityId, formModuleId);

  usePageTitle(data?.data?.activityName ?? 'Activity');

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

  const breadcrumbItems = [
    { label: 'Home', link: ROUTES.admin.home },
    { label: 'Submissions', link: ROUTES.admin.submissions.list },
    { label: activity.localAuthorityName, link: ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId) },
    { label: activity.dataCollectionName, link: ROUTES.admin.submissions.view(submissionId) },
    { label: activity.moduleName, link: sectionHref },
    { label: activity.activityName, link: '' },
  ];

  return (
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage="">
      <span className="govuk-caption-l">{activity.dataCollectionName}</span>
      <h1 className="govuk-heading-l">{activity.activityName}</h1>

      <GovUKInsetText>
        Local authority: <strong>{activity.localAuthorityName}</strong>
      </GovUKInsetText>

      <AnswerGroups groups={activity.answerGroups} />

      <GovUKPagination
        ariaLabel="Activity"
        previous={
          navigation.previous
            ? {
                title: 'Previous activity',
                label: `${navigation.previous.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.reviewActivity(submissionId, localAuthorityId, formModuleId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next activity',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.reviewActivity(submissionId, localAuthorityId, formModuleId, navigation.next.id),
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
}

export default ReviewActivityView;
