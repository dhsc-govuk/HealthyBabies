import React, { useMemo } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Link, LoadingBox, Button } from 'govuk-react';
import { GovUKNotificationBanner, GovUKTag } from '../../../../components/GovUKComponents';
import { GovUKTable } from '../../../../components/GovUKComponents';
import type { Column, FilterOption, TagColour } from '../../../../components/GovUKComponents';
import { getDataCollections, DataCollectionResponse } from './queries';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';


type DataCollectionStatus = 'draft' | 'planned' | 'open' | 'closed';

const toTitleCase = (str: string): string => {
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
};

const formatDate = (dateString: string | null): string => {
  if (!dateString) return '—';
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const getStatusTagColour = (status: DataCollectionStatus): TagColour => {
  switch (status) {
    case 'planned':
      return 'grey';
    case 'open':
      return 'green';
    case 'draft':
      return 'grey';
    default:
      return 'grey';
  }
};

interface LocationState {
  successMessage?: string;
}

const DataCollectionsList = (): React.ReactElement => {
  usePageTitle('Data collections');
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState | null;

  const { data: collectionsData, isLoading, isError, error } = useQuery({
    queryKey: ['data-collections-list'],
    queryFn: getDataCollections,
  });

  if (isError) {
    console.error('Error fetching data collections:', error);
  }

  const collections = useMemo(() => {
    return collectionsData?.data ?? [];
  }, [collectionsData]);

  const columns: Column<DataCollectionResponse>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (collection) => (
        <Link
          href={`/admin/data-collections/${collection.id}`}
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            navigate(`/admin/data-collections/${collection.id}`);
          }}
        >
          {collection.name}<span className="govuk-visually-hidden"> - view data collection</span>
        </Link>
      ),
    },
    {
      key: 'endDate',
      header: 'Due date',
      render: (collection) => formatDate(collection.endDate),
    },
    {
      key: 'status',
      header: '',
      align: 'right',
      render: (collection) => {
        const status = collection.status.toLowerCase() as DataCollectionStatus;
        if (status === 'closed') {
          return <span>{toTitleCase(status)}</span>;
        }
        return <GovUKTag colour={getStatusTagColour(status)}>{toTitleCase(status)}</GovUKTag>;
      },
    },
  ];

  const sortOptions: FilterOption[] = [
    { value: 'name-asc', label: 'Alphabetically A-Z' },
    { value: 'name-desc', label: 'Alphabetically Z-A' },
    { value: 'date-asc', label: 'Due date (earliest first)' },
    { value: 'date-desc', label: 'Due date (latest first)' },
  ];

  const filterOptions: FilterOption[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Planned', label: 'Planned' },
    { value: 'Open', label: 'Open' },
    { value: 'Closed', label: 'Closed' },
  ];

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
      ]}
    >
      {state?.successMessage && (
        <GovUKNotificationBanner
          type="success"
          title="Success"
        >
          <p className="govuk-notification-banner__heading">Data collection saved</p>
          <p className="govuk-body">{state.successMessage}</p>
        </GovUKNotificationBanner>
      )}

      <PageHeaderContainer>
        <PageTitle>Data collections</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/data-collections/create')}>
            Add data collection
          </Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <LoadingBox loading={isLoading}>
        <GovUKTable<DataCollectionResponse>
          data={collections}
          columns={columns}
          searchPlaceholder="Search by name"
          searchable={true}
          sortOptions={sortOptions}
          sortLabel="Sort list"
          filterOptions={filterOptions}
          filterLabel="Filter by status"
          filterPlaceholder="Select status..."
          filterField="status"
          keyExtractor={(collection) => collection.id}
          getRowHref={(collection) => `/admin/data-collections/${collection.id}`}
          emptyMessage="No data collections found"
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default DataCollectionsList;
