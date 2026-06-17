import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Link, LoadingBox, Button } from 'govuk-react';
import { GovUKTable, GovUKTag } from '../../../../components/GovUKComponents';
import type { Column } from '../../../../components/GovUKComponents';
import { getOrganisations, Organisation } from './queries';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../layouts';
import { PageHeaderContainer, PageHeaderActions } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

interface LocationState {
  created?: boolean;
}

const ListOrganisation = (): React.ReactElement => {
  usePageTitle('Local authorities');
  const navigate = useNavigate();
  const location = useLocation();
  const [showSuccessBanner, setShowSuccessBanner] = useState(false);

  useEffect(() => {
    const state = location.state as LocationState;
    if (state?.created) {
      setShowSuccessBanner(true);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-organisations-list'],
    queryFn: getOrganisations,
  });

  const columns: Column<Organisation>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (org) => (
        <Link href={`/admin/organisations/${org.id}`} onClick={(e: React.MouseEvent) => { e.preventDefault(); navigate(`/admin/organisations/${org.id}`); }}>
          {org.name}<span className="govuk-visually-hidden"> - view local authority</span>
        </Link>
      ),
    },
    {
      key: 'onsCode',
      header: 'ONS code',
      render: (org) => org.onsCode ?? '-',
    },
    {
      key: 'isActive',
      header: '',
      align: 'right',
      render: (org) => (
        org.isActive ? (
          <GovUKTag colour="blue">Active</GovUKTag>
        ) : (
          <GovUKTag colour="grey">Inactive</GovUKTag>
        )
      ),
    },
  ];

  const sortOptions = [
    { value: 'name-asc', label: 'Alphabetically A-Z' },
    { value: 'name-desc', label: 'Alphabetically Z-A' },
    { value: 'status-asc', label: 'By status' },
  ];

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
      ]}>
      {showSuccessBanner && (
        <div className="govuk-notification-banner govuk-notification-banner--success" role="alert" aria-labelledby="govuk-notification-banner-title">
          <div className="govuk-notification-banner__header">
            <h2 className="govuk-notification-banner__title" id="govuk-notification-banner-title">
              Success
            </h2>
          </div>
          <div className="govuk-notification-banner__content">
            <h3 className="govuk-notification-banner__heading">
              Local authority saved.
            </h3>
            <p className="govuk-body">
              The local authority has been added to your list of local authorities. You can view, change, or delete the user account at any time.
            </p>
          </div>
        </div>
      )}

      <PageHeaderContainer>
        <h1 className="govuk-heading-l">Local authorities</h1>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/organisations/create')}>Add local authority</Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <LoadingBox loading={isLoading}>
        <GovUKTable<Organisation>
          data={data?.data ?? []}
          columns={columns}
          searchPlaceholder="Search local authority by name or ONS code"
          searchable={true}
          sortOptions={sortOptions}
          sortLabel="Sort local authorities"
          keyExtractor={(org) => org.id}
          getRowHref={(org) => `/admin/organisations/${org.id}`}
          emptyMessage="No local authorities found"
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default ListOrganisation;
