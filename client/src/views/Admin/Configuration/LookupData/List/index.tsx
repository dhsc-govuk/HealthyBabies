import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Link, LoadingBox, Button } from 'govuk-react';
import { GovUKTable } from '../../../../../components/GovUKComponents';
import type { Column } from '../../../../../components/GovUKComponents';
import { getAllGlobalData } from './queries';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../../styles/govuk-global';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';
import usePageTitle from '../../../../../hooks/usePageTitle';

const LookupDataList = (): React.ReactElement => {
  usePageTitle('Lookup data');
  const navigate = useNavigate();

  const { data, isLoading } = useQuery({
    queryKey: ['global-data-list'],
    queryFn: getAllGlobalData,
  });

  const columns: Column<GlobalDataDto>[] = [
    {
      key: 'entity',
      header: 'Entity',
    },
    {
      key: 'value',
      header: 'Value',
      render: (item) => (
        <Link href={`/admin/configuration/lookup-data/${item.id}`} onClick={(e: React.MouseEvent) => { e.preventDefault(); navigate(`/admin/configuration/lookup-data/${item.id}`); }}>
          {item.value}<span className="govuk-visually-hidden"> - view lookup data item</span>
        </Link>
      ),
    },
    {
      key: 'description',
      header: 'Description',
      render: (item) => item.description || '-',
    },
  ];

  const sortOptions = [
    { value: 'entity-asc', label: 'Entity (A-Z)' },
    { value: 'entity-desc', label: 'Entity (Z-A)' },
    { value: 'value-asc', label: 'Value (A-Z)' },
  ];

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
      ]}>
      <PageHeaderContainer>
        <PageTitle>Lookup Data</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/configuration/lookup-data/create')}>Create</Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <LoadingBox loading={isLoading}>
        <GovUKTable<GlobalDataDto>
          data={data?.data ?? []}
          columns={columns}
          searchPlaceholder="Search lookup data"
          searchable={true}
          sortOptions={sortOptions}
          keyExtractor={(item) => item.id}
          getRowHref={(item) => `/admin/configuration/lookup-data/${item.id}`}
          emptyMessage="No lookup data found"
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default LookupDataList;
