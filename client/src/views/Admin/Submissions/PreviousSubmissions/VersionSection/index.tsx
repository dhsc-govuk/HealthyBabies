import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKInsetText, GovUKPagination, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../../components/Global/ReviewAnswerGroups';
import { ActivitiesTable } from '../../../../../components/Global/ReviewActivitiesTable';
import { getSubmissionVersionSection } from '../../reviewQueries';
import { getLocalAuthoritySubmission } from '../../LocalAuthorityView/queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

/** Admin-only view of one section's answers within a previous Delivery Plan submission. */
function VersionSection(): React.ReactElement {
  const { submissionId: dataCollectionId, localAuthorityId, versionNumber, formModuleId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
    versionNumber: string;
    formModuleId: string;
  }>();
  const versionNum = Number(versionNumber);

  const { data: submissionData } = useQuery({
    queryKey: ['local-authority-submission', dataCollectionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(dataCollectionId!, localAuthorityId!),
    enabled: !!dataCollectionId && !!localAuthorityId,
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-section', dataCollectionId, localAuthorityId, versionNum, formModuleId],
    queryFn: () => getSubmissionVersionSection(dataCollectionId!, localAuthorityId!, versionNum, formModuleId!),
    enabled: !!dataCollectionId && !!localAuthorityId && !!versionNumber && !!formModuleId,
  });

  const localAuthorityName = submissionData?.data?.localAuthorityName;
  const dataCollectionName = submissionData?.data?.dataCollectionName;

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
  const detailHref = ROUTES.admin.submissions.versions.view(dataCollectionId, localAuthorityId, versionNum);

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.admin.home },
        { label: 'Submissions', link: ROUTES.admin.submissions.list },
        ...(localAuthorityName ? [{ label: localAuthorityName, link: ROUTES.admin.submissions.localAuthority(dataCollectionId, localAuthorityId) }] : []),
        ...(dataCollectionName ? [{ label: dataCollectionName, link: ROUTES.admin.submissions.view(dataCollectionId) }] : []),
        { label: 'Previous submissions', link: ROUTES.admin.submissions.versions.list(dataCollectionId, localAuthorityId) },
        { label: `Version ${versionNum}`, link: detailHref },
        { label: section.name, link: '' },
      ]}
      currentPage="">
      <h1 className="govuk-heading-l">{section.name}</h1>

      {localAuthorityName && (
        <GovUKInsetText>
          Local authority: <strong>{localAuthorityName}</strong>
        </GovUKInsetText>
      )}

      {section.allowsMultipleActivities ? (
        <ActivitiesTable
          activities={section.activities}
          activityHref={(activityId) => ROUTES.admin.submissions.versions.activity(dataCollectionId, localAuthorityId, versionNum, formModuleId, activityId)}
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
                href: ROUTES.admin.submissions.versions.section(dataCollectionId, localAuthorityId, versionNum, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next section',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.versions.section(dataCollectionId, localAuthorityId, versionNum, navigation.next.id),
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
}

export default VersionSection;
