import React from 'react';
import { useNavigate } from 'react-router-dom';
import ListOrganisationUsersComponent from '../../../../components/Global/Organisations/Users/List';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { SettingsLayout } from '../../../../layouts';
import { Button } from '../../../../components/GovUKComponents';
import { PageHeaderContainer, PageHeaderActions } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const ListOrganisationUsers = (): React.ReactElement => {
  usePageTitle('LA users');
  const navigate = useNavigate();

  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const handleView = (userId: string) => {
    navigate(`/organisation-admin/la-users/${userId}`);
  };

  const handleEdit = (userId: string) => {
    navigate(`/organisation-admin/la-users/${userId}/edit`);
  };

  const handleCreate = () => {
    navigate(`/organisation-admin/la-users/create`);
  };

  return (
    <SettingsLayout breadcrumbs={[{ label: 'Home', link: '/organisation-admin/home' }]}>
      <PageHeaderContainer>
        <h1 className="govuk-heading-l">LA users</h1>
        <PageHeaderActions>
          <Button onClick={handleCreate}>Add LA user</Button>
        </PageHeaderActions>
      </PageHeaderContainer>
      <ListOrganisationUsersComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} />
    </SettingsLayout>
  );
};

export default ListOrganisationUsers;
