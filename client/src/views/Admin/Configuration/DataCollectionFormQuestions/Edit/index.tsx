import React, { useReducer, useState, useCallback, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { Box } from '@mui/material';
import { GovUKButton, LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../hooks/useErrorSummaryFocus';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageTitle } from '../../../../../styles/govuk-global';
import QuestionForm from '../Common/QuestionForm';
import { questionFormReducer, QuestionFormAction } from '../Common/reducer';
import { initialFormState, requiresOptions, ValidationErrors, FormSection, QuestionFormState, FormField } from '../Common/types';
import { getAllModules } from '../List/queries';
import { getFormFieldById } from './queries';
import { updateFormField, UpdateFormFieldRequest } from './mutations';
import axios from 'axios';
import usePageTitle from '../../../../../hooks/usePageTitle';

const DataCollectionFormQuestionsEdit = (): React.ReactElement => {
  usePageTitle('Edit question');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const [formState, dispatch] = useReducer(questionFormReducer, initialFormState);
  const [errors, setErrors] = useState<ValidationErrors>({
    fieldKey: false,
    label: false,
    options: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(
    submitAttempts,
    (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k])
  );
  const [allSections, setAllSections] = useState<FormSection[]>([]);
  const [availableFields, setAvailableFields] = useState<FormField[]>([]);
  const [initialized, setInitialized] = useState(false);

  const { data: modules = [], isLoading: modulesLoading } = useQuery({
    queryKey: ['dataCollectionFormModules'],
    queryFn: getAllModules,
  });

  const { data: field, isLoading: fieldLoading } = useQuery({
    queryKey: ['formField', id],
    queryFn: () => getFormFieldById(id!),
    enabled: !!id,
    staleTime: 0,
    cacheTime: 0,
  });

  useEffect(() => {
    if (field && !initialized) {
      const state: QuestionFormState = {
        fieldKey: field.fieldKey,
        label: field.label,
        fieldType: field.fieldType,
        formModuleId: field.formModuleId,
        formSectionId: field.formSectionId || '',
        displayOrder: field.displayOrder,
        isRequired: field.isRequired,
        isActive: field.isActive,
        placeholder: field.placeholder || '',
        helpText: field.helpText || '',
        defaultValue: field.defaultValue || '',
        validationRules: field.validationRules || '',
        conditionalRules: field.conditionalRules || '',
        configuration: field.configuration || '',
        options: field.options.map((opt) => ({
          value: opt.value,
          label: opt.label,
          displayOrder: opt.displayOrder,
          isDefault: opt.isDefault,
        })),
      };
      dispatch({ type: QuestionFormAction.INIT, value: state });
      setInitialized(true);
    }
  }, [field, initialized]);

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

  const updateMutation = useMutation({
    mutationFn: (request: UpdateFormFieldRequest) => updateFormField(id!, request),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Question updated successfully' });
      navigate('/admin/configuration/data-collection-form-questions');
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to update question' });
    },
  });

  const validateForm = useCallback((): boolean => {
    const newErrors: ValidationErrors = {
      fieldKey: false,
      label: !formState.label ? 'Label is required' : false,
      options: requiresOptions(formState.fieldType) && formState.options.length === 0 ? 'At least one option is required' : false,
    };
    setErrors(newErrors);
    setSubmitAttempts((n) => n + 1);
    return !newErrors.label && !newErrors.options;
  }, [formState]);

  const handleSubmit = useCallback(() => {
    if (!validateForm()) return;

    const request: UpdateFormFieldRequest = {
      formSectionId: formState.formSectionId || null,
      label: formState.label,
      fieldType: formState.fieldType,
      displayOrder: formState.displayOrder,
      isRequired: formState.isRequired,
      isActive: formState.isActive,
      placeholder: formState.placeholder || null,
      helpText: formState.helpText || null,
      defaultValue: formState.defaultValue || null,
      validationRules: formState.validationRules || null,
      conditionalRules: formState.conditionalRules || null,
      configuration: formState.configuration || null,
      options: formState.options,
    };

    updateMutation.mutate(request);
  }, [formState, validateForm, updateMutation]);

  const handleCancel = () => {
    navigate('/admin/configuration/data-collection-form-questions');
  };

  const isLoading = modulesLoading || fieldLoading || updateMutation.isLoading;

  return (
    <LoadingSpinner loading={isLoading} label="Loading...">
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
          { label: 'Data Collection Form Questions', link: '/admin/configuration/data-collection-form-questions' },
        ]}>
        <PageHeaderContainer>
          <PageTitle>Edit Question</PageTitle>
        </PageHeaderContainer>

        <QuestionForm
          formState={formState}
          errors={errors}
          setErrors={setErrors}
          dispatch={dispatch}
          modules={modules}
          sections={allSections}
          availableFields={availableFields}
          isEdit={true}
        />

        <Box sx={{ mt: 4, display: 'flex', gap: 2 }}>
          <GovUKButton onClick={handleSubmit} isLoading={updateMutation.isLoading}>
            Save Changes
          </GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleCancel}>
            Cancel
          </GovUKButton>
        </Box>
      </SettingsLayout>
    </LoadingSpinner>
  );
};

export default DataCollectionFormQuestionsEdit;