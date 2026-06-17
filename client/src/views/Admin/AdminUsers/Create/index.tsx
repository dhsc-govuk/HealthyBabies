import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { OrganisationUserForm, OrganisationUserFormData } from '../../../../components/Global/OrganisationUsers';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import axios from 'axios';

interface CreateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

interface CreateAdminUserResponse {
  id: string;
}

const createAdminUser = (data: CreateAdminUserRequest) =>
  axios.post<CreateAdminUserResponse>('/admin/users/create', data);

const CreateAdminUser = (): React.ReactElement => {
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['departmental-user-create'],
    mutationFn: createAdminUser,
    onSuccess(data) {
      setNotification({ type: 'success', title: 'Success', message: 'Departmental user created successfully' });
      navigate(`/admin/departmental-users/${data.data.id}`);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: OrganisationUserFormData) => {
    await mutateAsync({
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      isActive: formData.autoActivate,
    });
  };

  return (
    <SettingsLayout
      currentPage="Add departmental user"
      backLink={{ href: '/admin/departmental-users', onClick: () => navigate('/admin/departmental-users') }}>
      <OrganisationUserForm
        organisations={[]}
        isSaving={saving}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/admin/departmental-users')}
        submitLabel="Confirm and create"
        showOrganisation={false}
        showRole={false}
        hideBackLink={true}
      />
    </SettingsLayout>
  );
};

export default CreateAdminUser;
