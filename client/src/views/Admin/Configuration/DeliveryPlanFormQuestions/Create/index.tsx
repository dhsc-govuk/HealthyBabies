import React, { useReducer, useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { Box } from '@mui/material';
import { GovUKButton, LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageTitle } from '../../../../../styles/govuk-global';
import QuestionForm from '../../DataCollectionFormQuestions/Common/QuestionForm';
import { getConditionFieldKeys } from '../../DataCollectionFormQuestions/Common/ConditionalRulesBuilder';
import { questionFormReducer } from '../../DataCollectionFormQuestions/Common/reducer';
import {
  initialFormState,
  requiresOptions,
  validationMessages,
  ValidationErrors,
  FormSection,
  FormField,
} from '../../DataCollectionFormQuestions/Common/types';
import { getAllModules } from '../List/queries';
import { createFormField, CreateFormFieldRequest } from './mutations';
import axios from 'axios';
import usePageTitle from '../../../../../hooks/usePageTitle';

const DeliveryPlanFormQuestionsCreate = (): React.ReactElement => {
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const [formState, dispatch] = useReducer(questionFormReducer, initialFormState);
  const [errors, setErrors] = useState<ValidationErrors>({
    fieldKey: false,
    label: false,
    options: false,
    conditionalRules: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(
    submitAttempts,
    (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k])
  );
  usePageTitle('Add delivery plan question', Object.values(errors).some(Boolean));
  const [allSections, setAllSections] = useState<FormSection[]>([]);
  const [availableFields, setAvailableFields] = useState<FormField[]>([]);

  const { data: allModules = [], isLoading: modulesLoading } = useQuery({
    queryKey: ['dataCollectionFormModules'],
    queryFn: getAllModules,
  });
  const modules = allModules.filter((m) => m.type === 'delivery-plan');

  useEffect(() => {
    const fetchModuleData = async () => {
      if (formState.formModuleId) {
        try {
          const response = await axios.get(`/data-collection-form-questions/modules/${formState.formModuleId}`);
          setAllSections(response.data.sections || []);
          setAvailableFields(response.data.fields || []);
        } catch (error) {
          console.error('Failed to fetch module data', error);
        }
      }
    };
    fetchModuleData();
  }, [formState.formModuleId]);

  const createMutation = useMutation({
    mutationFn: createFormField,
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Question created successfully' });
      navigate('/admin/configuration/delivery-plan-form-questions');
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to create question' });
    },
  });

  const validateForm = useCallback((): boolean => {
    // Ordering (forward-ref) isn't checked here: a new question is always appended after every
    // existing one, so nothing it could reference is ever "later" in the form.
    const conditionFieldKeys = getConditionFieldKeys(formState.conditionalRules);
    const hasDuplicateCondition = new Set(conditionFieldKeys).size !== conditionFieldKeys.length;

    const newErrors: ValidationErrors = {
      fieldKey: !formState.fieldKey ? 'Field key is required' : false,
      label: !formState.label ? 'Label is required' : false,
      options: requiresOptions(formState.fieldType) && formState.options.length === 0 ? 'At least one option is required' : false,
      conditionalRules: hasDuplicateCondition ? validationMessages.conditionalRulesDuplicate : false,
    };
    setErrors(newErrors);
    setSubmitAttempts((n) => n + 1);
    return !newErrors.fieldKey && !newErrors.label && !newErrors.options && !newErrors.conditionalRules;
  }, [formState]);

  const handleSubmit = useCallback(() => {
    if (!validateForm()) return;

    const request: CreateFormFieldRequest = {
      formModuleId: formState.formModuleId,
      formSectionId: formState.formSectionId || null,
      fieldKey: formState.fieldKey,
      label: formState.label,
      fieldType: formState.fieldType,
      isRequired: formState.isRequired,
      placeholder: formState.placeholder || null,
      helpText: formState.helpText || null,
      defaultValue: formState.defaultValue || null,
      validationRules: formState.validationRules || null,
      conditionalRules: formState.conditionalRules || null,
      configuration: formState.configuration || null,
      options: formState.options,
    };

    createMutation.mutate(request);
  }, [formState, validateForm, createMutation]);

  const handleCancel = () => {
    navigate('/admin/configuration/delivery-plan-form-questions');
  };

  return (
    <LoadingSpinner loading={modulesLoading || createMutation.isLoading} label="Loading...">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
          { label: 'Delivery Plan Form Questions', link: '/admin/configuration/delivery-plan-form-questions' },
        ]}>
        <PageHeaderContainer>
          <PageTitle>Add Question</PageTitle>
        </PageHeaderContainer>

        <QuestionForm
          formState={formState}
          errors={errors}
          setErrors={setErrors}
          dispatch={dispatch}
          modules={modules}
          sections={allSections}
          availableFields={availableFields}
          isEdit={false}
        />

        <Box sx={{ mt: 4, display: 'flex', gap: 2 }}>
          <GovUKButton onClick={handleSubmit} isLoading={createMutation.isLoading}>
            Save Question
          </GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleCancel}>
            Cancel
          </GovUKButton>
        </Box>
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default DeliveryPlanFormQuestionsCreate;
