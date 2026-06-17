import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { SettingsLayout } from '../../../../layouts';
import { getDataCollection, deleteDataCollection } from '../List/queries';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const DeleteDataCollection = (): React.ReactElement => {
  usePageTitle('Delete data collection');
  const { dataCollectionId } = useParams<{ dataCollectionId: string }>();
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['data-collection', dataCollectionId],
    queryFn: () => getDataCollection(dataCollectionId!),
    enabled: !!dataCollectionId,
  });

  const { mutateAsync: deleteMutation, isLoading: deleting } = useMutation({
    mutationFn: () => deleteDataCollection(dataCollectionId!),
    onSuccess: () => {
      navigate('/admin/data-collections', {
        state: { successMessage: 'The data collection has been deleted.' },
      });
    },
    onError: () => {
      setNotification({ type: 'important', title: 'Error', message: 'Failed to delete data collection' });
    },
  });

  const dataCollection = data?.data;

  const handleDelete = async () => {
    await deleteMutation();
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
      <LoadingBox loading={isLoading || deleting}>
        {dataCollection && (
          <WarningPanel>
            <WarningPanelTitle>Are you sure you want to delete data collection?</WarningPanelTitle>
            <WarningPanelBody>
              Deleting the data collection '{dataCollection.name}' will permanently remove it and immediately remove access for local authorities and administrators.
            </WarningPanelBody>
            <WarningPanelBody>This will also remove any data already submitted by local authorities for this data collection.</WarningPanelBody>
            <WarningPanelBody>
              <strong>This cannot be undone.</strong>
            </WarningPanelBody>
            <WarningPanelBody>If you want to keep this data collection, you can go back.</WarningPanelBody>
            <WarningPanelActions>
              <Button onClick={handleDelete} disabled={deleting} buttonColour="#ffffff" buttonTextColour="#1d70b8">
                Yes, I want to delete
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

export default DeleteDataCollection;
