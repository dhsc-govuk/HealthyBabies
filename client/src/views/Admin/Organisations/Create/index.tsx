import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createOrganisation } from './mutations';
import { useMutation } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { LocalAuthorityForm, LocalAuthorityFormData } from '../../../../components/Global/Organisations';
import { useGovUKNotification } from '../../../../components/GovUKComponents';
import usePageTitle from '../../../../hooks/usePageTitle';
import { encodeForWaf } from '../../../../helpers/stringUtils';

const OrganisationCreate = (): React.ReactElement => {
  usePageTitle('Add local authority');
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const [, setCurrentStep] = useState<'form' | 'review'>('form');

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['admin-organisations-create'],
    mutationFn: createOrganisation,
    onSuccess() {
      navigate('/admin/organisations', { state: { created: true } });
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (formData: LocalAuthorityFormData) => {
    await mutateAsync({
      name: encodeForWaf(formData.name),
      onsCode: formData.onsCode,
      isActive: formData.isActive,
      contacts: formData.contacts.map((c) => ({
        ...c,
        fullName: encodeForWaf(c.fullName),
        role: encodeForWaf(c.role),
        roleTitle: c.roleTitle ? encodeForWaf(c.roleTitle) : undefined,
      })),
    });
  };

  return (
    <SettingsLayout>
      <LocalAuthorityForm
        isSaving={saving}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/admin/organisations')}
        submitLabel="Confirm and create"
        onStepChange={setCurrentStep}
      />
    </SettingsLayout>
  );
};

export default OrganisationCreate;
