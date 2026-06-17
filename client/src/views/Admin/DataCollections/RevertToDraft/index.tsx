import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { SettingsLayout } from '../../../../layouts';
import { getDataCollection, revertDataCollectionToDraft } from '../List/queries';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const RevertToDraft = (): React.ReactElement => {
  usePageTitle('Revert to draft');
  const { dataCollectionId } = useParams<{ dataCollectionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['data-collection', dataCollectionId],
    queryFn: () => getDataCollection(dataCollectionId!),
    enabled: !!dataCollectionId,
  });

  const { mutateAsync: revertMutation, isLoading: reverting } = useMutation({
    mutationFn: () => revertDataCollectionToDraft(dataCollectionId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['data-collections-list']);
      queryClient.invalidateQueries(['data-collection-full', dataCollectionId]);
      navigate(`/admin/data-collections/${dataCollectionId}`, {
        state: { successMessage: 'The data collection has been reverted to draft.' },
      });
    },
    onError: () => {
      setNotification({ type: 'important', title: 'Error', message: 'Failed to revert data collection to draft' });
    },
  });

  const dataCollection = data?.data;

  const handleRevert = async () => {
    await revertMutation();
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
      <LoadingBox loading={isLoading || reverting}>
        {dataCollection && (
          <WarningPanel>
            <WarningPanelTitle>Are you sure you want to revert data collection to draft?</WarningPanelTitle>
            <WarningPanelBody>
              Reverting the data collection '{dataCollection.name}' to draft will immediately close submissions and remove access for local authorities. Local authorities will not
              be able to view the data collection details or submit new data anymore.
            </WarningPanelBody>
            <WarningPanelBody>This will not remove any data already submitted by local authorities for this data collection.</WarningPanelBody>
            <WarningPanelBody>You can later republish this data collection.</WarningPanelBody>
            <WarningPanelBody>If you want to keep this data collection open, you can go back.</WarningPanelBody>
            <WarningPanelActions>
              <Button onClick={handleRevert} disabled={reverting} buttonColour="#ffffff" buttonTextColour="#1d70b8">
                Yes, I want to revert
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

export default RevertToDraft;
