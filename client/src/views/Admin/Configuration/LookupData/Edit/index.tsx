import React, { useEffect, useReducer } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { LoadingBox } from 'govuk-react';
import { useGovUKNotification } from '../../../../../components/GovUKComponents';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { getGlobalDataById } from './queries';
import { updateGlobalData } from './mutations';
import { lookupDataReducer, LookupDataReducerAction } from '../Common';
import LookupDataSteps from '../Common/components/Steps';

type UrlParams = {
  lookupId: string;
};

const EditLookupData = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { lookupId } = useParams<UrlParams>();

  const [lookupData, dispatchLookupData] = useReducer(lookupDataReducer, {
    entity: '',
    value: '',
    description: '',
  });

  const { data, isLoading: loading } = useQuery({
    queryKey: ['global-data-edit', lookupId],
    queryFn: () => getGlobalDataById(lookupId!),
    enabled: !!lookupId,
  });

  useEffect(() => {
    if (data?.data) {
      dispatchLookupData({
        type: LookupDataReducerAction.INIT,
        value: {
          entity: data.data.entity,
          value: data.data.value,
          description: data.data.description || '',
        },
      });
    }
  }, [data]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['global-data-update'],
    mutationFn: updateGlobalData,
    onSuccess(_data, variables, _context) {
      setNotification({ type: 'success', title: 'Success', message: 'Lookup data updated successfully' });
      queryClient.invalidateQueries(['global-data-list']);
      queryClient.invalidateQueries(['global-data', variables.entity]);
      queryClient.invalidateQueries(['global-data-edit', lookupId]);
      navigate('/admin/configuration/lookup-data');
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSave = async () => {
    await mutateAsync({
      id: lookupId!,
      entity: lookupData.entity,
      value: lookupData.value,
      description: lookupData.description || null,
    });
  };

  const dataReady = !loading && !!data?.data;

  return (
    <SettingsLayout
      currentPage="Edit lookup data"
      backLink={{ href: '/admin/configuration/lookup-data', onClick: () => navigate('/admin/configuration/lookup-data') }}>
      <LoadingBox loading={loading || saving}>
        {dataReady && (
          <LookupDataSteps
            completeLabel="Save"
            lookupData={lookupData}
            dispatch={dispatchLookupData}
            handleSave={handleSave}
            isEdit
            onCancel={() => navigate('/admin/configuration/lookup-data')}
          />
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default EditLookupData;
