import React, { useReducer } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { LoadingBox } from 'govuk-react';
import { useGovUKNotification } from '../../../../../components/GovUKComponents';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { createGlobalData } from './mutations';
import { lookupDataReducer } from '../Common';
import LookupDataSteps from '../Common/components/Steps';
import { encodeForWaf, encodeNullableForWaf } from '../../../../../helpers/stringUtils';

const CreateLookupData = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [lookupData, dispatchLookupData] = useReducer(lookupDataReducer, {
    entity: '',
    value: '',
    description: '',
  });

  const { mutateAsync, isLoading } = useMutation({
    mutationKey: ['global-data-create'],
    mutationFn: createGlobalData,
    onSuccess(_data, variables, _context) {
      setNotification({ type: 'success', title: 'Success', message: 'Lookup data created successfully' });
      queryClient.invalidateQueries(['global-data-list']);
      queryClient.invalidateQueries(['global-data', variables.entity]);
      navigate('/admin/configuration/lookup-data');
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSave = async () => {
    await mutateAsync({
      entity: encodeForWaf(lookupData.entity),
      value: encodeForWaf(lookupData.value),
      description: encodeNullableForWaf(lookupData.description),
    });
  };

  return (
    <SettingsLayout
      currentPage="Create lookup data"
      backLink={{ href: '/admin/configuration/lookup-data', onClick: () => navigate('/admin/configuration/lookup-data') }}>
      <LoadingBox loading={isLoading}>
        <LookupDataSteps
          completeLabel="Create"
          lookupData={lookupData}
          dispatch={dispatchLookupData}
          handleSave={handleSave}
          onCancel={() => navigate('/admin/configuration/lookup-data')}
        />
      </LoadingBox>
    </SettingsLayout>
  );
};

export default CreateLookupData;
