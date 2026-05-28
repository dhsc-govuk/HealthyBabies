import React, { useReducer, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, Button, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { createSiteFormQuestion } from './mutations';
import { getAllSiteFormQuestions } from '../List/queries';
import { Stack } from '@mui/material';
import QuestionForm from '../Common/QuestionForm';
import { questionFormReducer } from '../Common/reducer';
import { encodeForWaf, encodeNullableForWaf } from '../../../../../helpers/stringUtils';
import { ValidationErrors, validationMessages, initialFormState, requiresOptions } from '../Common/types';

const CreateSiteFormQuestion = (): React.ReactElement => {
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
    queryKey: ['site-form-questions-list'],
    queryFn: getAllSiteFormQuestions,
  });

  const { mutateAsync, isLoading } = useMutation({
    mutationKey: ['site-form-question-create'],
    mutationFn: createSiteFormQuestion,
    onSuccess() {
      queryClient.invalidateQueries(['site-form-questions-list']);
      setNotification({ type: 'success', title: 'Success', message: 'Question created successfully' });
      navigate('/admin/configuration/site-form-questions');
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const validate = useCallback(() => {
    const errorItems: ValidationErrors = {
      code: !formState.code
        ? validationMessages.code
        : !/^[A-Za-z0-9]+$/.test(formState.code) || formState.code.length > 10
          ? validationMessages.codeFormat
          : false,
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
      code: formState.code,
      label: encodeForWaf(formState.label),
      hint: encodeNullableForWaf(formState.hint),
      placeholder: encodeNullableForWaf(formState.placeholder),
      questionType: formState.questionType,
      isRequired: formState.isRequired,
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

  return (
    <>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Admin', link: '/admin/home' },
          { label: 'Configuration', link: '/admin/configuration' },
          { label: 'Delivery Location Form Questions', link: '/admin/configuration/site-form-questions' },
        ]}
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
      </GeneralLayout>
      {isLoading && <LoadingSpinner label="Creating question" />}
    </>
  );
};

export default CreateSiteFormQuestion;
