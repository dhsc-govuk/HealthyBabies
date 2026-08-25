import React, { useReducer, useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { GovUKStepper, GovUKErrorSummary, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { getSiteFormQuestions, siteFormQuestionsCacheKey, SiteFormQuestionType } from '../../../../components/Global/SiteForms';
import { processError } from '../../../../helpers/axiosErrorFallback';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import axios from 'axios';
import { ValidationErrors, initialLocationState, locationReducer, validateLocation, buildAnswersForRequest, SITE_NAME_QUESTION_CODE } from '../Common';
import LocationForm from './LocationForm';
import SummaryStep from './SummaryStep';
import { ROUTES } from '../../../../constants/routes';
import { API_ROUTES } from '../../../../constants/apiRoutes';

const LocationsCreate = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { organisationId } = useAuthProvider();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState(0);
  const [state, dispatch] = useReducer(locationReducer, initialLocationState);
  const [errors, setErrors] = useState<ValidationErrors>({});
  // Server-side rejections (e.g. duplicate site name) belong in the error summary alongside
  // client-side validation, not in a notification banner — the banner moves no focus and
  // offers no link to the offending field (DAC_Validation_Pattern_Usability_03).
  const [submitError, setSubmitError] = useState<string | undefined>(undefined);
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean) || !!submitError);

  const { trackStarted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackValidationFailed } = useFormTelemetry('location_create');

  useEffect(() => {
    trackStarted();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const { data: questionsResponse, isLoading: isLoadingQuestions } = useQuery(siteFormQuestionsCacheKey(), () => getSiteFormQuestions(), {
    staleTime: 5 * 60 * 1000,
  });

  const questions = useMemo(() => questionsResponse?.data ?? [], [questionsResponse?.data]);

  const createMutation = useMutation({
    mutationFn: (data: { answers: any[] }) => axios.post(API_ROUTES.organisations.locations.create(organisationId), data),
    onSuccess: () => {
      trackSubmitted();
      setNotification({
        type: 'success',
        title: 'Site saved.',
        message: 'The site has been added to your list of sites. You can view, change, or delete the site record at any time.',
      });
      queryClient.invalidateQueries(['locations']);
      navigate(ROUTES.organisationAdmin.locations.list);
    },
    onError: (error) => {
      processError(error, (e) => setSubmitError(e || 'Failed to create site'));
      // Bump the attempt counter so the summary is re-focused on repeat submissions, when it
      // is already mounted and its own mount-time focus will not fire again.
      setSubmitAttempts((n) => n + 1);
    },
  });

  const validate = (currentStep: number): boolean => {
    if (currentStep === 0) {
      const validationErrors = validateLocation(state, questions);
      setErrors(validationErrors);
      setSubmitAttempts((n) => n + 1);
      if (Object.keys(validationErrors).length > 0) {
        Object.entries(validationErrors).forEach(([field, message]) => {
          if (message) trackValidationFailed(field, message);
        });
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
    setSubmitError(undefined);
    createMutation.mutate({
      answers: buildAnswersForRequest(state, questions),
    });
  };

  const errorList = useMemo(() => {
    // A duplicate-name rejection is about the official public name, which lives on step 1 —
    // so the link returns to that step before focusing the field. Any other server error has
    // no specific field, so it carries no target and renders as text.
    const serverErrors = submitError
      ? [
          /already exists/i.test(submitError)
            ? {
                targetName: SITE_NAME_QUESTION_CODE.toLowerCase(),
                text: submitError,
                onNavigate: () => setStep(0),
              }
            : { text: submitError },
        ]
      : [];

    const fieldErrors = Object.entries(errors)
      .filter(([, value]) => value)
      .map(([key, value]) => {
        const lcKey = key.toLowerCase();
        const question = questions.find((q) => q.code === key);
        let targetName = lcKey;
        if (question) {
          if (question.questionType === SiteFormQuestionType.Date) {
            targetName = `${lcKey}-day`;
          } else if ((question.questionType === SiteFormQuestionType.Radio || question.questionType === SiteFormQuestionType.Checkbox) && question.options.length > 0) {
            const firstOption = [...question.options].sort((a, b) => a.displayOrder - b.displayOrder)[0];
            targetName = `${lcKey}-${firstOption.value}`;
          }
        }
        return { targetName, text: value || '' };
      });

    return [...serverErrors, ...fieldErrors];
  }, [errors, questions, submitError]);

  if (isLoadingQuestions) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading form" />
      </GeneralLayout>
    );
  }

  const stepperSteps = [
    {
      label: 'Delivery location details',
      component: <LocationForm state={state} dispatch={dispatch} errors={errors} questions={questions} />,
    },
    {
      label: 'Check your answers',
      component: <SummaryStep state={state} questions={questions} onChangeAll={() => handleStepChange(0)} />,
    },
  ];

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.organisationAdmin.home },
        { label: 'Delivery locations', link: ROUTES.organisationAdmin.locations.list },
      ]}>
      {errorList.length > 0 && <GovUKErrorSummary errors={errorList} />}
      <GovUKStepper
        step={step}
        setStep={handleStepChange}
        stepperSteps={stepperSteps}
        completeLabel="Confirm and save"
        handleComplete={handleComplete}
        validate={validate}
        isNextDisabled={createMutation.isLoading}
        title="Add delivery location"
        pageTitle="Add delivery location"
        // Drives the "Error: " title prefix (WCAG 2.4.2 / 3.3.1). errorList covers both
        // client-side validation and server rejections, and empties once they are resolved.
        pageHasErrors={errorList.length > 0}
        description="Tell us about the site so it can be added to your account. This information will be saved and can be updated at any time."
        helpSummary="Help with sites"
        helpContent={<p>Sites are the physical locations where services are delivered. Each site should be recorded separately to help families find the right support.</p>}
      />
      {createMutation.isLoading && <LoadingSpinner label="Creating site" />}
    </GeneralLayout>
  );
};

export default LocationsCreate;
