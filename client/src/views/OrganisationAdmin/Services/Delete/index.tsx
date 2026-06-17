import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { getService, deleteService, serviceCacheKey, servicesCacheKey } from '../../../../components/Global/Services';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const DeleteService = (): React.ReactElement => {
  usePageTitle('Delete service');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { serviceId } = useParams<{ serviceId: string }>();

  const { data, isLoading } = useQuery({
    queryKey: serviceCacheKey(serviceId!),
    queryFn: () => getService(serviceId!),
    enabled: !!serviceId,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteService(serviceId!),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Service deleted successfully' });
      queryClient.invalidateQueries(servicesCacheKey());
      navigate('/organisation-admin/core-data/services');
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to delete service' }));
    },
  });

  const handleDelete = () => {
    deleteMutation.mutate();
  };

  const serviceName = data?.data?.name ?? 'this service';

  return (
    <LoadingBox loading={isLoading || deleteMutation.isLoading}>
      <GeneralLayout backLink={{ href: `/organisation-admin/core-data/services/${serviceId}` }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete the service?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting &apos;{serviceName}&apos; will remove all saved information about this service from your Best Start Family Hub and Healthy Baby account.
          </WarningPanelBody>
          <WarningPanelBody>You will also no longer be able to use its information for future Delivery Plan and Management Information submissions.</WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>If you want to keep this service&apos;s information, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleDelete} disabled={deleteMutation.isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelAnchor href={`/organisation-admin/core-data/services/${serviceId}`}>Go back</WarningPanelAnchor>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    </LoadingBox>
  );
};

export default DeleteService;
