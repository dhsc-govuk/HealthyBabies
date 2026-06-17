import React from 'react';
import { useNavigate } from 'react-router-dom';
import CreateOrganisationUsersComponent from '../../../../components/Global/Organisations/Users/Create';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { SettingsLayout } from '../../../../layouts';

const CreateOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();

  const { user } = useAuthProvider();

  const organisationId = user?.organisation_id;

  const handleFinish = (_organisationId: string, userId: string) => {
    navigate(`/organisation-admin/la-users/${userId}`);
  };

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'LA users', link: '/organisation-admin/la-users' },
      ]}
      currentPage="Add LA user">
      <CreateOrganisationUsersComponent organisationId={organisationId!} handleFinish={handleFinish} onCancel={() => navigate('/organisation-admin/la-users')} />
    </SettingsLayout>
  );
};

export default CreateOrganisationUsers;
