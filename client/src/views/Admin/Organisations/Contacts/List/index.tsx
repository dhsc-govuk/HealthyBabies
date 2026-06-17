import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import ListOrganisationContactsComponent from '../../../../../components/Global/Organisations/Contacts/List';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import { getOrganisation } from '../../Users/List/queries';

type UrlParams = {
  organisationId: string;
};

const ListOrganisationContacts = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleView = (contactId: string) => {
    navigate(`/admin/organisations/${organisationId}/contacts/${contactId}`);
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
        ]}
        currentPage="Contacts">
        <ListOrganisationContactsComponent organisationId={organisationId!} handleView={handleView} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ListOrganisationContacts;
