import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, LoadingSpinner } from '../../../../../components/GovUKComponents';
import { getOrganisation } from './queries';
import { useQuery } from 'react-query';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../../../helpers/queriesParams';
import ListOrganisationUsersComponent from '../../../../../components/Global/Organisations/Users/List';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageTitle } from '../../../../../styles/govuk-global';
import usePageTitle from '../../../../../hooks/usePageTitle';

type UrlParams = {
  organisationId: string;
};

const ListOrganisationUsers = (): React.ReactElement => {
  usePageTitle('Local authority users');
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId!)],
    queryFn: () => getOrganisation(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleView = (userId: string) => {
    navigate(`/admin/organisations/${organisationId}/users/${userId}`);
  };

  const handleEdit = (userId: string) => {
    navigate(`/admin/organisations/${organisationId}/users/${userId}/edit`);
  };

  const handleCreate = () => {
    navigate(`/admin/organisations/${organisationId}/users/create`);
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
        ]}>
        <PageHeaderContainer>
          <PageTitle>Local Authority Users</PageTitle>
          <Button onClick={handleCreate}>Create</Button>
        </PageHeaderContainer>
        <ListOrganisationUsersComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} />
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default ListOrganisationUsers;
