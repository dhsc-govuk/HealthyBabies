import React from 'react';
import { Link } from 'govuk-react';
import { LoadingSpinner, GovUKTable } from '../../../../../components/GovUKComponents';
import type { Column } from '../../../../../components/GovUKComponents';
import { capitaliseFirst } from '../../../../../helpers/stringUtils';
import { getOrganisationUsers, OrganisationUserDto } from './queries';
import { useQuery } from 'react-query';

interface Props {
  organisationId: string;
  handleView: (userId: string) => void;
  handleEdit: (userId: string) => void;
}

const ListOrganisationUsersComponent = ({ organisationId, handleView, handleEdit }: Props): React.ReactElement => {
  const { data: usersData, isLoading: usersLoading } = useQuery({
    queryKey: 'organisations-admins-list',
    queryFn: () => getOrganisationUsers(organisationId!),
  });

  const columns: Column<OrganisationUserDto>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (user) => (
        <Link
          href="#"
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            handleView(user.id);
          }}>
          {user.firstName} {user.lastName}
        </Link>
      ),
    },
    {
      key: 'email',
      header: 'Email',
      render: (user) => user.email,
    },
    {
      key: 'role',
      header: 'Role',
      render: (user) => capitaliseFirst(user.role === 'organisation admin' ? 'Admin' : user.role),
    },
    {
      key: 'isActive',
      header: 'Active',
      render: (user) => (user.isActive ? 'Yes' : 'No'),
    },
    {
      key: 'mfaStatus',
      header: 'MFA',
      render: (user) => user.mfaStatus ?? 'Pending Setup',
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (user) => (
        <div className="govuk-table__actions">
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleView(user.id);
            }}>
            View<span className="govuk-visually-hidden"> {user.firstName} {user.lastName}</span>
          </Link>{' '}
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleEdit(user.id);
            }}>
            Edit<span className="govuk-visually-hidden"> {user.firstName} {user.lastName}</span>
          </Link>
        </div>
      ),
    },
  ];

  return (
    <LoadingSpinner loading={usersLoading} label="Loading">
      <GovUKTable<OrganisationUserDto> data={usersData?.data ?? []} columns={columns} keyExtractor={(user) => user.id} searchable={false} emptyMessage="No users found" />
    </LoadingSpinner>
  );
};

export default ListOrganisationUsersComponent;
