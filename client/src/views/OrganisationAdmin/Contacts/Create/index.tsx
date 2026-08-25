import React, { useReducer, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { contactReducer } from '../Common';
import ContactSteps from '../Common/components/ContactSteps';
import { createContact } from './mutations';
import { defaultStaleTime } from '../../../../helpers/queriesParams';
import { getContactRoles } from '../Common/queries';
import { encodeForWaf } from '../../../../helpers/stringUtils';
import { ROUTES } from '../../../../constants/routes';

const CreateContact = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const [contact, dispatchContact] = useReducer(contactReducer, {
    name: '',
    email: '',
    role: '',
  });

  const { data: rolesData } = useQuery({
    queryKey: ['contact-roles'],
    queryFn: getContactRoles,
    staleTime: defaultStaleTime,
  });

  const { mutateAsync, isLoading } = useMutation({
    mutationKey: ['organisation-contacts-create'],
    mutationFn: createContact,
    onSuccess(_data, _variables, _context) {
      setNotification({ type: 'success', title: 'Success', message: 'Contact created successfully' });
      queryClient.invalidateQueries(['organisation-contacts', organisationId]);
      navigate(ROUTES.organisationAdmin.contacts.list);
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const roles = useMemo(() => rolesData?.data ?? [], [rolesData?.data]);

  const handleSave = async () => {
    await mutateAsync({
      organisationId: organisationId!,
      name: encodeForWaf(contact.name),
      email: contact.email,
      role: encodeForWaf(contact.role),
    });
  };

  return (
    <LoadingSpinner loading={isLoading} label="Creating contact">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.organisationAdmin.home },
          { label: 'Contacts', link: ROUTES.organisationAdmin.contacts.list },
          { label: 'Create', link: '' },
        ]}
        currentPage="Create contact"
        manageTitle={false}>
        <ContactSteps
          completeLabel="Create"
          contact={contact}
          dispatch={dispatchContact}
          handleSave={handleSave}
          roles={roles}
          onCancel={() => navigate(ROUTES.organisationAdmin.contacts.list)}
          pageTitle="Create contact"
        />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default CreateContact;
