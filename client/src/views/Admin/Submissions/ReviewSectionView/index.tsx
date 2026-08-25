import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../layouts';
import { GovUKInsetText, GovUKPagination, LoadingSpinner } from '../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../components/Global/ReviewAnswerGroups';
import { ActivitiesTable } from '../../../../components/Global/ReviewActivitiesTable';
import { ROUTES } from '../../../../constants/routes';
import { getReviewSection } from '../reviewQueries';
import usePageTitle from '../../../../hooks/usePageTitle';

/**
 * Read-only view of one section of a submitted Delivery Plan, for admin reviewers.
 *
 * Serves both section shapes. A section without activity records shows its answers directly; a
 * section with activity records lists each activity and links through to its own answers. Which one
 * applies is decided by the section, not by this component's caller, so the summary page can link
 * every section to the same route.
 */
function ReviewSectionView(): React.ReactElement {
  const { submissionId, localAuthorityId, formModuleId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
    formModuleId: string;
  }>();

  const { data, isLoading, error } = useQuery({
    queryKey: ['review-section', submissionId, localAuthorityId, formModuleId],
    queryFn: () => getReviewSection(submissionId!, localAuthorityId!, formModuleId!),
    enabled: !!submissionId && !!localAuthorityId && !!formModuleId,
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

  const breadcrumbItems = [
    { label: 'Home', link: ROUTES.admin.home },
    { label: 'Submissions', link: ROUTES.admin.submissions.list },
    { label: section.localAuthorityName, link: ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId) },
    { label: section.dataCollectionName, link: ROUTES.admin.submissions.view(submissionId) },
    { label: section.name, link: '' },
  ];

  const summaryHref = ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId);

  return (
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage="">
      <span className="govuk-caption-l">{section.dataCollectionName}</span>
      <h1 className="govuk-heading-l">{section.name}</h1>

      <GovUKInsetText>
        Local authority: <strong>{section.localAuthorityName}</strong>
      </GovUKInsetText>

      {section.allowsMultipleActivities ? (
        <ActivitiesTable
          activities={section.activities}
          activityHref={(activityId) => ROUTES.admin.submissions.reviewActivity(submissionId, localAuthorityId, formModuleId, activityId)}
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
                href: ROUTES.admin.submissions.reviewSection(submissionId, localAuthorityId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next section',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.reviewSection(submissionId, localAuthorityId, navigation.next.id),
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
}

export default ReviewSectionView;
