import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { updateOrganisation } from './mutations';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { viewOrganisationCacheKey, viewOrganisationHomeCacheKey } from '../../../../helpers/queriesParams';
import { useOrganisationQuery } from '../../../../components/Global/Queries/organisations';
import { LoadingBox } from 'govuk-react';
import { LocalAuthorityForm, LocalAuthorityFormData } from '../../../../components/Global/Organisations';
import { useGovUKNotification } from '../../../../components/GovUKComponents';

type UrlParams = {
  organisationId: string;
};

const OrganisationEdit = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { organisationId } = useParams<UrlParams>();

  const { data: orgData, isLoading: orgLoading } = useOrganisationQuery({ organisationId: organisationId! });

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['admin-organisations-edit'],
    mutationFn: updateOrganisation,
    onSuccess(data) {
      queryClient.removeQueries({ queryKey: viewOrganisationCacheKey(organisationId!) });
      queryClient.removeQueries({ queryKey: viewOrganisationHomeCacheKey(organisationId!) });
      navigate(`/admin/organisations/${data.data.id}`);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: LocalAuthorityFormData) => {
    await mutateAsync({
      id: organisationId!,
      name: formData.name,
      onsCode: formData.onsCode,
      isActive: formData.isActive,
    });
  };

  const initialData = orgData?.data ? {
    name: orgData.data.name,
    onsCode: orgData.data.onsCode,
    isActive: orgData.data.isActive,
  } : undefined;

  const dataReady = !orgLoading && !!orgData?.data;

  return (
    <SettingsLayout currentPage="Edit local authority">
      <LoadingBox loading={orgLoading}>
        {dataReady && (
          <LocalAuthorityForm
            initialData={initialData}
            isSaving={saving}
            onSubmit={handleSubmit}
            onCancel={() => navigate(`/admin/organisations/${organisationId}`)}
            submitLabel="Save changes"
            isEdit={true}
          />
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default OrganisationEdit;
