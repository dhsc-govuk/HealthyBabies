import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { GovUKInsetText, GovUKPagination, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { AnswerGroups } from '../../../../../components/Global/ReviewAnswerGroups';
import { getSubmissionVersionActivity } from '../../reviewQueries';
import { getLocalAuthoritySubmission } from '../../LocalAuthorityView/queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

/** Admin-only view of a single activity's answers within a previous submission's activity-based section. */
function VersionActivity(): React.ReactElement {
  const { submissionId: dataCollectionId, localAuthorityId, versionNumber, formModuleId, activityId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
    versionNumber: string;
    formModuleId: string;
    activityId: string;
  }>();
  const versionNum = Number(versionNumber);

  const { data: submissionData } = useQuery({
    queryKey: ['local-authority-submission', dataCollectionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(dataCollectionId!, localAuthorityId!),
    enabled: !!dataCollectionId && !!localAuthorityId,
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['submission-version-activity', dataCollectionId, localAuthorityId, versionNum, formModuleId, activityId],
    queryFn: () => getSubmissionVersionActivity(dataCollectionId!, localAuthorityId!, versionNum, formModuleId!, activityId!),
    enabled: !!dataCollectionId && !!localAuthorityId && !!versionNumber && !!formModuleId && !!activityId,
  });

  const submission = submissionData?.data;
  const sectionHref = ROUTES.admin.submissions.versions.section(dataCollectionId, localAuthorityId, versionNum, formModuleId);

  usePageTitle(data?.data?.activityName ?? 'Previous submission');

  if (isLoading) {
    return (
      <GeneralLayout backLink={{ href: sectionHref }}>
        <LoadingSpinner />
      </GeneralLayout>
    );
  }

  if (error || !data?.data) {
    return (
      <GeneralLayout backLink={{ href: sectionHref }}>
        <p className="govuk-body">Failed to load this activity.</p>
      </GeneralLayout>
    );
  }

  const activity = data.data;
  const { navigation } = activity;

  const breadcrumbItems = [
    { label: 'Home', link: ROUTES.admin.home },
    { label: 'Submissions', link: ROUTES.admin.submissions.list },
    ...(submission ? [{ label: submission.localAuthorityName, link: ROUTES.admin.submissions.localAuthority(dataCollectionId, localAuthorityId) }] : []),
    ...(submission ? [{ label: submission.dataCollectionName, link: ROUTES.admin.submissions.view(dataCollectionId) }] : []),
    { label: 'Previous submissions', link: ROUTES.admin.submissions.versions.list(dataCollectionId, localAuthorityId) },
    { label: `Version ${versionNum}`, link: ROUTES.admin.submissions.versions.view(dataCollectionId, localAuthorityId, versionNum) },
    { label: activity.moduleName, link: sectionHref },
    { label: activity.activityName, link: '' },
  ];

  return (
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage="">
      {submission && <span className="govuk-caption-l">{submission.dataCollectionName}</span>}
      <h1 className="govuk-heading-l">{activity.activityName}</h1>

      {submission && (
        <GovUKInsetText>
          Local authority: <strong>{submission.localAuthorityName}</strong>
        </GovUKInsetText>
      )}

      <AnswerGroups groups={activity.answerGroups} />

      <GovUKPagination
        ariaLabel="Activity"
        previous={
          navigation.previous
            ? {
                title: 'Previous activity',
                label: `${navigation.previous.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.versions.activity(dataCollectionId, localAuthorityId, versionNum, formModuleId, navigation.previous.id),
              }
            : undefined
        }
        next={
          navigation.next
            ? {
                title: 'Next activity',
                label: `${navigation.next.position} of ${navigation.total}`,
                href: ROUTES.admin.submissions.versions.activity(dataCollectionId, localAuthorityId, versionNum, formModuleId, navigation.next.id),
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

export default VersionActivity;
