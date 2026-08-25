import React, { useReducer, useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GovUKErrorSummary, LoadingSpinner, Button, useGovUKNotification } from '../../../../components/GovUKComponents';
import { GeneralLayout } from '../../../../layouts';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSiteFormQuestions, siteFormQuestionsCacheKey, SiteFormQuestionType } from '../../../../components/Global/SiteForms';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { viewServiceDeliverySiteCacheKey } from '../../../../helpers/queriesParams';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import usePageTitle from '../../../../hooks/usePageTitle';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import axios from 'axios';
import { Stack } from '@mui/material';
import { ValidationErrors, initialLocationState, locationReducer, validateLocation, buildAnswersForRequest, LocationReducerAction } from '../Common';
import LocationForm from '../Create/LocationForm';
import { ROUTES } from '../../../../constants/routes';
import { API_ROUTES } from '../../../../constants/apiRoutes';

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

  const { data: locationData, isLoading: isLoadingLocation } = useQuery(['location', locationId], () => axios.get(API_ROUTES.organisations.locations.byId(organisationId, locationId)), {
    enabled: !!locationId && !!organisationId,
  });

  const questions = useMemo(() => questionsResponse?.data ?? [], [questionsResponse?.data]);

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
    mutationFn: (data: { id: string; isActive: boolean; answers: any[] }) => axios.put(API_ROUTES.organisations.locations.edit(organisationId), data),
    onSuccess: () => {
      trackSubmitted();
      setNotification({ type: 'success', title: 'Success', message: 'Site updated successfully' });
      queryClient.invalidateQueries(['locations']);
      queryClient.invalidateQueries(['location', locationId]);
      queryClient.invalidateQueries([viewServiceDeliverySiteCacheKey(organisationId!, locationId!)]);
      navigate(ROUTES.organisationAdmin.locations.view(locationId));
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
    navigate(ROUTES.organisationAdmin.locations.view(locationId));
  };

  const errorList = useMemo(() => {
    return Object.entries(errors)
      .filter(([, value]) => value)
      .map(([key, value]) => {
        const lcKey = key.toLowerCase();
        const question = questions.find((q) => q.code === key);
        let targetName = lcKey;
        if (question) {
          if (question.questionType === SiteFormQuestionType.Date) {
            targetName = `${lcKey}-day`;
          } else if (
            (question.questionType === SiteFormQuestionType.Radio || question.questionType === SiteFormQuestionType.Checkbox) &&
            question.options.length > 0
          ) {
            const firstOption = [...question.options].sort((a, b) => a.displayOrder - b.displayOrder)[0];
            targetName = `${lcKey}-${firstOption.value}`;
          }
        }
        return { targetName, text: value || '' };
      });
  }, [errors, questions]);

  // The layout's own title call cannot express an error state, so the view owns the title here
  // and the layout is told to stand down (manageTitle={false}). Gives the "Error: " prefix
  // whenever validation errors are present, and drops it once they clear (WCAG 2.4.2 / 3.3.1).
  usePageTitle('Edit site', errorList.length > 0);

  if (isLoadingQuestions || isLoadingLocation) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading" />
      </GeneralLayout>
    );
  }

  return (
    <LoadingSpinner loading={isLoadingLocation} label="Loading">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.organisationAdmin.home },
          { label: 'Delivery locations', link: ROUTES.organisationAdmin.locations.list },
          { label: locationData?.data?.name ?? '', link: ROUTES.organisationAdmin.locations.view(locationId) },
        ]}
        currentPage="Edit site"
        manageTitle={false}>
        {errorList.length > 0 && <GovUKErrorSummary errors={errorList} />}
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
