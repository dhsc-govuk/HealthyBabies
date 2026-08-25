import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { GeneralLayout } from '../../../../layouts';
import { Button } from '../../../../components/GovUKComponents';
import ListOrganisationContactsComponent from '../../../../components/Global/Organisations/Contacts/List';
import { ROUTES } from '../../../../constants/routes';

const ListContacts = (): React.ReactElement => {
  const navigate = useNavigate();
  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const handleView = (contactId: string) => {
    navigate(ROUTES.organisationAdmin.contacts.view(contactId));
  };

  const handleEdit = (contactId: string) => {
    navigate(ROUTES.organisationAdmin.contacts.edit(contactId));
  };

  const handleCreate = () => {
    navigate(ROUTES.organisationAdmin.contacts.create);
  };

  return (
    <GeneralLayout breadcrumbs={[{ label: 'Home', link: ROUTES.organisationAdmin.home }]} currentPage="Contacts" endContent={<Button onClick={handleCreate}>Create</Button>}>
      <ListOrganisationContactsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} />
    </GeneralLayout>
  );
};

export default ListContacts;
