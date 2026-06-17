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

interface CreateDataCollectionRequest {
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  isSubmittedByAllLocalAuthorities: boolean;
  localAuthorityIds: string[];
  formModuleIds: string[];
  saveAsDraft: boolean;
}

interface CreateDataCollectionResponse {
  id: string;
}

const createDataCollection = (data: CreateDataCollectionRequest) => axios.post<CreateDataCollectionResponse>('/admin/data-collections/create', data);

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
      navigate('/admin/data-collections', {
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
    })) ?? [];

  return (
    <SettingsLayout backLink={{ href: '/admin/data-collections' }}>
      <DataCollectionWizard
        isLoading={loadingOrganisations || loadingFormModules}
        isSaving={saving}
        availableLocalAuthorities={availableLocalAuthorities}
        availableFormModules={availableFormModules}
        onSubmit={(data) => handleSubmit(data, false)}
        onSaveAsDraft={(data) => handleSubmit(data, true)}
        submitLabel="Confirm and save"
      />
    </SettingsLayout>
  );
};

export default CreateDataCollection;
