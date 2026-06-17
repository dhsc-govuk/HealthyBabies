import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { stringFromArray } from '../../../../helpers/stringUtils';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { getOrganisationUser } from './queries';
import { useQuery } from 'react-query';
import { viewOrganisationUsersCacheKey } from '../../../../helpers/queriesParams';
import ViewComponent from '../../../../components/Global/Users/View';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { SettingsLayout } from '../../../../layouts';

type UrlParams = {
  userId: string;
};

const ViewOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();
  const { userId } = useParams<UrlParams>();

  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const { data: userData, isLoading: userLoading } = useQuery({
    queryKey: [viewOrganisationUsersCacheKey(userId!)],
    queryFn: () => getOrganisationUser(organisationId!, userId!),
  });

  const handleEdit = () => {
    navigate(`/organisation-admin/la-users/${userId}/edit`);
  };

  return (
    <LoadingSpinner loading={userLoading} label="Loading">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'LA users', link: '/organisation-admin/la-users' },
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
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default ViewOrganisationUsers;
