import React, { useReducer, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { GovUKStepper, ErrorSummary, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSiteFormQuestions, siteFormQuestionsCacheKey } from '../../../../components/Global/SiteForms';
import { processError } from '../../../../helpers/axiosErrorFallback';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import axios from 'axios';
import { ValidationErrors, initialLocationState, locationReducer, validateLocation, buildAnswersForRequest } from '../Common';
import LocationForm from './LocationForm';
import SummaryStep from './SummaryStep';

const LocationsCreate = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { organisationId } = useAuthProvider();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState(0);
  const [state, dispatch] = useReducer(locationReducer, initialLocationState);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));

  const { trackStarted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackValidationFailed } = useFormTelemetry('location_create');

  useEffect(() => {
    trackStarted();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const { data: questionsResponse, isLoading: isLoadingQuestions } = useQuery(siteFormQuestionsCacheKey(), () => getSiteFormQuestions(), {
    staleTime: 5 * 60 * 1000,
  });

  const questions = questionsResponse?.data ?? [];

  const createMutation = useMutation({
    mutationFn: (data: { answers: any[] }) => axios.post(`/organisations/${organisationId}/locations/create`, data),
    onSuccess: () => {
      trackSubmitted();
      setNotification({
        type: 'success',
        title: 'Site saved.',
        message: 'The site has been added to your list of sites. You can view, change, or delete the site record at any time.',
      });
      queryClient.invalidateQueries(['locations']);
      navigate('/organisation-admin/core-data/delivery-locations');
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to create site' }));
    },
  });

  const validate = (currentStep: number): boolean => {
    if (currentStep === 0) {
      const validationErrors = validateLocation(state, questions);
      setErrors(validationErrors);
      setSubmitAttempts((n) => n + 1);
      if (Object.keys(validationErrors).length > 0) {
        Object.entries(validationErrors).forEach(([field, message]) => { if (message) trackValidationFailed(field, message); });
        return true;
      }
      return false;
    }
    return false;
  };

  const handleStepChange = (newStep: number | ((prev: number) => number)) => {
    const targetStep = typeof newStep === 'function' ? newStep(step) : newStep;
    if (targetStep === 1) trackReviewReached();
    setStep(targetStep);
  };

  const handleComplete = () => {
    trackSubmitAttempted();
    createMutation.mutate({
      answers: buildAnswersForRequest(state, questions),
    });
  };

  if (isLoadingQuestions) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading form" />
      </GeneralLayout>
    );
  }

  const errorList = Object.entries(errors)
    .map(([key, value]) => ({
      targetName: key,
      text: value || '',
    }))
    .filter((e) => e.text);

  const stepperSteps = [
    {
      label: 'Delivery location details',
      component: <LocationForm state={state} dispatch={dispatch} errors={errors} questions={questions} />,
    },
    {
      label: 'Check your answers',
      component: <SummaryStep state={state} questions={questions} />,
    },
  ];

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Delivery locations', link: '/organisation-admin/core-data/delivery-locations' },
      ]}
      currentPage="Add delivery location">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKStepper
        step={step}
        setStep={handleStepChange}
        stepperSteps={stepperSteps}
        completeLabel="Confirm and save"
        handleComplete={handleComplete}
        validate={validate}
        isNextDisabled={createMutation.isLoading}
        title="Add delivery location"
        description="Tell us about the site so it can be added to your account. This information will be saved and can be updated at any time."
        helpSummary="Help with sites"
        helpContent={<p>Sites are the physical locations where services are delivered. Each site should be recorded separately to help families find the right support.</p>}
      />
      {createMutation.isLoading && <LoadingSpinner label="Creating site" />}
    </GeneralLayout>
  );
};

export default LocationsCreate;
