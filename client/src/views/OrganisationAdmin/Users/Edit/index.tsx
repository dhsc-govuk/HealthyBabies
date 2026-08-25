import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { getOrganisationUser } from './queries';
import { useQuery } from 'react-query';
import { organisationUserStaleTime, viewOrganisationUsersCacheKey } from '../../../../helpers/queriesParams';
import EditOrganisationUsersComponent from '../../../../components/Global/Organisations/Users/Edit';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { SettingsLayout } from '../../../../layouts';
import { ROUTES } from '../../../../constants/routes';

type UrlParams = {
  userId: string;
};

const EditOrganisationUsers = (): React.ReactElement => {
  const navigate = useNavigate();
  const { userId } = useParams<UrlParams>();

  const { user } = useAuthProvider();
  const organisationId = user?.organisation_id;

  const { isLoading: userLoading } = useQuery({
    queryKey: [viewOrganisationUsersCacheKey(userId!)],
    queryFn: () => getOrganisationUser(organisationId!, userId!),
    staleTime: organisationUserStaleTime,
  });

  const handleFinish = (_organisationId: string, userId: string) => {
    navigate(ROUTES.organisationAdmin.laUsers.view(userId));
  };

  return (
    <LoadingSpinner loading={userLoading} label="Loading">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.organisationAdmin.home },
          { label: 'LA users', link: ROUTES.organisationAdmin.laUsers.list },
        ]}
        currentPage="Edit LA user">
        <EditOrganisationUsersComponent
          organisationId={organisationId!}
          userId={userId!}
          handleFinish={handleFinish}
          onCancel={() => navigate(ROUTES.organisationAdmin.laUsers.view(userId))}
        />
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default EditOrganisationUsers;
