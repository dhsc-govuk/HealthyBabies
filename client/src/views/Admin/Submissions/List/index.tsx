import React, { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKTaskList } from '../../../../components/GovUKComponents';
import type { TaskListItem, TagColour } from '../../../../components/GovUKComponents';
import {
  getSubmissions,
  getSubmissionStatus,
  isOngoingOrUpcoming,
  SubmissionDataCollection,
  SubmissionStatus,
} from './queries';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const getStatusTagColour = (status: SubmissionStatus): TagColour => {
  switch (status) {
    case 'Planned':
      return 'grey';
    case 'Open':
      return 'green';
    case 'Closed':
      return 'grey';
    default:
      return 'grey';
  }
};

const mapOngoingCollectionToTaskItem = (
  collection: SubmissionDataCollection,
  navigate: (path: string) => void
): TaskListItem => {
  const status = getSubmissionStatus(collection);

  return {
    id: collection.id,
    title: collection.name,
    hint: `Due date ${formatDate(collection.endDate)}`,
    href: `/admin/submissions/${collection.id}`,
    onClick: () => navigate(`/admin/submissions/${collection.id}`),
    status: {
      text: status,
      tag: {
        text: status,
        colour: getStatusTagColour(status),
      },
    },
  };
};

const mapPastCollectionToTaskItem = (
  collection: SubmissionDataCollection,
  navigate: (path: string) => void
): TaskListItem => {
  return {
    id: collection.id,
    title: collection.name,
    href: `/admin/submissions/${collection.id}`,
    onClick: () => navigate(`/admin/submissions/${collection.id}`),
    status: {
      text: 'Closed',
    },
  };
};

function SubmissionsList(): React.ReactElement {
  const navigate = useNavigate();

  const { data: collectionsData, isLoading, isError, error } = useQuery({
    queryKey: ['submissions-list'],
    queryFn: getSubmissions,
  });

  if (isError) {
    console.error('Error fetching submissions:', error);
  }

  const { ongoingAndUpcoming, past } = useMemo(() => {
    const collections = collectionsData?.data ?? [];

    const ongoing = collections
      .filter(isOngoingOrUpcoming)
      .sort((a, b) => new Date(a.endDate).getTime() - new Date(b.endDate).getTime());

    const closed = collections
      .filter((c) => !isOngoingOrUpcoming(c))
      .sort((a, b) => new Date(b.endDate).getTime() - new Date(a.endDate).getTime());

    return {
      ongoingAndUpcoming: ongoing,
      past: closed,
    };
  }, [collectionsData]);

  const ongoingItems = useMemo(
    () => ongoingAndUpcoming.map((c) => mapOngoingCollectionToTaskItem(c, navigate)),
    [ongoingAndUpcoming, navigate]
  );

  const pastItems = useMemo(
    () => past.map((c) => mapPastCollectionToTaskItem(c, navigate)),
    [past, navigate]
  );

  return (
    <GeneralLayout
      breadcrumbs={[{ label: 'Home', link: '/admin/home' }]}
      currentPage="Submissions"
    >
      <p className="govuk-body">
        View all data submissions and their status. Check which collections are open,
        planned or closed, and access details for each of them.
      </p>

      <LoadingBox loading={isLoading}>
        <div className="govuk-!-margin-top-6">
          <h2 className="govuk-heading-m">Ongoing and upcoming submissions</h2>
          {ongoingItems.length > 0 ? (
            <GovUKTaskList items={ongoingItems} idPrefix="ongoing-submissions" />
          ) : (
            <p className="govuk-body govuk-!-colour-secondary">
              No ongoing or upcoming submissions.
            </p>
          )}
        </div>

        <div className="govuk-!-margin-top-8">
          <h2 className="govuk-heading-m">Past submissions</h2>
          {pastItems.length > 0 ? (
            <GovUKTaskList items={pastItems} idPrefix="past-submissions" />
          ) : (
            <p className="govuk-body govuk-!-colour-secondary">
              No past submissions.
            </p>
          )}
        </div>
      </LoadingBox>
    </GeneralLayout>
  );
}

export default SubmissionsList;
