import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LoadingBox, Button } from 'govuk-react';
import { getAdmin } from '../View/queries';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { stringFromArray } from '../../../../helpers/stringUtils';
import { deleteAdmin } from './mutations';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

type UrlParams = {
  userId: string;
};

const DeleteAdmin = (): React.ReactElement => {
  usePageTitle('Delete LA user');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { userId } = useParams<UrlParams>();

  const { data, isLoading } = useQuery({
    queryKey: ['la-users-view', userId],
    queryFn: () => getAdmin(userId!),
  });

  const { mutateAsync: performDelete, isLoading: isDeleting } = useMutation({
    mutationKey: ['admin-delete-user'],
    mutationFn: () => deleteAdmin(userId!),
    onSuccess() {
      setNotification({ type: 'success', title: 'Success', message: 'LA user deleted successfully' });
      queryClient.invalidateQueries(['la-users-list']);
      navigate('/admin/la-users');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleDelete = async () => {
    await performDelete();
  };

  const handleCancel = () => {
    navigate(`/admin/la-users/${userId}`);
  };

  const userName = data ? stringFromArray([data.data.firstName, data.data.lastName]) : '';

  return (
    <SettingsLayout hideNavigation backLink={{ href: `/admin/la-users/${userId}`, onClick: handleCancel }}>
      <LoadingBox loading={isLoading || isDeleting}>
        {data && (
          <WarningPanel>
            <WarningPanelTitle>Are you sure you want to delete user account?</WarningPanelTitle>
            <WarningPanelBody>
              Deleting the user account for '{userName}' will immediately and permanently remove their access to the service and delete their personal details.
            </WarningPanelBody>
            <WarningPanelBody>This will not remove any saved or draft data the user has already created within the service.</WarningPanelBody>
            <WarningPanelBody>
              <strong>This cannot be undone.</strong>
            </WarningPanelBody>
            <WarningPanelBody>If you want to keep this user account, you can go back.</WarningPanelBody>
            <WarningPanelActions>
              <Button onClick={handleDelete} disabled={isDeleting} buttonColour="#ffffff" buttonTextColour="#1d70b8">
                Yes, I want to delete
              </Button>
              <WarningPanelAnchor
                href={`/admin/la-users/${userId}`}
                onClick={(e) => {
                  e.preventDefault();
                  handleCancel();
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

export default DeleteAdmin;
