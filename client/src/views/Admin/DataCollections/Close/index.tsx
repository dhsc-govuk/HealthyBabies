import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { SettingsLayout } from '../../../../layouts';
import { getDataCollection, closeDataCollection } from '../List/queries';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const CloseDataCollection = (): React.ReactElement => {
  usePageTitle('Close data collection');
  const { dataCollectionId } = useParams<{ dataCollectionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['data-collection', dataCollectionId],
    queryFn: () => getDataCollection(dataCollectionId!),
    enabled: !!dataCollectionId,
  });

  const { mutateAsync: closeMutation, isLoading: closing } = useMutation({
    mutationFn: () => closeDataCollection(dataCollectionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['data-collections-list']);
      queryClient.invalidateQueries(['data-collection-full', dataCollectionId]);
      navigate(`/admin/data-collections/${dataCollectionId}`, {
        state: { successMessage: 'The data collection has been closed.' },
      });
    },
    onError: () => {
      setNotification({ type: 'important', title: 'Error', message: 'Failed to close data collection' });
    },
  });

  const dataCollection = data?.data;

  const handleClose = async () => {
    await closeMutation();
  };

  const handleGoBack = () => {
    navigate(`/admin/data-collections/${dataCollectionId}`);
  };

  if (isError) {
    return (
      <SettingsLayout hideNavigation>
        <h1 className="govuk-heading-l">Error</h1>
        <p className="govuk-body">Unable to load data collection.</p>
        <GovUKButton onClick={() => navigate('/admin/data-collections')}>Back to list</GovUKButton>
      </SettingsLayout>
    );
  }

  return (
    <SettingsLayout hideNavigation backLink={{ href: `/admin/data-collections/${dataCollectionId}`, onClick: handleGoBack }}>
      <LoadingBox loading={isLoading || closing}>
        {dataCollection && (
          <WarningPanel>
            <WarningPanelTitle>Are you sure you want to close data collection?</WarningPanelTitle>
            <WarningPanelBody>
              Closing the data collection '{dataCollection.name}' will immediately close submissions, regardless of the due date set before. Local authorities will still be able to
              view the data collection details, but they will no longer be able to submit new data.
            </WarningPanelBody>
            <WarningPanelBody>This will not remove any data already submitted by local authorities for this data collection.</WarningPanelBody>
            <WarningPanelBody>You can later reopen this data collection.</WarningPanelBody>
            <WarningPanelBody>If you want to keep this data collection open, you can go back.</WarningPanelBody>
            <WarningPanelActions>
              <Button onClick={handleClose} disabled={closing} buttonColour="#ffffff" buttonTextColour="#1d70b8">
                Yes, I want to close
              </Button>
              <WarningPanelAnchor
                href={`/admin/data-collections/${dataCollectionId}`}
                onClick={(e) => {
                  e.preventDefault();
                  handleGoBack();
                }}>
                Go back
              </WarningPanelAnchor>
            </WarningPanelActions>
          </WarningPanel>
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default CloseDataCollection;
