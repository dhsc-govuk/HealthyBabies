import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import LocationCreateComponent from '../../../../../components/Global/Organisations/Locations/Create';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';

type UrlParams = {
  organisationId: string;
};

const LocationCreate = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleFinish = (locationId: string) => {
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations/${locationId}`);
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Delivery location', link: `/admin/organisations/${organisationId}/service-delivery-locations` },
          { label: 'Create', link: '' },
        ]}
        currentPage="Create">
        <LocationCreateComponent organisationId={organisationId!} handleFinish={handleFinish} onCancel={() => navigate(`/admin/organisations/${organisationId}/service-delivery-locations`)} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationCreate;
