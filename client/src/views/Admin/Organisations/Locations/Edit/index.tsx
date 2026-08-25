import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import LocationsEditComponent from '../../../../../components/Global/Organisations/Locations/Edit';
import { useLocationQuery } from '../../../../../components/Global/Queries/locations';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';
import { ROUTES } from '../../../../../constants/routes';

type UrlParams = {
  organisationId: string;
  locationId: string;
};

const LocationsEdit = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId, locationId } = useParams<UrlParams>();

  const { data: locationData, isLoading: locLoading } = useLocationQuery({ organisationId: organisationId!, locationId: locationId! });
  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleFinish = (locationId: string) => {
    navigate(ROUTES.admin.organisations.locations.view(organisationId, locationId));
  };

  return (
    <LoadingSpinner loading={locLoading || orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
          { label: 'Service delivery locations', link: ROUTES.admin.organisations.locations.list(organisationId) },
          { label: locationData?.data.name ?? '', link: ROUTES.admin.organisations.locations.view(organisationId, locationId) },
          { label: 'Edit', link: '' },
        ]}
        currentPage="Edit">
        <LocationsEditComponent organisationId={organisationId!} locationId={locationId!} handleFinish={handleFinish} onCancel={() => navigate(ROUTES.admin.organisations.locations.view(organisationId, locationId))} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationsEdit;
