import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { GeneralLayout } from '../../../../layouts';
import { Button } from '../../../../components/GovUKComponents';
import ListOrganisationContactsComponent from '../../../../components/Global/Organisations/Contacts/List';

const ListContacts = (): React.ReactElement => {
  const navigate = useNavigate();
  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const handleView = (contactId: string) => {
    navigate(`/organisation-admin/contacts/${contactId}`);
  };

  const handleEdit = (contactId: string) => {
    navigate(`/organisation-admin/contacts/${contactId}/edit`);
  };

  const handleCreate = () => {
    navigate('/organisation-admin/contacts/create');
  };

  return (
    <GeneralLayout breadcrumbs={[{ label: 'Home', link: '/organisation-admin/home' }]} currentPage="Contacts" endContent={<Button onClick={handleCreate}>Create</Button>}>
      <ListOrganisationContactsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} />
    </GeneralLayout>
  );
};

export default ListContacts;
