import React from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { createOrganisationUser } from './mutations';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { getOrganisations } from './queries';
import { OrganisationUserForm, OrganisationUserFormData } from '../../../../components/Global/OrganisationUsers';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { encodeForWaf } from '../../../../helpers/stringUtils';
import { ROUTES } from '../../../../constants/routes';

const CreateUser = (): React.ReactElement => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { setNotification } = useGovUKNotification();
  const preselectedOrganisationId = searchParams.get('organisationId') || undefined;

  const { data: organisationsData, isLoading: loadingOrganisations } = useQuery({
    queryKey: ['organisations-list'],
    queryFn: getOrganisations,
  });

  const organisations = organisationsData?.data ?? [];

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['organisation-user-create'],
    mutationFn: createOrganisationUser,
    onSuccess(data) {
      navigate(ROUTES.admin.laUsers.list, { state: { created: true } });
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: OrganisationUserFormData) => {
    await mutateAsync({
      firstName: encodeForWaf(formData.firstName),
      lastName: encodeForWaf(formData.lastName),
      email: formData.email,
      organisationId: formData.organisationId,
      role: formData.role,
      isActive: formData.autoActivate,
    });
  };

  return (
    <SettingsLayout
      currentPage="Add an LA user"
      manageTitle={false}
      backLink={{ href: ROUTES.admin.laUsers.list, onClick: () => navigate(ROUTES.admin.laUsers.list) }}>
      <OrganisationUserForm
        initialData={preselectedOrganisationId ? { organisationId: preselectedOrganisationId } : undefined}
        organisations={organisations}
        isLoading={loadingOrganisations}
        isSaving={saving}
        onSubmit={handleSubmit}
        onCancel={() => navigate(ROUTES.admin.laUsers.list)}
        submitLabel="Confirm and create"
        hideBackLink={true}
        pageTitle="Add an LA user"
      />
    </SettingsLayout>
  );
};

export default CreateUser;
