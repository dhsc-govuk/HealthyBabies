import React from 'react';
import { useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, SummaryList } from '../../../../../components/GovUKComponents';
import { getContact } from './queries';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import { getOrganisation } from '../../Users/List/queries';

type UrlParams = {
  organisationId: string;
  contactId: string;
};

const ViewOrganisationContact = (): React.ReactElement => {
  const { organisationId, contactId } = useParams<UrlParams>();

  const { data: orgData } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['admin-organisation-contact', contactId],
    queryFn: () => getContact(organisationId!, contactId!),
    staleTime: defaultStaleTime,
    enabled: !!organisationId && !!contactId,
  });

  return (
    <LoadingSpinner loading={isLoading} label="Loading contact">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Contacts', link: `/admin/organisations/${organisationId}/contacts` },
        ]}
        currentPage={data?.data.name ?? 'Contact'}>
        {data && (
          <SummaryList
            items={[
              { label: 'Name', value: data.data.name },
              { label: 'Email', value: data.data.email },
              { label: 'Role', value: data.data.role },
            ]}
          />
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewOrganisationContact;
