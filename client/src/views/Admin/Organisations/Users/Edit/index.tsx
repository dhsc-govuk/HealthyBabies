import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { stringFromArray } from '../../../../../helpers/stringUtils';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation, getOrganisationUser } from './queries';
import { useQuery } from 'react-query';
import { defaultStaleTime, organisationUserStaleTime, viewOrganisationCacheKey, viewOrganisationUsersCacheKey } from '../../../../../helpers/queriesParams';
import EditOrganisationUsersComponent from '../../../../../components/Global/Organisations/Users/Edit';
import { GeneralLayout } from '../../../../../layouts';

type UrlParams = {
  organisationId: string;
  userId: string;
};

const EditOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId, userId } = useParams<UrlParams>();

  const { data: userData, isLoading: userLoading } = useQuery({
    queryKey: [viewOrganisationUsersCacheKey(userId!)],
    queryFn: () => getOrganisationUser(organisationId!, userId!),
    staleTime: organisationUserStaleTime,
  });

  const { data: orgData, isLoading: orgLoading } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleFinish = (organisationId: string, userId: string) => {
    navigate(`/admin/organisations/${organisationId}/users/${userId}`);
  };

  return (
    <LoadingSpinner loading={orgLoading || userLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Local Authority Users', link: `/admin/organisations/${organisationId}/users` },
          {
            label: stringFromArray([userData?.data.firstName ?? '', userData?.data.lastName ?? '']),
            link: `/admin/organisations/${organisationId}/users/${userId}`,
          },
          { label: 'Edit', link: '' },
        ]}
        currentPage="Edit">
        <EditOrganisationUsersComponent organisationId={organisationId!} userId={userId!} handleFinish={handleFinish} onCancel={() => navigate(`/admin/organisations/${organisationId}/users/${userId}`)} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default EditOrganisationUsers;
