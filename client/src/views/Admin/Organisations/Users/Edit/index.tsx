import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { stringFromArray } from '../../../../../helpers/stringUtils';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation, getOrganisationUser } from './queries';
import { useQuery } from 'react-query';
import { defaultStaleTime, organisationUserStaleTime, viewOrganisationCacheKey, viewOrganisationUsersCacheKey } from '../../../../../helpers/queriesParams';
import EditOrganisationUsersComponent from '../../../../../components/Global/Organisations/Users/Edit';
import { GeneralLayout } from '../../../../../layouts';
import { ROUTES } from '../../../../../constants/routes';

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
    navigate(ROUTES.admin.organisations.users.view(organisationId, userId));
  };

  return (
    <LoadingSpinner loading={orgLoading || userLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
          { label: 'Local Authority Users', link: ROUTES.admin.organisations.users.list(organisationId) },
          {
            label: stringFromArray([userData?.data.firstName ?? '', userData?.data.lastName ?? '']),
            link: ROUTES.admin.organisations.users.view(organisationId, userId),
          },
          { label: 'Edit', link: '' },
        ]}
        currentPage="Edit">
        <EditOrganisationUsersComponent organisationId={organisationId!} userId={userId!} handleFinish={handleFinish} onCancel={() => navigate(ROUTES.admin.organisations.users.view(organisationId, userId))} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default EditOrganisationUsers;
