import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { Button } from '../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../layouts';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { useLocationQuery } from '../../../../components/Global/Queries/locations';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import { viewServiceDeliverySiteCacheKey } from '../../../../helpers/queriesParams';
import axios from 'axios';
import usePageTitle from '../../../../hooks/usePageTitle';

type UrlParams = {
  locationId: string;
};

const DeleteLocation = (): React.ReactElement => {
  usePageTitle('Delete delivery location');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { locationId } = useParams<UrlParams>();
  const { organisationId } = useAuthProvider();

  const { data, isLoading } = useLocationQuery({
    organisationId: organisationId!,
    locationId: locationId!,
  });

  const deleteMutation = useMutation({
    mutationFn: () =>
      axios.delete(`/organisations/${organisationId}/locations/${locationId}`),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Delivery location deleted successfully' });
      queryClient.invalidateQueries([viewServiceDeliverySiteCacheKey(organisationId!, locationId!)]);
      queryClient.invalidateQueries(['locations']);
      navigate('/organisation-admin/core-data/delivery-locations');
    },
    onError: (error) => {
      processError(error, (e) =>
        setNotification({ type: 'important', title: 'Error', message: e || 'Failed to delete delivery location' })
      );
    },
  });

  const locationName = data?.data?.name ?? 'this delivery location';

  return (
    <LoadingBox loading={isLoading || deleteMutation.isLoading}>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Delivery locations', link: '/organisation-admin/core-data/delivery-locations' },
          { label: locationName, link: `/organisation-admin/core-data/delivery-locations/${locationId}` },
        ]}
        currentPage="Delete delivery location">
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete this delivery location?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting &apos;{locationName}&apos; will remove all saved information about this location from your Best Start Family Hub and Healthy Baby account.
          </WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>If you want to keep this location&apos;s information, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isLoading}
              buttonColour="#ffffff"
              buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelAnchor href={`/organisation-admin/core-data/delivery-locations/${locationId}`}>
              Go back
            </WarningPanelAnchor>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    </LoadingBox>
  );
};

export default DeleteLocation;
