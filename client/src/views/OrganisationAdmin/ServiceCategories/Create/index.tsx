import React, { useReducer, useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { ErrorSummary, GovUKButton, SummaryList, H1, H2, Paragraph, GovUKDynamicQuestionRenderer } from '../../../../components/GovUKComponents';
import {
  getServiceCategoryFormQuestionsByStep,
  createServiceCategory,
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
  replaceCategoryNamePlaceholder,
} from '../Common';
import { encodeForWaf } from '../../../../helpers/stringUtils';

interface LocationState {
  categoryCode: string;
  categoryName: string;
}

const CreateServiceCategory = (): React.ReactElement => {
  usePageTitle('Add wider services category');
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const locationState = location.state as LocationState | null;

  const [state, dispatch] = useReducer(serviceCategoryReducer, initialServiceCategoryState);
  const [currentStep, setCurrentStep] = useState(1);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [serviceCategoryId, setServiceCategoryId] = useState<string | null>(null);

  useEffect(() => {
    if (locationState) {
      dispatch({
        type: ServiceCategoryReducerAction.SET_CATEGORY_CODE,
        payload: locationState.categoryCode,
      });
      dispatch({
        type: ServiceCategoryReducerAction.SET_NAME,
        payload: locationState.categoryName,
      });
    }
  }, [locationState]);

  const { data: questionsData, isLoading: isLoadingQuestions } = useQuery({
    queryKey: ['service-category-form-questions', 'step', 1],
    queryFn: () => getServiceCategoryFormQuestionsByStep(1),
  });

  const questions = questionsData?.data ?? [];

  const createMutation = useMutation({
    mutationFn: () =>
      createServiceCategory({
        categoryCode: state.categoryCode,
        categoryName: encodeForWaf(state.name),
      }),
    onSuccess: (response) => {
      setServiceCategoryId(response.data.id);
    },
  });

  const updateStepOneMutation = useMutation({
    mutationFn: (id: string) =>
      updateServiceCategoryStepOne(id, {
        answers: buildAnswersForRequest(state, questions),
        advanceStep: true,
      }),
    onSuccess: () => {
      setCurrentStep(2);
    },
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => completeServiceCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries(serviceCategoriesCacheKey());
      queryClient.invalidateQueries(['organisation-admin-submission']);
      queryClient.invalidateQueries(['wider-service-users-module']);
      navigate('/organisation-admin/core-data/wider-service-categories', {
        state: {
          notification: {
            type: 'success',
            title: 'Wider services category saved',
            message: 'The category has been added to your list of wider services categories. You can view, change, or delete the category record at any time.',
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

    if (!serviceCategoryId) {
      const createResult = await createMutation.mutateAsync();
      await updateStepOneMutation.mutateAsync(createResult.data.id!);
    } else {
      await updateStepOneMutation.mutateAsync(serviceCategoryId);
    }
  };

  const handleSubmit = async () => {
    if (serviceCategoryId) {
      await completeMutation.mutateAsync(serviceCategoryId);
    }
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

  const handleSaveAsDraft = async () => {
    const validationErrors = validateStep(state, questions);
    setErrors(validationErrors);
    setSubmitAttempts((n) => n + 1);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    if (!serviceCategoryId) {
      const createResult = await createMutation.mutateAsync();
      await updateServiceCategoryStepOne(createResult.data.id!, {
        answers: buildAnswersForRequest(state, questions),
        advanceStep: false,
      });
      queryClient.invalidateQueries(serviceCategoriesCacheKey());
      queryClient.invalidateQueries(['organisation-admin-submission']);
      queryClient.invalidateQueries(['wider-service-users-module']);
      navigate('/organisation-admin/core-data/wider-service-categories', {
        state: {
          notification: {
            type: 'success',
            title: 'Success',
            message: `${state.name} has been saved as draft.`,
          },
        },
      });
    } else {
      await updateServiceCategoryStepOne(serviceCategoryId, {
        answers: buildAnswersForRequest(state, questions),
        advanceStep: false,
      });
      queryClient.invalidateQueries(serviceCategoriesCacheKey());
      queryClient.invalidateQueries(['organisation-admin-submission']);
      queryClient.invalidateQueries(['wider-service-users-module']);
      navigate('/organisation-admin/core-data/wider-service-categories', {
        state: {
          notification: {
            type: 'success',
            title: 'Success',
            message: `${state.name} has been saved as draft.`,
          },
        },
      });
    }
  };

  const isLoading = isLoadingQuestions || createMutation.isLoading || updateStepOneMutation.isLoading || completeMutation.isLoading;

  const errorList = Object.entries(errors).map(([key, message]) => ({
    targetName: key.toLowerCase(),
    text: message || '',
  }));

  if (!locationState) {
    return (
      <GeneralLayout>
        <Paragraph>No category selected. Please go back and select a category to add.</Paragraph>
        <Link to="/organisation-admin/core-data/wider-service-categories" className="govuk-link">
          Back to categories
        </Link>
      </GeneralLayout>
    );
  }

  const categoryDisplayName = state.name || locationState?.categoryName || '';

  const renderStepOne = () => (
    <>
      {errorList.length > 0 && <ErrorSummary heading="There is a problem" errors={errorList} />}

      <span className="govuk-caption-l">Step 1 of 2</span>
      <H1>Add &apos;{categoryDisplayName}&apos; services</H1>

      <Paragraph>Tell us about the wider services offered so it can be added to your account. This information will be saved and can be updated at any time.</Paragraph>

      <details className="govuk-details">
        <summary className="govuk-details__summary">
          <span className="govuk-details__summary-text">Help with wider services</span>
        </summary>
        <div className="govuk-details__text">Wider services are additional services offered by Family Hubs in your local authority that support families beyond core services.</div>
      </details>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

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
        placeholderReplacer={(text) => replaceCategoryNamePlaceholder(text, categoryDisplayName)}
      />

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleContinue} disabled={isLoading}>
          Save and continue
        </GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} disabled={isLoading}>
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
        label: replaceCategoryNamePlaceholder(question.label, categoryDisplayName),
        value: displayValue,
        onAction: handleChangeAnswer,
        actionLabel: 'Change',
      };
    });

    return (
      <>
        <span className="govuk-caption-l">Step 2 of 2</span>
        <H1>Add &apos;{categoryDisplayName}&apos; services</H1>

        <H2>Check your answers before saving your wider services category</H2>
        <Paragraph>
          Check the information you&apos;ve entered before saving the wider services category. Make sure the details are accurate and complete. You can go back to make changes if
          anything needs updating.
        </Paragraph>

        <SummaryList items={summaryItems} noOuterBorder />

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

export default CreateServiceCategory;
