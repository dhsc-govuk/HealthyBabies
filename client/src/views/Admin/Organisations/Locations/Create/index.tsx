import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import LocationCreateComponent from '../../../../../components/Global/Organisations/Locations/Create';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';
import { ROUTES } from '../../../../../constants/routes';

type UrlParams = {
  organisationId: string;
};

const LocationCreate = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleFinish = (locationId: string) => {
    navigate(ROUTES.admin.organisations.locations.view(organisationId, locationId));
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
          { label: 'Delivery location', link: ROUTES.admin.organisations.locations.list(organisationId) },
          { label: 'Create', link: '' },
        ]}
        currentPage="Create">
        <LocationCreateComponent organisationId={organisationId!} handleFinish={handleFinish} onCancel={() => navigate(ROUTES.admin.organisations.locations.list(organisationId))} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationCreate;
