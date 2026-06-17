import React, { useReducer, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { LoadingSpinner, Button, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { createServiceFormQuestion } from './mutations';
import { getAllServiceFormQuestions } from '../List/queries';
import { Stack } from '@mui/material';
import QuestionForm from '../Common/QuestionForm';
import { questionFormReducer } from '../Common/reducer';
import { encodeForWaf, encodeNullableForWaf } from '../../../../../helpers/stringUtils';
import { ValidationErrors, validationMessages, initialFormState, requiresOptions } from '../Common/types';

const CreateServiceFormQuestion = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [formState, dispatch] = useReducer(questionFormReducer, initialFormState);

  const [errors, setErrors] = useState<ValidationErrors>({
    code: false,
    label: false,
    options: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const { data: questionsData } = useQuery({
    queryKey: ['service-form-questions-list'],
    queryFn: getAllServiceFormQuestions,
  });

  const { mutateAsync, isLoading } = useMutation({
    mutationKey: ['service-form-question-create'],
    mutationFn: createServiceFormQuestion,
    onSuccess() {
      queryClient.invalidateQueries(['service-form-questions-list']);
      setNotification({ type: 'success', title: 'Success', message: 'Question created successfully' });
      navigate('/admin/configuration/service-form-questions');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const validate = useCallback(() => {
    const needsOptions = requiresOptions(formState.questionType) && formState.isPredefined;
    const errorItems: ValidationErrors = {
      code: !formState.code
        ? validationMessages.code
        : !/^[A-Za-z0-9]+$/.test(formState.code) || formState.code.length > 10
          ? validationMessages.codeFormat
          : false,
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
      code: formState.code,
      label: encodeForWaf(formState.label),
      hint: encodeNullableForWaf(formState.hint),
      placeholder: encodeNullableForWaf(formState.placeholder),
      questionType: formState.questionType,
      step: formState.step,
      isRequired: formState.isRequired,
      isPredefined: formState.isPredefined,
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
    <LoadingSpinner loading={isLoading} label="Creating question">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
        ]}
        backLink={{ href: '/admin/configuration/service-form-questions', onClick: handleCancel }}
        currentPage="Create Question">
        <QuestionForm
          formState={formState}
          errors={errors}
          setErrors={setErrors}
          dispatch={dispatch}
          allQuestions={questionsData?.data ?? []}
        />
        <Stack direction="row" spacing={2} sx={{ mt: 3 }}>
          <Button onClick={handleSave}>Create</Button>
          <Button variant="secondary" onClick={handleCancel}>
            Cancel
          </Button>
        </Stack>
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default CreateServiceFormQuestion;
