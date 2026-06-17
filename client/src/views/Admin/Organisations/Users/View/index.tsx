import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { stringFromArray } from '../../../../../helpers/stringUtils';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation, getOrganisationUser } from './queries';
import ViewComponent from '../../../../../components/Global/Users/View';
import { useQuery } from 'react-query';
import { defaultStaleTime, viewOrganisationCacheKey, viewOrganisationUsersCacheKey } from '../../../../../helpers/queriesParams';
import { GeneralLayout } from '../../../../../layouts';

type UrlParams = {
  organisationId: string;
  userId: string;
};

const ViewOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId, userId } = useParams<UrlParams>();

  const { data: userData, isLoading: userLoading } = useQuery({
    queryKey: [viewOrganisationUsersCacheKey(userId!)],
    queryFn: () => getOrganisationUser(organisationId!, userId!),
  });

  const { data: orgData, isLoading: orgLoading } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleEdit = () => {
    navigate(`/admin/organisations/${organisationId}/users/${userId}/edit`);
  };

  return (
    <LoadingSpinner loading={userLoading || orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Local Authority Users', link: `/admin/organisations/${organisationId}/users` },
        ]}
        currentPage={stringFromArray([userData?.data.firstName ?? '', userData?.data.lastName ?? ''])}>
        {userData && (
          <ViewComponent
            user={{
              firstName: userData.data.firstName,
              lastName: userData.data.lastName,
              email: userData.data.email,
              isActive: userData.data.isActive,
              role: userData.data.role,
            }}
            handleEdit={handleEdit}
          />
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewOrganisationUsers;
