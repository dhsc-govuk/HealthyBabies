import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import ListOrganisationContactsComponent from '../../../../../components/Global/Organisations/Contacts/List';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import { getOrganisation } from '../../Users/List/queries';
import { ROUTES } from '../../../../../constants/routes';

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
    navigate(ROUTES.admin.organisations.contacts.view(organisationId, contactId));
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
        ]}
        currentPage="Contacts">
        <ListOrganisationContactsComponent organisationId={organisationId!} handleView={handleView} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ListOrganisationContacts;
