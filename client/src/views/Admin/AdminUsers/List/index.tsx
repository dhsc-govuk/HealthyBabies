import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Link, LoadingBox, Button } from 'govuk-react';
import { GovUKTable } from '../../../../components/GovUKComponents';
import type { Column } from '../../../../components/GovUKComponents';
import { getAdmins, GetAdminsResponse } from './queries';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const AdminUsersList = (): React.ReactElement => {
  usePageTitle('Departmental users');
  const navigate = useNavigate();

  const { data: adminsData, isLoading, isError, error } = useQuery({
    queryKey: ['departmental-users-list'],
    queryFn: getAdmins,
  });

  if (isError) {
    console.error('Error fetching admin users:', error);
  }

  const adminColumns: Column<GetAdminsResponse>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (user) => (
        <Link href={`/admin/departmental-users/${user.id}`} onClick={(e: React.MouseEvent) => { e.preventDefault(); navigate(`/admin/departmental-users/${user.id}`); }}>
          {user.firstName} {user.lastName}<span className="govuk-visually-hidden"> - view departmental user</span>
        </Link>
      ),
    },
    {
      key: 'role',
      header: 'Role',
      render: () => 'Admin',
    },
    {
      key: 'isActive',
      header: 'Status',
      align: 'right',
      render: (user) => (
        <span style={user.isActive ? {} : { backgroundColor: '#fff7bf', padding: '2px 6px' }}>
          {user.isActive ? 'Active' : 'Inactive'}
        </span>
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
      <PageHeaderContainer>
        <PageTitle>Departmental users</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/departmental-users/create')}>Add departmental user</Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <LoadingBox loading={isLoading}>
        <GovUKTable<GetAdminsResponse>
          data={adminsData?.data ?? []}
          columns={adminColumns}
          searchPlaceholder="Search departmental user by name"
          searchable={true}
          sortOptions={sortOptions}
          keyExtractor={(user) => user.id}
          getRowHref={(user) => `/admin/departmental-users/${user.id}`}
          emptyMessage="No departmental users found"
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default AdminUsersList;
