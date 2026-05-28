import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAdmin } from './queries';
import { updateAdmin } from './mutations';
import { useMutation, useQuery } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { OrganisationUserForm, OrganisationUserFormData } from '../../../../components/Global/OrganisationUsers';
import { getOrganisations } from '../Create/queries';
import { LoadingBox } from 'govuk-react';
import { useGovUKNotification } from '../../../../components/GovUKComponents';

type UrlParams = {
  userId: string;
};

const EditAdmin = (): React.ReactElement => {
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const { userId } = useParams<UrlParams>();

  const { data, isLoading } = useQuery({
    queryKey: ['la-users-edit', userId],
    queryFn: () => getAdmin(userId!),
  });

  const { data: organisationsData, isLoading: loadingOrganisations } = useQuery({
    queryKey: ['organisations-list'],
    queryFn: getOrganisations,
  });

  const organisations = organisationsData?.data ?? [];

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['la-users-edit'],
    mutationFn: updateAdmin,
    onSuccess(data) {
      navigate(`/admin/la-users/${data.data.id}`);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: OrganisationUserFormData) => {
    await mutateAsync({
      id: userId,
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      organisationId: formData.organisationId,
      role: formData.role,
      isActive: formData.autoActivate,
    });
  };

  const initialData = data?.data ? {
    firstName: data.data.firstName,
    lastName: data.data.lastName,
    email: data.data.email,
    organisationId: data.data.organisationId ?? '',
    role: data.data.role ?? '',
    autoActivate: data.data.isActive,
  } : undefined;

  const dataReady = !isLoading && !loadingOrganisations && !!data?.data;

  return (
    <SettingsLayout
      currentPage="Edit LA user"
      backLink={{ href: `/admin/la-users/${userId}`, onClick: () => navigate(`/admin/la-users/${userId}`) }}>
      <LoadingBox loading={isLoading || loadingOrganisations}>
        {dataReady && (
          <OrganisationUserForm
            initialData={initialData}
            organisations={organisations}
            isLoading={false}
            isSaving={saving}
            onSubmit={handleSubmit}
            onCancel={() => navigate(`/admin/la-users/${userId}`)}
            submitLabel="Save changes"
            isEdit={true}
            emailReadOnly={true}
            hideBackLink={true}
          />
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default EditAdmin;
