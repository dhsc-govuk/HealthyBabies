import React, { useReducer, useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { GovUKStepper, ErrorSummary, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import {
  getService,
  serviceCacheKey,
  updateServiceStepOne,
  updateServiceStepTwo,
  completeService,
  servicesCacheKey,
  getServiceFormQuestions,
  serviceFormQuestionsCacheKey,
  UpdateServiceStepOneRequest,
  UpdateServiceStepTwoRequest,
  ServiceStatus,
} from '../../../../components/Global/Services';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { encodeForWaf } from '../../../../helpers/stringUtils';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import {
  ValidationErrors,
  initialServiceState,
  serviceReducer,
  ServiceReducerAction,
  validateStep,
  buildAnswersForRequest,
  mapServiceToFormState,
  shouldSkipServiceCharacteristics,
} from '../Common';
import StepOne from '../Create/StepOne';
import StepTwo from '../Create/StepTwo';
import StepThree from '../Create/StepThree';

const EditService = (): React.ReactElement => {
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState(0);
  const [state, dispatch] = useReducer(serviceReducer, initialServiceState);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [initialized, setInitialized] = useState(false);
  const isSavingAsDraft = useRef(false);

  const { trackStarted, trackSectionCompleted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackDraftSaved, trackValidationFailed } =
    useFormTelemetry('service_edit');

  const { data: serviceData, isLoading: isLoadingService } = useQuery({
    queryKey: serviceCacheKey(serviceId!),
    queryFn: () => getService(serviceId!),
    enabled: !!serviceId,
  });

  // Fetch all service form questions
  const { data: questionsResponse, isLoading: isLoadingQuestions } = useQuery(serviceFormQuestionsCacheKey(), () => getServiceFormQuestions(), {
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const questions = questionsResponse?.data ?? [];
  const stepOneQuestions = questions.filter((q) => q.step === 1);
  const stepTwoQuestions = questions.filter((q) => q.step === 2);

  // Initialize form state from loaded service
  useEffect(() => {
    if (serviceData?.data && !initialized) {
      const service = serviceData.data;
      const formState = mapServiceToFormState(service);
      dispatch({ type: ServiceReducerAction.INIT_FROM_SERVICE, payload: formState });

      // Set step based on status and currentStep from backend
      // currentStep from backend: 1 = just created, 2 = step 1 done, 3 = step 2 done
      // UI step: 0 = step 1, 1 = step 2, 2 = step 3 (summary)
      if (service.status === ServiceStatus.Complete) {
        // For complete services, start from step 1 (UI step 0) so user can review all data
        setStep(0);
      } else {
        // For draft services, resume from where they left off
        // currentStep 1 -> UI step 0 (Service details)
        // currentStep 2 -> UI step 1 (Service characteristics)
        // currentStep 3 -> UI step 2 (Summary) - shouldn't happen for draft
        // When SMD03 = no_longer_offered, the stepper has only 2 steps,
        // so clamp to the dynamic last index to avoid landing out of range.
        const skipStepTwo = formState.answers.SMD03 === 'no_longer_offered';
        const lastStepIndex = skipStepTwo ? 1 : 2;
        const backendStep = service.currentStep ?? 1;
        const uiStep = Math.min(Math.max(0, backendStep - 1), lastStepIndex);
        setStep(uiStep);
      }
      setInitialized(true);
      trackStarted();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceData, initialized]);

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
    mutationFn: (request: UpdateServiceStepOneRequest) => updateServiceStepOne(serviceId!, request),
    onSuccess: () => {
      if (isSavingAsDraft.current) {
        isSavingAsDraft.current = false;
        trackDraftSaved();
        setNotification({ type: 'success', title: 'Service saved', message: 'Service has been saved as draft' });
        queryClient.invalidateQueries(servicesCacheKey());
        queryClient.invalidateQueries(serviceCacheKey(serviceId!));
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

  const updateStepTwoMutation = useMutation({
    mutationFn: (request: UpdateServiceStepTwoRequest) => updateServiceStepTwo(serviceId!, request),
    onSuccess: () => {
      if (isSavingAsDraft.current) {
        isSavingAsDraft.current = false;
        trackDraftSaved();
        setNotification({ type: 'success', title: 'Service saved', message: 'Service has been saved as draft' });
        queryClient.invalidateQueries(servicesCacheKey());
        queryClient.invalidateQueries(serviceCacheKey(serviceId!));
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
    mutationFn: () => completeService(serviceId!),
    onSuccess: () => {
      trackSubmitted();
      setNotification({
        type: 'success',
        title: 'Service saved.',
        message: 'The service has been added to your list of services. You can view, change, or delete the service record at any time.',
      });
      queryClient.invalidateQueries(servicesCacheKey());
      queryClient.invalidateQueries(serviceCacheKey(serviceId!));
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

    // Going forwards - need to make API calls to save data
    if (step === 0 && targetStep === 1) {
      // Advancing from step 0 to step 1 - save step 1 data
      updateStepOneMutation.mutate(buildStepOneRequest(true));
      // Don't advance step here - mutation onSuccess will do it
      return;
    }

    if (step === 1 && targetStep === 2) {
      // Advancing from step 1 to step 2 - save step 2 data
      updateStepTwoMutation.mutate(buildStepTwoRequest(true));
      // Don't advance step here - mutation onSuccess will do it
      return;
    }

    // For any other case, just update step
    setStep(targetStep);
  };

  const handleComplete = () => {
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

    if (step === 0) {
      // Save step 1 data without advancing step
      updateStepOneMutation.mutate(buildStepOneRequest(false));
    } else if (step === 1) {
      // Save step 2 data without advancing step
      updateStepTwoMutation.mutate(buildStepTwoRequest(false));
    }
  };

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

  const isLoading = updateStepOneMutation.isLoading || updateStepTwoMutation.isLoading || completeMutation.isLoading;

  if (isLoadingService || isLoadingQuestions || !initialized) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading service" />
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Services', link: '/organisation-admin/core-data/services' },
        { label: serviceData?.data?.name ?? 'Service', link: `/organisation-admin/core-data/services/${serviceId}` },
      ]}
      currentPage="Edit service">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKStepper
        step={step}
        setStep={handleStepChange}
        stepperSteps={stepperSteps}
        completeLabel="Confirm and save"
        handleComplete={handleComplete}
        validate={validate}
        isNextDisabled={isLoading}
        title="Edit service"
        description="Update the service details. This information will be saved and can be updated at any time."
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

export default EditService;
