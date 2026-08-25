import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import ListLocationsComponent from '../../../../../components/Global/Organisations/Locations/List';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';
import { ROUTES } from '../../../../../constants/routes';

type UrlParams = {
  organisationId: string;
};

const ListLocations = (): React.ReactElement => {
  const { organisationId } = useParams<UrlParams>();

  const navigate = useNavigate();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleView = (locationId: string) => {
    navigate(ROUTES.admin.organisations.locations.view(organisationId, locationId));
  };

  const handleEdit = (locationId: string) => {
    navigate(ROUTES.admin.organisations.locations.edit(organisationId, locationId));
  };

  const handleCreate = () => {
    navigate(ROUTES.admin.organisations.locations.create(organisationId));
  };


  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
        ]}
        currentPage="Sites">
        <ListLocationsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} handleCreate={handleCreate} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ListLocations;
