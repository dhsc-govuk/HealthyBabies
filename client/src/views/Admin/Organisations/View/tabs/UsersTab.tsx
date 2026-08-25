import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Link, LoadingBox } from 'govuk-react';
import { GovUKTable } from '../../../../../components/GovUKComponents';
import type { Column } from '../../../../../components/GovUKComponents';
import { ROUTES } from '../../../../../constants/routes';

export interface UserRow {
  id: string;
  name: string;
  role: string;
  isActive: boolean;
}

interface UsersTabProps {
  users: UserRow[];
  isLoading: boolean;
  organisationId: string;
}

const UsersTab = ({ users, isLoading, organisationId }: UsersTabProps): React.ReactElement => {
  const navigate = useNavigate();

  const columns: Column<UserRow>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (user) => (
        <Link
          href={ROUTES.admin.laUsers.view(user.id)}
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            navigate(ROUTES.admin.laUsers.view(user.id));
          }}>
          {user.name}
        </Link>
      ),
    },
    { key: 'role', header: 'Role' },
    {
      key: 'status',
      header: 'Status',
      align: 'right',
      render: (user) => (user.isActive ? <span>Active</span> : <span className="govuk-tag govuk-tag--yellow">Inactive</span>),
    },
  ];

  return (
    <>
      <div className="govuk-button-group govuk-!-text-align-right">
        <button
          type="button"
          className="govuk-button govuk-button--secondary"
          onClick={() => navigate(ROUTES.admin.laUsers.createForOrganisation(organisationId))}>
          Add LA user
        </button>
      </div>
      <LoadingBox loading={isLoading}>
        <GovUKTable<UserRow>
          caption="Local authority users"
          data={users}
          columns={columns}
          searchPlaceholder="Search LA user by name"
          sortLabel="Sort users"
          sortOptions={[
            { value: 'status', label: 'By status' },
            { value: 'name', label: 'By name' },
          ]}
          keyExtractor={(user) => user.id}
          getRowHref={(user) => ROUTES.admin.laUsers.view(user.id)}
        />
      </LoadingBox>
    </>
  );
};

export default UsersTab;
