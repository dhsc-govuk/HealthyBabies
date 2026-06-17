import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { OrganisationUserForm, OrganisationUserFormData } from '../../../../components/Global/OrganisationUsers';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { LoadingBox } from 'govuk-react';
import axios from 'axios';

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

interface UpdateAdminUserRequest {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

const getAdminUser = (userId: string) => 
  axios.get<AdminUserResponse>(`/admin/users/${userId}`);

const updateAdminUser = (data: UpdateAdminUserRequest) => 
  axios.put<AdminUserResponse>(`/admin/users/${data.id}`, data);

const EditAdminUser = (): React.ReactElement => {
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const { userId } = useParams<UrlParams>();

  const { data, isLoading } = useQuery({
    queryKey: ['departmental-user-edit', userId],
    queryFn: () => getAdminUser(userId!),
  });

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['departmental-user-edit'],
    mutationFn: updateAdminUser,
    onSuccess(data) {
      navigate(`/admin/departmental-users/${data.data.id}`);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: OrganisationUserFormData) => {
    await mutateAsync({
      id: userId!,
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      isActive: formData.autoActivate,
    });
  };

  const initialData = data?.data ? {
    firstName: data.data.firstName,
    lastName: data.data.lastName,
    email: data.data.email,
    autoActivate: data.data.isActive,
  } : undefined;

  const dataReady = !isLoading && !!data?.data;

  return (
    <SettingsLayout
      currentPage="Edit departmental user"
      backLink={{ href: `/admin/departmental-users/${userId}`, onClick: () => navigate(`/admin/departmental-users/${userId}`) }}>
      <LoadingBox loading={isLoading}>
        {dataReady && (
          <OrganisationUserForm
            initialData={initialData}
            organisations={[]}
            isLoading={false}
            isSaving={saving}
            onSubmit={handleSubmit}
            onCancel={() => navigate(`/admin/departmental-users/${userId}`)}
            submitLabel="Save changes"
            isEdit={true}
            emailReadOnly={true}
            showOrganisation={false}
            showRole={false}
            hideBackLink={true}
          />
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default EditAdminUser;
