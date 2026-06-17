import React, { useReducer, useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { GovUKStepper, ErrorSummary, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import {
  createService,
  updateServiceStepOne,
  updateServiceStepTwo,
  completeService,
  servicesCacheKey,
  getServiceFormQuestions,
  serviceFormQuestionsCacheKey,
  UpdateServiceStepOneRequest,
  UpdateServiceStepTwoRequest,
} from '../../../../components/Global/Services';
import { processError } from '../../../../helpers/axiosErrorFallback';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import { ValidationErrors, initialServiceState, serviceReducer, validateStep, buildAnswersForRequest, shouldSkipServiceCharacteristics } from '../Common';
import StepOne from './StepOne';
import StepTwo from './StepTwo';
import StepThree from './StepThree';
import { encodeForWaf } from '../../../../helpers/stringUtils';

const CreateService = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState(0);
  const [state, dispatch] = useReducer(serviceReducer, initialServiceState);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [serviceId, setServiceId] = useState<string | null>(null);
  const serviceIdRef = useRef<string | null>(null);
  const isSavingAsDraft = useRef(false);

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('service_create');

  useEffect(() => {
    trackStarted();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Helper to get serviceId from either state or ref (ref is backup for state batching issues)
  const getServiceId = () => serviceId || serviceIdRef.current;

  // Fetch all service form questions
  const { data: questionsResponse, isLoading: isLoadingQuestions } = useQuery(serviceFormQuestionsCacheKey(), () => getServiceFormQuestions(), {
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const questions = questionsResponse?.data ?? [];
  const stepOneQuestions = questions.filter((q) => q.step === 1);
  const stepTwoQuestions = questions.filter((q) => q.step === 2);

  // Helper to build step one request
  const buildStepOneRequest = (advanceStep: boolean): UpdateServiceStepOneRequest => ({
    name: encodeForWaf(state.name),
    answers: buildAnswersForRequest(state, stepOneQuestions),
    advanceStep,
  });

  // Helper to build step two request
  const buildStepTwoRequest = (advanceStep: boolean): UpdateServiceStepTwoRequest => ({
    answers: buildAnswersForRequest(state, stepTwoQuestions),
    advanceStep,
  });

  const updateStepOneMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateServiceStepOneRequest }) => updateServiceStepOne(id, request),
    onSuccess: () => {
      if (isSavingAsDraft.current) {
        isSavingAsDraft.current = false;
        trackDraftSaved();
        queryClient.invalidateQueries(servicesCacheKey());
        setNotification({ type: 'success', title: 'Service saved', message: 'Service has been saved as draft' });
        navigate('/organisation-admin/core-data/services');
      } else {
        if (skipStepTwo) {
          trackReviewReached();
        } else {
          trackSectionCompleted('0');
        }
        setStep(1);
      }
    },
    onError: (error) => {
      isSavingAsDraft.current = false;
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to update service' }));
    },
  });

  const createMutation = useMutation({
    mutationFn: () => createService({ name: encodeForWaf(state.name) }),
    onSuccess: (response) => {
      const newServiceId = response.data.id;
      if (!newServiceId) {
        isSavingAsDraft.current = false;
        setNotification({ type: 'important', title: 'Error', message: 'Failed to create service - no ID returned' });
        return;
      }
      // Store in both state and ref (ref is backup for React state batching issues)
      serviceIdRef.current = newServiceId;
      setServiceId(newServiceId);
      // After creating, immediately update with all step one data
      // Pass advanceStep based on whether we're saving as draft or continuing
      const advanceStep = !isSavingAsDraft.current;
      updateStepOneMutation.mutate({ id: newServiceId, request: buildStepOneRequest(advanceStep) });
    },
    onError: (error) => {
      isSavingAsDraft.current = false;
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to create service' }));
    },
  });

  const updateStepTwoMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateServiceStepTwoRequest }) => updateServiceStepTwo(id, request),
    onSuccess: () => {
      if (isSavingAsDraft.current) {
        isSavingAsDraft.current = false;
        trackDraftSaved();
        queryClient.invalidateQueries(servicesCacheKey());
        setNotification({ type: 'success', title: 'Service saved', message: 'Service has been saved as draft' });
        navigate('/organisation-admin/core-data/services');
      } else {
        trackReviewReached();
        setStep(2);
      }
    },
    onError: (error) => {
      isSavingAsDraft.current = false;
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to update service' }));
    },
  });

  const completeMutation = useMutation({
    mutationFn: () => completeService(getServiceId()!),
    onSuccess: () => {
      trackSubmitted();
      setNotification({
        type: 'success',
        title: 'Service saved.',
        message: 'The service has been added to your list of services. You can view, change, or delete the service record at any time.',
      });
      queryClient.invalidateQueries(servicesCacheKey());
      navigate('/organisation-admin/core-data/services');
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e || 'Failed to complete service' }));
    },
  });

  const skipStepTwo = shouldSkipServiceCharacteristics(state);

  const validate = (currentStep: number): boolean => {
    if (currentStep === 0) {
      const stepErrors = validateStep(state, stepOneQuestions);
      setErrors(stepErrors);
      setSubmitAttempts((n) => n + 1);
      if (Object.keys(stepErrors).length > 0) {
        Object.entries(stepErrors).forEach(([field, message]) => { if (message) trackValidationFailed(field, message); });
        return true;
      }
      return false;
    }
    if (currentStep === 1 && !skipStepTwo) {
      const stepErrors = validateStep(state, stepTwoQuestions);
      setErrors(stepErrors);
      setSubmitAttempts((n) => n + 1);
      if (Object.keys(stepErrors).length > 0) {
        Object.entries(stepErrors).forEach(([field, message]) => { if (message) trackValidationFailed(field, message); });
        return true;
      }
      return false;
    }
    return false;
  };

  // Custom step change handler - intercepts step changes to make API calls
  const handleStepChange = (newStep: number | ((prev: number) => number)) => {
    const targetStep = typeof newStep === 'function' ? newStep(step) : newStep;

    // Going backwards - just update step, no API call needed
    if (targetStep < step) {
      setStep(targetStep);
      return;
    }

    // Going forwards - need to make API calls
    const currentServiceId = getServiceId();

    if (step === 0 && targetStep === 1) {
      // Advancing from step 0 to step 1 - create/update service with step 1 data
      if (!currentServiceId) {
        createMutation.mutate();
      } else {
        updateStepOneMutation.mutate({ id: currentServiceId, request: buildStepOneRequest(true) });
      }
      // Don't advance step here - mutation onSuccess will do it
      return;
    }

    if (step === 1 && targetStep === 2) {
      // Advancing from step 1 to step 2 - save step 2 data
      if (!currentServiceId) {
        setNotification({ type: 'important', title: 'Warning', message: 'Please complete step 1 first' });
        setStep(0);
        return;
      }
      updateStepTwoMutation.mutate({ id: currentServiceId, request: buildStepTwoRequest(true) });
      // Don't advance step here - mutation onSuccess will do it
      return;
    }

    // For any other case, just update step
    setStep(targetStep);
  };

  // handleComplete is only called on the last step (step 2 -> complete)
  const handleComplete = () => {
    const currentServiceId = getServiceId();
    if (!currentServiceId) {
      setNotification({ type: 'important', title: 'Warning', message: 'Please complete step 1 first' });
      setStep(0);
      return;
    }
    trackSubmitAttempted();
    completeMutation.mutate();
  };

  const handleSaveAsDraft = () => {
    // Service name is always required, even for drafts
    if (!state.name || state.name.trim() === '') {
      setErrors({ name: 'Please provide a service name' });
      setSubmitAttempts((n) => n + 1);
      return;
    }

    isSavingAsDraft.current = true;
    const currentServiceId = getServiceId();

    if (step === 0) {
      // On step 0, need to create service first (if not exists) then save step 1 data
      if (!currentServiceId) {
        // Service doesn't exist yet - create will chain to updateStepOne
        createMutation.mutate();
      } else {
        // Service exists, just update step 1 data (don't advance step)
        updateStepOneMutation.mutate({ id: currentServiceId, request: buildStepOneRequest(false) });
      }
    } else if (step === 1) {
      if (!currentServiceId) {
        // Service doesn't exist yet - need to create it first
        setNotification({ type: 'important', title: 'Warning', message: 'Please complete step 1 first' });
        setStep(0);
        isSavingAsDraft.current = false;
        return;
      }
      // On step 1, save step 2 data (don't advance step)
      updateStepTwoMutation.mutate({ id: currentServiceId, request: buildStepTwoRequest(false) });
    }
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
      label: 'Service details',
      component: <StepOne state={state} dispatch={dispatch} errors={errors} questions={questions} />,
    },
    ...(skipStepTwo
      ? []
      : [
          {
            label: 'Service characteristics',
            component: <StepTwo state={state} dispatch={dispatch} errors={errors} questions={questions} />,
          },
        ]),
    {
      label: 'Check your answers',
      component: <StepThree state={state} questions={questions} />,
    },
  ];

  const isLoading = createMutation.isLoading || updateStepOneMutation.isLoading || updateStepTwoMutation.isLoading || completeMutation.isLoading;

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Services', link: '/organisation-admin/core-data/services' },
      ]}
      currentPage="Add a service">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKStepper
        step={step}
        setStep={handleStepChange}
        stepperSteps={stepperSteps}
        completeLabel="Confirm and save"
        handleComplete={handleComplete}
        validate={validate}
        isNextDisabled={isLoading}
        title="Add a service"
        description="Tell us about the service offered so it can be added to your account. This information will be saved and can be updated at any time."
        helpSummary="Help with services"
        helpContent={
          <p>
            Services are the specific programmes, activities, or support offered at a location. Each service should be recorded separately to help families find the right support.
            You can add multiple services to a single location.
          </p>
        }
        onSaveAsDraft={handleSaveAsDraft}
        showSaveAsDraft={step !== stepperSteps.length - 1}
      />
    </GeneralLayout>
  );
};

export default CreateService;
