import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LoadingBox, Button } from 'govuk-react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { stringFromArray } from '../../../../helpers/stringUtils';
import axios from 'axios';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

type UrlParams = {
  userId: string;
};

interface AdminUserResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

const getAdminUser = (userId: string) => axios.get<AdminUserResponse>(`/admin/users/${userId}`);
const deleteAdminUser = (userId: string) => axios.delete(`/admin/users/${userId}`);

const DeleteAdminUser = (): React.ReactElement => {
  usePageTitle('Delete departmental user');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { userId } = useParams<UrlParams>();

  const { data, isLoading } = useQuery({
    queryKey: ['departmental-user-view', userId],
    queryFn: () => getAdminUser(userId!),
  });

  const { mutateAsync: performDelete, isLoading: isDeleting } = useMutation({
    mutationKey: ['departmental-user-delete'],
    mutationFn: () => deleteAdminUser(userId!),
    onSuccess() {
      setNotification({ type: 'success', title: 'Success', message: 'Departmental user deleted successfully' });
      queryClient.invalidateQueries(['departmental-users-list']);
      navigate('/admin/departmental-users');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleDelete = async () => {
    await performDelete();
  };

  const handleCancel = () => {
    navigate(`/admin/departmental-users/${userId}`);
  };

  const userName = data ? stringFromArray([data.data.firstName, data.data.lastName]) : '';

  return (
    <SettingsLayout hideNavigation backLink={{ href: `/admin/departmental-users/${userId}`, onClick: handleCancel }}>
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
                href={`/admin/departmental-users/${userId}`}
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

export default DeleteAdminUser;
