import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Link, LoadingBox, Button } from 'govuk-react';
import { GovUKTable } from '../../../../components/GovUKComponents';
import type { Column } from '../../../../components/GovUKComponents';
import { getAllOrganisationUsers, OrganisationUserResponse } from './queries';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

interface LocationState {
  created?: boolean;
  temporaryPassword?: string | null;
}

const OrganisationUsersList = (): React.ReactElement => {
  usePageTitle('LA users');
  const navigate = useNavigate();
  const location = useLocation();
  const [showSuccessBanner, setShowSuccessBanner] = useState(false);
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null);

  useEffect(() => {
    const state = location.state as LocationState;
    if (state?.created) {
      setShowSuccessBanner(true);
      setTemporaryPassword(state.temporaryPassword ?? null);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const { data: orgUsersData, isLoading } = useQuery({
    queryKey: ['la-users-list'],
    queryFn: () => getAllOrganisationUsers(100, 1),
  });

  const orgUserColumns: Column<OrganisationUserResponse>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (user) => (
        <Link href={`/admin/la-users/${user.id}`} onClick={(e: React.MouseEvent) => { e.preventDefault(); navigate(`/admin/la-users/${user.id}`); }}>
          {user.firstName} {user.lastName}<span className="govuk-visually-hidden"> - view LA user</span>
        </Link>
      ),
    },
    {
      key: 'organisation',
      header: 'Local authority',
      render: (user) => user.organisation ?? '-',
    },
    {
      key: 'role',
      header: 'Role',
      render: (user) => user.role ?? '-',
    },
    {
      key: 'isActive',
      header: '',
      align: 'right',
      render: (user) => (
        user.isActive ? (
          <span>Active</span>
        ) : (
          <span className="govuk-tag" style={{ textTransform: 'none', backgroundColor: '#fff7bf', color: '#594d00' }}>Inactive</span>
        )
      ),
    },
  ];

  const sortOptions = [
    { value: 'name-asc', label: 'Name (A-Z)' },
    { value: 'name-desc', label: 'Name (Z-A)' },
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
              LA user saved.
            </h3>
            <p className="govuk-body">
              The LA user has been added to your list of LA users. You can view, change, or delete the user account at any time.
            </p>
            {/* TODO: remove temporary password from the banner once email delivery is reliable */}
            {temporaryPassword && (
              <p className="govuk-body">
                Temporary password: <strong>{temporaryPassword}</strong>
              </p>
            )}
          </div>
        </div>
      )}

      <PageHeaderContainer>
        <PageTitle>LA users</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/la-users/create')}>Add LA user</Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <LoadingBox loading={isLoading}>
        <GovUKTable<OrganisationUserResponse>
          data={orgUsersData?.data.items ?? []}
          columns={orgUserColumns}
          searchPlaceholder="Search LA user by name"
          searchable={true}
          sortOptions={sortOptions}
          keyExtractor={(user) => user.id}
          getRowHref={(user) => `/admin/la-users/${user.id}`}
          emptyMessage="No LA users found"
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default OrganisationUsersList;
