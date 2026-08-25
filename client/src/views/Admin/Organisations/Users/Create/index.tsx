import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation } from './queries';
import { useQuery } from 'react-query';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import CreateOrganisationUsersComponent from '../../../../../components/Global/Organisations/Users/Create';
import { GeneralLayout } from '../../../../../layouts';
import { ROUTES } from '../../../../../constants/routes';

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
    navigate(ROUTES.admin.organisations.users.view(organisationId, userId));
  };

  return (
    <LoadingSpinner loading={loading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
          { label: 'Local Authority Users', link: ROUTES.admin.organisations.users.list(organisationId) },
          { label: 'Create', link: '' },
        ]}
        currentPage="Create">
        <CreateOrganisationUsersComponent organisationId={organisationId!} handleFinish={handleFinish} onCancel={() => navigate(ROUTES.admin.organisations.users.list(organisationId))} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default CreateOrganisationUsers;
