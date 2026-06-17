import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { DataCollectionWizard, DataCollectionWizardData } from '../../../../components/Global/DataCollections';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import axios from 'axios';
import { encodeForWaf, encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { getDataCollectionWithLocalAuthorities, DataCollectionResponse, getDataCollectionFormModules } from '../List/queries';
import { getOrganisations } from '../../Organisations/List/queries';
import usePageTitle from '../../../../hooks/usePageTitle';

interface UpdateDataCollectionRequest {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  isSubmittedByAllLocalAuthorities: boolean;
  localAuthorityIds: string[];
  formModuleIds: string[];
  saveAsDraft: boolean;
}

const updateDataCollection = (data: UpdateDataCollectionRequest) => axios.put<DataCollectionResponse>(`/admin/data-collections/${data.id}/edit`, data);

const formatDateForGovUK = (dateString: string): string => {
  const date = new Date(dateString);
  const day = date.getDate().toString();
  const month = (date.getMonth() + 1).toString();
  const year = date.getFullYear().toString();
  return `${day}/${month}/${year}`;
};

const parseDateString = (dateStr: string): Date | null => {
  if (!dateStr) return null;
  const parts = dateStr.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    return new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
  }
  return null;
};

const EditDataCollection = (): React.ReactElement => {
  usePageTitle('Edit data collection');
  const { dataCollectionId } = useParams<{ dataCollectionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const { data: dcData, isLoading: dcLoading } = useQuery({
    queryKey: ['data-collection-edit', dataCollectionId],
    queryFn: () => getDataCollectionWithLocalAuthorities(dataCollectionId!),
    enabled: !!dataCollectionId,
  });

  const { data: organisationsData, isLoading: loadingOrganisations } = useQuery({
    queryKey: ['organisations-for-wizard'],
    queryFn: getOrganisations,
  });

  const { data: formModulesData, isLoading: loadingFormModules } = useQuery({
    queryKey: ['form-modules-for-wizard'],
    queryFn: getDataCollectionFormModules,
  });

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['data-collection-update'],
    mutationFn: updateDataCollection,
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Data collection updated successfully' });
      queryClient.invalidateQueries(['data-collection', dataCollectionId]);
      queryClient.invalidateQueries(['data-collection-edit', dataCollectionId]);
      queryClient.invalidateQueries(['data-collections-list']);
      navigate(`/admin/data-collections/${dataCollectionId}`);
    },
    onError: (error) => {
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
      id: dataCollectionId!,
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

  const initialData = dcData?.data
    ? {
        name: dcData.data.name,
        description: dcData.data.description || '',
        startDate: formatDateForGovUK(dcData.data.startDate),
        endDate: formatDateForGovUK(dcData.data.endDate),
        formModuleIds: dcData.data.formModules?.map((fm) => fm.id) ?? [],
        isSubmittedByAllLocalAuthorities: dcData.data.isSubmittedByAllLocalAuthorities,
        localAuthorityIds: dcData.data.localAuthorities?.map((la) => la.id) ?? [],
      }
    : undefined;

  const availableLocalAuthorities = organisationsData?.data?.filter((org) => org.isActive).map((org) => ({ id: org.id, name: org.name })) ?? [];

  const availableFormModules =
    formModulesData?.data?.map((fm) => ({
      id: fm.id,
      sectionNumber: fm.sectionNumber,
      name: fm.name,
      lastChangedOn: fm.lastChangedOn,
    })) ?? [];

  const isLoading = dcLoading || loadingOrganisations || loadingFormModules;
  const dataReady = !isLoading && !!dcData?.data;

  return (
    <SettingsLayout backLink={{ href: `/admin/data-collections/${dataCollectionId}` }}>
      {dataReady && (
        <DataCollectionWizard
          initialData={initialData}
          isLoading={isLoading}
          isSaving={saving}
          availableLocalAuthorities={availableLocalAuthorities}
          availableFormModules={availableFormModules}
          onSubmit={(data) => handleSubmit(data, false)}
          onSaveAsDraft={(data) => handleSubmit(data, true)}
          submitLabel="Confirm and save"
          isEdit={true}
        />
      )}
    </SettingsLayout>
  );
};

export default EditDataCollection;
