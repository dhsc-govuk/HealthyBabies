import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import BulkUploadComponent from '../../../../../components/Global/Organisations/Locations/BulkUpload';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';
import { ROUTES } from '../../../../../constants/routes';

type UrlParams = {
  organisationId: string;
};

const LocationBulkUpload = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleFinish = () => {
    navigate(ROUTES.admin.organisations.locations.list(organisationId));
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.admin.home },
          { label: 'Local Authorities', link: ROUTES.admin.organisations.list },
          { label: orgData?.data.name ?? '', link: ROUTES.admin.organisations.view(organisationId) },
          { label: 'Delivery location', link: ROUTES.admin.organisations.locations.list(organisationId) },
        ]}>
        <BulkUploadComponent organisationId={organisationId!} handleFinish={handleFinish} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationBulkUpload;
