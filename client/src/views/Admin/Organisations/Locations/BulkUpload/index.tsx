import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import BulkUploadComponent from '../../../../../components/Global/Organisations/Locations/BulkUpload';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';

type UrlParams = {
  organisationId: string;
};

const LocationBulkUpload = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleFinish = () => {
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations`);
  };

  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
          { label: 'Delivery location', link: `/admin/organisations/${organisationId}/service-delivery-locations` },
        ]}
        currentPage="Bulk Upload">
        <BulkUploadComponent organisationId={organisationId!} handleFinish={handleFinish} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default LocationBulkUpload;
