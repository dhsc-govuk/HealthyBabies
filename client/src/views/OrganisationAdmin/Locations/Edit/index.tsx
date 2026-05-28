import React, { useReducer, useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { ErrorSummary, LoadingSpinner, Button, useGovUKNotification } from '../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../layouts';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSiteFormQuestions, siteFormQuestionsCacheKey } from '../../../../components/Global/SiteForms';
import { processError } from '../../../../helpers/axiosErrorFallback';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import axios from 'axios';
import { Stack } from '@mui/material';
import { ValidationErrors, initialLocationState, locationReducer, validateLocation, buildAnswersForRequest, LocationReducerAction } from '../Common';
import LocationForm from '../Create/LocationForm';

type UrlParams = {
  locationId: string;
};

const LocationsEdit = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { locationId } = useParams<UrlParams>();
  const { organisationId } = useAuthProvider();

  const [state, dispatch] = useReducer(locationReducer, initialLocationState);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [initialized, setInitialized] = useState(false);

  const { trackStarted, trackSubmitAttempted, trackSubmitted, trackValidationFailed } = useFormTelemetry('location_edit');

  const { data: questionsResponse, isLoading: isLoadingQuestions } = useQuery(siteFormQuestionsCacheKey(), () => getSiteFormQuestions(), {
    staleTime: 5 * 60 * 1000,
  });

  const { data: locationData, isLoading: isLoadingLocation } = useQuery(['location', locationId], () => axios.get(`/organisations/${organisationId}/locations/${locationId}`), {
    enabled: !!locationId && !!organisationId,
  });

  const questions = questionsResponse?.data ?? [];

  useEffect(() => {
    if (locationData?.data && !initialized) {
      const loc = locationData.data;
      const answers: Record<string, string | undefined> = {};

      if (loc.answers) {
        for (const answer of loc.answers) {
          answers[answer.questionCode] = answer.value ?? undefined;
        }
      }

      dispatch({
        type: LocationReducerAction.INIT_FROM_LOCATION,
        payload: {
          id: loc.id,
          answers,
        },
      });
      setInitialized(true);
      trackStarted();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [locationData, initialized]);

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; isActive: boolean; answers: any[] }) => axios.put(`/organisations/${organisationId}/locations/edit`, data),
    onSuccess: () => {
      trackSubmitted();
      setNotification({ type: 'success', title: 'Success', message: 'Site updated successfully' });
      queryClient.invalidateQueries(['locations']);
      queryClient.invalidateQueries(['location', locationId]);
      navigate(`/organisation-admin/core-data/delivery-locations/${locationId}`);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to update site' }));
    },
  });

  const validate = (): boolean => {
    const validationErrors = validateLocation(state, questions);
    setErrors(validationErrors);
    setSubmitAttempts((n) => n + 1);
    if (Object.keys(validationErrors).length > 0) {
      Object.entries(validationErrors).forEach(([field, message]) => { if (message) trackValidationFailed(field, message); });
      return false;
    }
    return true;
  };

  const handleSave = () => {
    if (!validate()) return;

    trackSubmitAttempted();
    updateMutation.mutate({
      id: locationId!,
      isActive: true,
      answers: buildAnswersForRequest(state, questions),
    });
  };

  const handleCancel = () => {
    navigate(`/organisation-admin/core-data/delivery-locations/${locationId}`);
  };

  if (isLoadingQuestions || isLoadingLocation) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading" />
      </GeneralLayout>
    );
  }

  const errorList = Object.entries(errors)
    .map(([key, value]) => ({
      targetName: key,
      text: value || '',
    }))
    .filter((e) => e.text);

  return (
    <LoadingSpinner loading={isLoadingLocation} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Delivery locations', link: `/organisation-admin/core-data/delivery-locations` },
          { label: locationData?.data?.name ?? '', link: `/organisation-admin/core-data/delivery-locations/${locationId}` },
        ]}
        currentPage="Edit site">
        {errorList.length > 0 && <ErrorSummary errors={errorList} />}
        <LocationForm state={state} dispatch={dispatch} errors={errors} questions={questions} />
        <Stack direction="row" spacing={2} sx={{ mt: 3 }}>
          <Button onClick={handleSave}>Save changes</Button>
          <Button variant="secondary" onClick={handleCancel}>
            Cancel
          </Button>
        </Stack>
      </GeneralLayout>
      {updateMutation.isLoading && <LoadingSpinner label="Saving site" />}
    </LoadingSpinner>
  );
};

export default LocationsEdit;
