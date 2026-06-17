import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation } from './queries';
import { useQuery } from 'react-query';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import CreateOrganisationUsersComponent from '../../../../../components/Global/Organisations/Users/Create';
import { GeneralLayout } from '../../../../../layouts';

type UrlParams = {
  organisationId: string;
};

const CreateOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: loading } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleFinish = (organisationId: string, userId: string) => {
    navigate(`/admin/organisations/${organisationId}/users/${userId}`);
  };

  return (
    <LoadingSpinner loading={loading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Local Authority Users', link: `/admin/organisations/${organisationId}/users` },
          { label: 'Create', link: '' },
        ]}
        currentPage="Create">
        <CreateOrganisationUsersComponent organisationId={organisationId!} handleFinish={handleFinish} onCancel={() => navigate(`/admin/organisations/${organisationId}/users`)} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default CreateOrganisationUsers;
