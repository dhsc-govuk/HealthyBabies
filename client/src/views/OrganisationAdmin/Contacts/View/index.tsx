import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, SummaryList, Button } from '../../../../components/GovUKComponents';
import { getContact } from '../Edit/queries';
import { defaultStaleTime } from '../../../../helpers/queriesParams';

type UrlParams = {
  contactId: string;
};

const ViewContact = (): React.ReactElement => {
  const navigate = useNavigate();
  const { contactId } = useParams<UrlParams>();
  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const { data, isLoading } = useQuery({
    queryKey: ['organisation-contact', contactId],
    queryFn: () => getContact(organisationId!, contactId!),
    staleTime: defaultStaleTime,
    enabled: !!organisationId && !!contactId,
  });

  const handleEdit = () => {
    navigate(`/organisation-admin/contacts/${contactId}/edit`);
  };

  return (
    <LoadingSpinner loading={isLoading} label="Loading contact">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Contacts', link: '/organisation-admin/contacts' },
        ]}
        currentPage={data?.data.name ?? 'Contact'}>
        {data && (
          <>
            <SummaryList
              items={[
                { label: 'Name', value: data.data.name },
                { label: 'Email', value: data.data.email },
                { label: 'Role', value: data.data.role },
              ]}
            />
            <div className="govuk-button-group">
              <Button onClick={handleEdit}>Edit</Button>
            </div>
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewContact;
