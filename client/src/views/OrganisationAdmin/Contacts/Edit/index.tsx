import React, { useReducer, useEffect, useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { contactReducer, ContactReducerAction } from '../Common';
import { getContactRoles } from '../Common/queries';
import ContactSteps from '../Common/components/ContactSteps';
import { updateContact } from './mutations';
import { getContact } from './queries';
import { defaultStaleTime } from '../../../../helpers/queriesParams';

type UrlParams = {
  contactId: string;
};

const EditContact = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { contactId } = useParams<UrlParams>();
  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const [contact, dispatchContact] = useReducer(contactReducer, {
    name: '',
    email: '',
    role: '',
  });

  const { data: contactData, isLoading: contactLoading } = useQuery({
    queryKey: ['organisation-contact', contactId],
    queryFn: () => getContact(organisationId!, contactId!),
    staleTime: defaultStaleTime,
    enabled: !!organisationId && !!contactId,
  });

  const { data: rolesData } = useQuery({
    queryKey: ['contact-roles'],
    queryFn: getContactRoles,
    staleTime: defaultStaleTime,
  });

  useEffect(() => {
    if (contactData) {
      dispatchContact({ type: ContactReducerAction.INIT, value: contactData.data });
    }
  }, [contactData]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['organisation-contacts-edit'],
    mutationFn: updateContact,
    onSuccess(_data, _variables, _context) {
      setNotification({ type: 'success', title: 'Success', message: 'Contact updated successfully' });
      queryClient.invalidateQueries(['organisation-contacts', organisationId]);
      queryClient.invalidateQueries(['organisation-contact', contactId]);
      navigate('/organisation-admin/contacts');
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const roles = useMemo(() => rolesData?.data ?? [], [rolesData?.data]);

  const handleSave = async () => {
    await mutateAsync({
      organisationId: organisationId!,
      contactId: contactId!,
      name: contact.name,
      email: contact.email,
      role: contact.role,
    });
  };

  return (
    <LoadingSpinner loading={contactLoading || saving} label={contactLoading ? 'Loading contact' : 'Saving changes'}>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Contacts', link: '/organisation-admin/contacts' },
          { label: 'Edit', link: '' },
        ]}
        currentPage="Edit">
        <ContactSteps
          completeLabel="Save"
          contact={contact}
          dispatch={dispatchContact}
          handleSave={handleSave}
          roles={roles}
          onCancel={() => navigate('/organisation-admin/contacts')}
        />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default EditContact;
