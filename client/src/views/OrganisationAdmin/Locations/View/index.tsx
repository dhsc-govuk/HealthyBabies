import React from 'react';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { useParams } from 'react-router-dom';
import { GeneralLayout } from '../../../../layouts';
import ViewLocationComponent from '../../../../components/Global/Organisations/Locations/View';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { useLocationQuery } from '../../../../components/Global/Queries/locations';
import { ROUTES } from '../../../../constants/routes';

type UrlParams = {
  locationId: string;
};

const ViewLocation = (): React.ReactElement => {
  const { locationId } = useParams<UrlParams>();
  const { organisationId } = useAuthProvider();

  const { data: locationData, isLoading: locLoading } = useLocationQuery({ organisationId: organisationId!, locationId: locationId! });

  return (
    <LoadingSpinner loading={locLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.organisationAdmin.home },
          { label: 'Delivery locations', link: ROUTES.organisationAdmin.locations.list },
        ]}
        currentPage={locationData?.data.name ?? ''}>
        <ViewLocationComponent organisationId={organisationId!} locationId={locationId!} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewLocation;
