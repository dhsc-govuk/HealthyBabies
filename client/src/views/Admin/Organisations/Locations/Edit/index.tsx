import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import LocationsEditComponent from '../../../../../components/Global/Organisations/Locations/Edit';
import { useLocationQuery } from '../../../../../components/Global/Queries/locations';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';

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
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations/${locationId}`);
  };

  return (
    <LoadingSpinner loading={locLoading || orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Service delivery locations', link: `/admin/organisations/${organisationId}/service-delivery-locations` },
          { label: locationData?.data.name ?? '', link: `/admin/organisations/${organisationId}/service-delivery-locations/${locationId}` },
          { label: 'Edit', link: '' },
        ]}
        currentPage="Edit">
        <LocationsEditComponent organisationId={organisationId!} locationId={locationId!} handleFinish={handleFinish} onCancel={() => navigate(`/admin/organisations/${organisationId}/service-delivery-locations/${locationId}`)} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationsEdit;
