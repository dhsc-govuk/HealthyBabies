import React from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../layouts';
import { GovUKPagination, LoadingSpinner } from '../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../components/Global/ReviewAnswerGroups';
import { ActivitiesTable } from '../../../../components/Global/ReviewActivitiesTable';
import { getReviewSection } from '../queries';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

/**
 * Read-only view of one section of the caller's own accepted Delivery Plan submission - the live
 * current data. Shared by organisation-admin and delivery-plan-lead, same pattern as VersionSection.
 */
const ReviewSection = (): React.ReactElement => {
  const routerLocation = useLocation();
  const { submissionId, formModuleId } = useParams<{ submissionId: string; formModuleId: string }>();
  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const submissionsListHref = apiBase === 'organisation-admin' ? ROUTES.organisationAdmin.submissions.list : ROUTES.deliveryPlan.submissions.list;

  const { data, isLoading, error } = useQuery({
    queryKey: ['review-section', submissionId, formModuleId],
    queryFn: () => getReviewSection(submissionId!, formModuleId!, apiBase),
    enabled: !!submissionId && !!formModuleId,
  });

  usePageTitle(data?.data?.name ?? 'Submission');

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
  const summaryHref = ROUTES.areaSubmissions.view(apiBase, submissionId);

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.areaHome(apiBase) },
        { label: section.localAuthorityName, link: submissionsListHref },
        { label: section.dataCollectionName, link: summaryHref },
        { label: section.name, link: '' },
      ]}
      currentPage="">
      <h1 className="govuk-heading-l">{section.name}</h1>

      {section.allowsMultipleActivities ? (
        <ActivitiesTable
          activities={section.activities}
          activityHref={(activityId) => ROUTES.areaSubmissions.review.activity(apiBase, submissionId, formModuleId, activityId)}
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
                href: ROUTES.areaSubmissions.review.section(apiBase, submissionId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next section',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.areaSubmissions.review.section(apiBase, submissionId, navigation.next.id),
              }
            : undefined
        }
      />

      <p className="govuk-body govuk-!-margin-top-4">
        <Link className="govuk-link govuk-link--no-visited-state" to={summaryHref}>
          Return to submission summary
        </Link>
      </p>
    </GeneralLayout>
  );
};

export default ReviewSection;
