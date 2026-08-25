import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { DataCollectionWizard, DataCollectionWizardData } from '../../../../components/Global/DataCollections';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import { getOrganisations } from '../../Organisations/List/queries';
import { getDataCollectionFormModules } from '../List/queries';
import axios from 'axios';
import { encodeForWaf, encodeNullableForWaf } from '../../../../helpers/stringUtils';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';
import { API_ROUTES } from '../../../../constants/apiRoutes';

interface CreateDataCollectionRequest {
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  type: string;
  isSubmittedByAllLocalAuthorities: boolean;
  localAuthorityIds: string[];
  formModuleIds: string[];
  saveAsDraft: boolean;
}

interface CreateDataCollectionResponse {
  id: string;
}

const createDataCollection = (data: CreateDataCollectionRequest) => axios.post<CreateDataCollectionResponse>(API_ROUTES.admin.dataCollections.create, data);

const parseDateString = (dateStr: string): Date | null => {
  if (!dateStr) return null;
  const parts = dateStr.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    return new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
  }
  return null;
};

const CreateDataCollection = (): React.ReactElement => {
  usePageTitle('Create data collection');
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();

  const { data: organisationsData, isLoading: loadingOrganisations } = useQuery({
    queryKey: ['organisations-for-wizard'],
    queryFn: getOrganisations,
  });

  const { data: formModulesData, isLoading: loadingFormModules } = useQuery({
    queryKey: ['form-modules-for-wizard'],
    queryFn: getDataCollectionFormModules,
  });

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['data-collection-create'],
    mutationFn: createDataCollection,
    onSuccess() {
      navigate(ROUTES.admin.dataCollections.list, {
        state: {
          successMessage: 'The data collection has been added to your list of data collections. You can view, change, or delete the data collection at any time.',
        },
      });
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: DataCollectionWizardData, saveAsDraft: boolean) => {
    const start = parseDateString(formData.startDate);
    const end = parseDateString(formData.endDate);

    if (!start || !end) {
      return;
    }

    await mutateAsync({
      name: encodeForWaf(formData.name.trim()),
      description: encodeNullableForWaf(formData.description.trim() || undefined),
      startDate: start.toISOString(),
      endDate: end.toISOString(),
      type: formData.type,
      isSubmittedByAllLocalAuthorities: formData.isSubmittedByAllLocalAuthorities,
      localAuthorityIds: formData.localAuthorityIds,
      formModuleIds: formData.formModuleIds,
      saveAsDraft,
    });
  };

  const availableLocalAuthorities = organisationsData?.data?.filter((org) => org.isActive).map((org) => ({ id: org.id, name: org.name })) ?? [];

  const availableFormModules =
    formModulesData?.data?.map((fm) => ({
      id: fm.id,
      sectionNumber: fm.sectionNumber,
      name: fm.name,
      lastChangedOn: fm.lastChangedOn,
      type: fm.type,
    })) ?? [];

  return (
    <SettingsLayout>
      <DataCollectionWizard
        isLoading={loadingOrganisations || loadingFormModules}
        isSaving={saving}
        availableLocalAuthorities={availableLocalAuthorities}
        availableFormModules={availableFormModules}
        onSubmit={(data) => handleSubmit(data, false)}
        onSaveAsDraft={(data) => handleSubmit(data, true)}
        onCancel={() => navigate(ROUTES.admin.dataCollections.list)}
        submitLabel="Confirm and save"
      />
    </SettingsLayout>
  );
};

export default CreateDataCollection;
