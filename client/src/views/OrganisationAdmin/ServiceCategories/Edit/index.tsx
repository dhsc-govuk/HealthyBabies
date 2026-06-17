import React, { useReducer, useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { ErrorSummary, GovUKButton, SummaryList, H1, H2, Paragraph, GovUKDynamicQuestionRenderer } from '../../../../components/GovUKComponents';
import {
  getServiceCategory,
  serviceCategoryCacheKey,
  getServiceCategoryFormQuestionsByStep,
  updateServiceCategoryStepOne,
  completeServiceCategory,
  serviceCategoriesCacheKey,
} from '../../../../components/Global/ServiceCategories';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import usePageTitle from '../../../../hooks/usePageTitle';
import {
  ServiceCategoryReducerAction,
  serviceCategoryReducer,
  initialServiceCategoryState,
  validateStep,
  buildAnswersForRequest,
  ValidationErrors,
  mapServiceCategoryToFormState,
  replaceCategoryNamePlaceholder,
} from '../Common';

const EditServiceCategory = (): React.ReactElement => {
  usePageTitle('Edit wider services category');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { serviceCategoryId } = useParams<{ serviceCategoryId: string }>();

  const [state, dispatch] = useReducer(serviceCategoryReducer, initialServiceCategoryState);
  const [currentStep, setCurrentStep] = useState(1);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [isInitialized, setIsInitialized] = useState(false);

  const { data: serviceCategoryData, isLoading: isLoadingCategory } = useQuery({
    queryKey: serviceCategoryCacheKey(serviceCategoryId!),
    queryFn: () => getServiceCategory(serviceCategoryId!),
    enabled: !!serviceCategoryId,
  });

  const { data: questionsData, isLoading: isLoadingQuestions } = useQuery({
    queryKey: ['service-category-form-questions', 'step', 1],
    queryFn: () => getServiceCategoryFormQuestionsByStep(1),
  });

  const serviceCategory = serviceCategoryData?.data;
  const questions = questionsData?.data ?? [];

  useEffect(() => {
    if (serviceCategory && !isInitialized) {
      const formState = mapServiceCategoryToFormState(serviceCategory);
      dispatch({
        type: ServiceCategoryReducerAction.INIT_FROM_SERVICE_CATEGORY,
        payload: formState,
      });
      setIsInitialized(true);
    }
  }, [serviceCategory, isInitialized]);

  const updateStepOneMutation = useMutation({
    mutationFn: () =>
      updateServiceCategoryStepOne(serviceCategoryId!, {
        answers: buildAnswersForRequest(state, questions),
        advanceStep: true,
      }),
    onSuccess: () => {
      setCurrentStep(2);
    },
  });

  const completeMutation = useMutation({
    mutationFn: () => completeServiceCategory(serviceCategoryId!),
    onSuccess: () => {
      queryClient.invalidateQueries(serviceCategoriesCacheKey());
      queryClient.invalidateQueries(serviceCategoryCacheKey(serviceCategoryId!));
      queryClient.invalidateQueries(['organisation-admin-submission']);
      queryClient.invalidateQueries(['wider-service-users-module']);
      navigate('/organisation-admin/core-data/wider-service-categories', {
        state: {
          notification: {
            type: 'success',
            title: 'Wider services category saved',
            message: 'The category has been updated in your list of wider services categories. You can view, change, or delete the category record at any time.',
          },
        },
      });
    },
  });

  const handleContinue = async () => {
    const validationErrors = validateStep(state, questions);
    setErrors(validationErrors);
    setSubmitAttempts((n) => n + 1);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    await updateStepOneMutation.mutateAsync();
  };

  const handleSubmit = async () => {
    await completeMutation.mutateAsync();
  };

  const handleBack = () => {
    if (currentStep === 2) {
      setCurrentStep(1);
    } else {
      navigate('/organisation-admin/core-data/wider-service-categories');
    }
  };

  const handleChangeAnswer = () => {
    setCurrentStep(1);
  };

  const isLoading = isLoadingCategory || isLoadingQuestions || updateStepOneMutation.isLoading || completeMutation.isLoading;

  const errorList = Object.entries(errors).map(([key, message]) => ({
    targetName: key.toLowerCase(),
    text: message || '',
  }));

  if (isLoadingCategory || isLoadingQuestions) {
    return (
      <LoadingBox loading>
        <GeneralLayout>
          <Paragraph>Loading...</Paragraph>
        </GeneralLayout>
      </LoadingBox>
    );
  }

  if (!serviceCategory) {
    return (
      <GeneralLayout>
        <Paragraph>Service category not found.</Paragraph>
        <Link to="/organisation-admin/core-data/wider-service-categories" className="govuk-link">
          Back to categories
        </Link>
      </GeneralLayout>
    );
  }

  const renderStepOne = () => (
    <>
      <span className="govuk-caption-l">Step 1 of 2</span>
      <H1>Change wider service</H1>
      <H2>{state.name}</H2>

      {errorList.length > 0 && <ErrorSummary heading="There is a problem" errors={errorList} />}

      <GovUKDynamicQuestionRenderer
        questions={questions}
        answers={state.answers}
        errors={errors}
        onAnswerChange={(questionCode, value) =>
          dispatch({
            type: ServiceCategoryReducerAction.SET_ANSWER,
            payload: { code: questionCode, value },
          })
        }
        placeholderReplacer={(text) => replaceCategoryNamePlaceholder(text, state.name)}
      />

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleContinue} disabled={isLoading}>
          Save and continue
        </GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => navigate('/organisation-admin/core-data/wider-service-categories')} disabled={isLoading}>
          Save as draft and exit
        </GovUKButton>
      </div>
    </>
  );

  const renderStepTwo = () => {
    const visibleQuestions = questions.filter((q) => {
      if (!q.conditionalQuestionCode) return true;
      const parentValue = state.answers[q.conditionalQuestionCode];
      return parentValue === q.conditionalValue;
    });

    const summaryItems = visibleQuestions.map((question) => {
      const value = state.answers[question.code];
      let displayValue = value || 'Not provided';

      if (question.options.length > 0 && value) {
        if (value.includes(',')) {
          const values = value.split(',');
          const labels = values.map((v) => question.options.find((o) => o.value === v)?.label || v).join(', ');
          displayValue = labels;
        } else {
          const option = question.options.find((o) => o.value === value);
          displayValue = option?.label || value;
        }
      }

      return {
        label: replaceCategoryNamePlaceholder(question.label, state.name),
        value: displayValue,
        onAction: handleChangeAnswer,
        actionLabel: 'Change',
      };
    });

    return (
      <>
        <span className="govuk-caption-l">Step 2 of 2</span>
        <H1>Change &apos;{state.name}&apos; services</H1>

        <H2>Check your answers before saving your wider services category</H2>
        <Paragraph>
          Check the information you&apos;ve entered before saving the wider services category. Make sure the details are accurate and complete. You can go back to make changes if
          anything needs updating.
        </Paragraph>

        <SummaryList items={summaryItems} noOuterBorder equalColumns />

        <H2>Now save your wider services category</H2>
        <Paragraph>
          By saving the wider services category, you are confirming that, to the best of your knowledge, the details you are providing are correct. You may return and update these
          details whenever necessary if anything changes.
        </Paragraph>
        <Paragraph>Please review the information regularly and ensure it is up to date, especially before the Delivery Plan and Management Information submissions.</Paragraph>

        <div className="govuk-button-group">
          <GovUKButton onClick={handleSubmit} disabled={isLoading}>
            Confirm and save
          </GovUKButton>
        </div>
      </>
    );
  };

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout
        backLink={{
          href: '/organisation-admin/core-data/wider-service-categories',
          onClick: handleBack,
        }}>
        {currentStep === 1 ? renderStepOne() : renderStepTwo()}
      </GeneralLayout>
    </LoadingBox>
  );
};

export default EditServiceCategory;
