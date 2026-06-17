import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { LoadingSpinner } from '../../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../../layouts';
import ListLocationsComponent from '../../../../../components/Global/Organisations/Locations/List';
import { useOrganisationQuery } from '../../../../../components/Global/Queries/organisations';

type UrlParams = {
  organisationId: string;
};

const ListLocations = (): React.ReactElement => {
  const { organisationId } = useParams<UrlParams>();

  const navigate = useNavigate();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const handleView = (locationId: string) => {
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations/${locationId}`);
  };

  const handleEdit = (locationId: string) => {
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations/${locationId}/edit`);
  };

  const handleCreate = () => {
    navigate(`/admin/organisations/${organisationId}/service-delivery-locations/create`);
  };


  return (
    <LoadingSpinner loading={orgLoading} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Local Authorities', link: '/admin/organisations' },
          { label: orgData?.data.name ?? '', link: `/admin/organisations/${organisationId}` },
        ]}
        currentPage="Sites">
        <ListLocationsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} handleCreate={handleCreate} />
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ListLocations;
