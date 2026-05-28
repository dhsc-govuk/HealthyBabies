import React, { useReducer, useState, useCallback, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, Button, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { updateSiteFormQuestion, getSiteFormQuestionById } from './mutations';
import { getAllSiteFormQuestions } from '../List/queries';
import { Stack } from '@mui/material';
import QuestionForm from '../Common/QuestionForm';
import { questionFormReducer, ActionType } from '../Common/reducer';
import { encodeForWaf, encodeNullableForWaf } from '../../../../../helpers/stringUtils';
import { ValidationErrors, validationMessages, initialFormState, requiresOptions } from '../Common/types';

const EditSiteFormQuestion = (): React.ReactElement => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [formState, dispatch] = useReducer(questionFormReducer, initialFormState);
  const [initialized, setInitialized] = useState(false);

  const [errors, setErrors] = useState<ValidationErrors>({
    code: false,
    label: false,
    options: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const { data: questionData, isLoading: loadingQuestion } = useQuery({
    queryKey: ['site-form-question', id],
    queryFn: () => getSiteFormQuestionById(id!),
    enabled: !!id,
  });

  const { data: questionsData } = useQuery({
    queryKey: ['site-form-questions-list'],
    queryFn: getAllSiteFormQuestions,
  });

  useEffect(() => {
    if (questionData?.data && !initialized) {
      const q = questionData.data;
      dispatch({
        type: ActionType.INIT_FROM_QUESTION,
        payload: {
          code: q.code,
          label: q.label,
          hint: q.hint || '',
          placeholder: q.placeholder || '',
          questionType: q.questionType,
          displayOrder: q.displayOrder,
          isRequired: q.isRequired,
          isActive: q.isActive,
          helpTextSummary: q.helpTextSummary || '',
          helpText: q.helpText || '',
          conditionalQuestionCode: q.conditionalQuestionCode || '',
          conditionalValue: q.conditionalValue || '',
          options: q.options.map((opt) => ({
            value: opt.value,
            label: opt.label,
            displayOrder: opt.displayOrder,
          })),
        },
      });
      setInitialized(true);
    }
  }, [questionData, initialized]);

  const { mutateAsync, isLoading } = useMutation({
    mutationKey: ['site-form-question-update'],
    mutationFn: (data: Parameters<typeof updateSiteFormQuestion>[1]) => updateSiteFormQuestion(id!, data),
    onSuccess() {
      queryClient.invalidateQueries(['site-form-questions-list']);
      queryClient.invalidateQueries(['site-form-question', id]);
      setNotification({ type: 'success', title: 'Success', message: 'Question updated successfully' });
      navigate('/admin/configuration/site-form-questions');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const validate = useCallback(() => {
    const errorItems: ValidationErrors = {
      code: false,
      label: !formState.label ? validationMessages.label : false,
      options:
        requiresOptions(formState.questionType) && formState.options.length === 0
          ? validationMessages.options
          : requiresOptions(formState.questionType) &&
              formState.options.some((opt) => !opt.value || !opt.label)
            ? 'All options must have a value and label'
            : false,
    };
    setErrors(errorItems);
    setSubmitAttempts((n) => n + 1);
    return !Object.values(errorItems).some((e) => e);
  }, [formState]);

  const handleSave = async () => {
    if (!validate()) return;
    await mutateAsync({
      label: encodeForWaf(formState.label),
      hint: encodeNullableForWaf(formState.hint),
      placeholder: encodeNullableForWaf(formState.placeholder),
      questionType: formState.questionType,
      displayOrder: formState.displayOrder,
      isRequired: formState.isRequired,
      isActive: formState.isActive,
      helpTextSummary: encodeNullableForWaf(formState.helpTextSummary),
      helpText: encodeNullableForWaf(formState.helpText),
      conditionalQuestionCode: formState.conditionalQuestionCode || null,
      conditionalValue: encodeNullableForWaf(formState.conditionalValue),
      options: formState.options.map((opt) => ({ ...opt, label: encodeForWaf(opt.label), value: encodeForWaf(opt.value) })),
    });
  };

  const handleCancel = () => {
    navigate('/admin/configuration/site-form-questions');
  };

  if (loadingQuestion) {
    return (
      <GeneralLayout>
        <LoadingSpinner label="Loading question" />
      </GeneralLayout>
    );
  }

  return (
    <>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Admin', link: '/admin/home' },
          { label: 'Configuration', link: '/admin/configuration' },
          { label: 'Delivery Location Form Questions', link: '/admin/configuration/site-form-questions' },
        ]}
        currentPage="Edit Question">
        <QuestionForm
          formState={formState}
          errors={errors}
          setErrors={setErrors}
          dispatch={dispatch}
          isEdit
          allQuestions={questionsData?.data ?? []}
        />
        <Stack direction="row" spacing={2} sx={{ mt: 3 }}>
          <Button onClick={handleSave}>Save</Button>
          <Button variant="secondary" onClick={handleCancel}>
            Cancel
          </Button>
        </Stack>
      </GeneralLayout>
      {isLoading && <LoadingSpinner label="Saving question" />}
    </>
  );
};

export default EditSiteFormQuestion;
