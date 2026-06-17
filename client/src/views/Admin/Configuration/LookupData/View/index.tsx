import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { Button, LoadingBox } from 'govuk-react';
import { GovUKButton, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { getGlobalDataById } from './queries';
import { deleteGlobalData } from './mutations';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../../styles/govuk-global';
import { Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions } from '@mui/material';
import './styles.css';
import usePageTitle from '../../../../../hooks/usePageTitle';

type UrlParams = {
  lookupId: string;
};

const ViewLookupData = (): React.ReactElement => {
  usePageTitle('View lookup data');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { lookupId } = useParams<UrlParams>();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const { setNotification } = useGovUKNotification();

  const { data, isLoading } = useQuery({
    queryKey: ['global-data-view', lookupId],
    queryFn: () => getGlobalDataById(lookupId!),
    enabled: !!lookupId,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteGlobalData(lookupId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['global-data-list'] });
      setNotification({ type: 'success', title: 'Success', message: 'Lookup data deleted successfully' });
      navigate('/admin/configuration/lookup-data');
    },
    onError: () => {
      setNotification({ type: 'important', title: 'Error', message: 'Failed to delete lookup data' });
    },
  });

  const handleEdit = () => {
    navigate(`/admin/configuration/lookup-data/${lookupId}/edit`);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const confirmDelete = () => {
    deleteMutation.mutate();
    setDeleteDialogOpen(false);
  };

  const lookupData = data?.data;

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Lookup Data', link: '/admin/configuration/lookup-data' },
      ]}>
      <LoadingBox loading={isLoading}>
        {lookupData && (
          <>
            <PageHeaderContainer>
              <PageTitle>{lookupData.value}</PageTitle>
              <PageHeaderActions>
                <Button onClick={handleEdit}>Change</Button>
                <Button buttonColour="#b10e1e" onClick={handleDelete}>Delete</Button>
              </PageHeaderActions>
            </PageHeaderContainer>

            <dl className="govuk-summary-list">
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Entity</dt>
                <dd className="govuk-summary-list__value">{lookupData.entity}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Value</dt>
                <dd className="govuk-summary-list__value">{lookupData.value}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Description</dt>
                <dd className="govuk-summary-list__value">{lookupData.description || '-'}</dd>
              </div>
            </dl>
          </>
        )}
      </LoadingBox>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Lookup Data</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete "{lookupData?.value}"? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button buttonColour="#f3f2f1" buttonTextColour="#0b0c0c" onClick={() => setDeleteDialogOpen(false)}>
            Cancel
          </Button>
          <GovUKButton
            style={{ backgroundColor: '#b10e1e', boxShadow: '0 2px 0 #6a0812' }}
            onClick={confirmDelete}
            isLoading={deleteMutation.isLoading}
          >
            Delete
          </GovUKButton>
        </DialogActions>
      </Dialog>
    </SettingsLayout>
  );
};

export default ViewLookupData;
