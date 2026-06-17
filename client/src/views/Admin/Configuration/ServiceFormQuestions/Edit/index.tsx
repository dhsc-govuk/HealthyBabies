import React, { useEffect, useReducer, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { LoadingSpinner, Button, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { getServiceFormQuestionById } from './queries';
import { updateServiceFormQuestion } from './mutations';
import { getAllServiceFormQuestions } from '../List/queries';
import { Stack } from '@mui/material';
import QuestionForm from '../Common/QuestionForm';
import { questionFormReducer, QuestionFormAction } from '../Common/reducer';
import { encodeForWaf, encodeNullableForWaf } from '../../../../../helpers/stringUtils';
import { ValidationErrors, validationMessages, initialFormState, requiresOptions, QuestionFormState } from '../Common/types';

type UrlParams = {
  questionId: string;
};

const EditServiceFormQuestion = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { questionId } = useParams<UrlParams>();

  const [formState, dispatch] = useReducer(questionFormReducer, initialFormState);

  const [errors, setErrors] = useState<ValidationErrors>({
    code: false,
    label: false,
    options: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const { data, isLoading: loading } = useQuery({
    queryKey: ['service-form-question-edit', questionId],
    queryFn: () => getServiceFormQuestionById(questionId!),
    enabled: !!questionId,
  });

  const { data: questionsData } = useQuery({
    queryKey: ['service-form-questions-list'],
    queryFn: getAllServiceFormQuestions,
  });

  useEffect(() => {
    if (data?.data) {
      const q = data.data;
      const state: QuestionFormState = {
        code: q.code,
        label: q.label,
        hint: q.hint || '',
        placeholder: q.placeholder || '',
        questionType: q.questionType,
        step: q.step,
        displayOrder: q.displayOrder,
        isRequired: q.isRequired,
        isPredefined: q.isPredefined,
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
      };
      dispatch({ type: QuestionFormAction.INIT, value: state });
    }
  }, [data]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['service-form-question-update'],
    mutationFn: updateServiceFormQuestion,
    onSuccess() {
      queryClient.invalidateQueries(['service-form-questions-list']);
      queryClient.invalidateQueries(['service-form-question-edit', questionId]);
      setNotification({ type: 'success', title: 'Success', message: 'Question updated successfully' });
      navigate('/admin/configuration/service-form-questions');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const validate = useCallback(() => {
    const needsOptions = requiresOptions(formState.questionType) && formState.isPredefined;
    const errorItems: ValidationErrors = {
      code: false,
      label: !formState.label ? validationMessages.label : false,
      options:
        needsOptions && formState.options.length === 0
          ? validationMessages.options
          : needsOptions && formState.options.some((opt) => !opt.value || !opt.label)
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
      id: questionId!,
      label: encodeForWaf(formState.label),
      hint: encodeNullableForWaf(formState.hint),
      placeholder: encodeNullableForWaf(formState.placeholder),
      questionType: formState.questionType,
      step: formState.step,
      displayOrder: formState.displayOrder,
      isRequired: formState.isRequired,
      isPredefined: formState.isPredefined,
      isActive: formState.isActive,
      helpTextSummary: encodeNullableForWaf(formState.helpTextSummary),
      helpText: encodeNullableForWaf(formState.helpText),
      conditionalQuestionCode: formState.conditionalQuestionCode || null,
      conditionalValue: encodeNullableForWaf(formState.conditionalValue),
      options: formState.isPredefined
        ? formState.options.map((opt) => ({ ...opt, label: encodeForWaf(opt.label), value: encodeForWaf(opt.value) }))
        : [],
    });
  };

  const handleCancel = () => {
    navigate('/admin/configuration/service-form-questions');
  };

  return (
    <LoadingSpinner loading={loading || saving} label={loading ? 'Loading question' : 'Saving changes'}>
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
        ]}
        backLink={{ href: '/admin/configuration/service-form-questions', onClick: handleCancel }}
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
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default EditServiceFormQuestion;
