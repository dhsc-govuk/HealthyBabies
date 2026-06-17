import React from 'react';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { useParams } from 'react-router-dom';
import { GeneralLayout } from '../../../../../layouts';
import ViewLocationComponent from '../../../../../components/Global/Organisations/Locations/View';
import { useLocationQuery } from '../../../../../components/Global/Queries/locations';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';

type UrlParams = {
  organisationId: string;
  locationId: string;
};

const ViewLocation = (): React.ReactElement => {
  const { organisationId, locationId } = useParams<UrlParams>();

  const { data: locationData, isLoading: locLoading } = useLocationQuery({ organisationId: organisationId!, locationId: locationId! });
  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  return (
    <LoadingSpinner loading={locLoading || orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Delivery location', link: `/admin/organisations/${organisationId}/service-delivery-locations` },
        ]}
        currentPage={locationData?.data.name ?? ''}>
        <ViewLocationComponent organisationId={organisationId!} locationId={locationId!} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewLocation;
