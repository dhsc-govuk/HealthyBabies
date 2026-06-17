import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset, ErrorSummary } from '../../../../../components/GovUKComponents';
import OptionsManager from './OptionsManager';
import ConditionalRulesBuilder from './ConditionalRulesBuilder';
import { QuestionFormAction } from './reducer';
import {
  fieldTypeOptions,
  FormModule,
  FormSection,
  QuestionFormState,
  requiresOptions,
  ValidationErrors,
  validationMessages,
} from './types';

import { FormField } from './types';

interface Props {
  formState: QuestionFormState;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<any>;
  modules: FormModule[];
  sections: FormSection[];
  availableFields?: FormField[];
  isEdit?: boolean;
}

const QuestionForm = ({
  formState,
  errors,
  setErrors,
  dispatch,
  modules,
  sections,
  availableFields = [],
  isEdit = false,
}: Props): React.ReactElement => {
  const filteredSections = useMemo(
    () => sections.filter((s) => s.formModuleId === formState.formModuleId),
    [sections, formState.formModuleId]
  );

  const moduleSelectOptions = useMemo(
    () => [
      { value: '', label: 'Select a module' },
      ...modules.map((m) => ({ value: m.id, label: m.name })),
    ],
    [modules]
  );

  const sectionSelectOptions = useMemo(
    () => [
      { value: '', label: 'None' },
      ...filteredSections.map((s) => ({ value: s.id, label: `${s.sectionNumber}. ${s.title}` })),
    ],
    [filteredSections]
  );

  const fieldTypeSelectOptions = useMemo(
    () => [
      { value: '', label: 'Select field type' },
      ...fieldTypeOptions.map((opt) => ({ value: opt.value, label: opt.label })),
    ],
    []
  );

  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'fieldKey') {
        if (!formState.fieldKey) {
          errorItems.fieldKey = validationMessages.fieldKey;
        } else if (!/^[A-Za-z0-9_]+$/.test(formState.fieldKey) || formState.fieldKey.length > 20) {
          errorItems.fieldKey = validationMessages.fieldKeyFormat;
        } else {
          errorItems.fieldKey = false;
        }
      }
      if (field === 'label') {
        errorItems.label = !formState.label ? validationMessages.label : false;
      }
      setErrors((prev) => ({ ...prev, ...errorItems }));
    },
    [formState, setErrors]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.fieldKey) list.push({ targetName: 'field-key', text: String(errors.fieldKey) });
    if (errors.label) list.push({ targetName: 'field-label', text: String(errors.label) });
    if (errors.options) list.push({ targetName: 'field-options', text: String(errors.options) });
    return list;
  }, [errors]);

  const showOptions = requiresOptions(formState.fieldType);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="Question Details" legendSize="m">
        <GovUKFieldset.Select
          id="form-module"
          name="formModuleId"
          label="Form Module"
          hint="Select the form module this question belongs to"
          value={formState.formModuleId}
          options={moduleSelectOptions}
          required
          disabled={isEdit}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_FORM_MODULE_ID, value: e.target.value })}
        />
        <GovUKFieldset.Select
          id="form-section"
          name="formSectionId"
          label="Section"
          hint="Select the section within the module"
          value={formState.formSectionId}
          options={sectionSelectOptions}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_FORM_SECTION_ID, value: e.target.value })}
        />
        <GovUKFieldset.Input
          id="field-key"
          name="fieldKey"
          label="Field Key"
          hint="A unique identifier for this field (alphanumeric and underscores, max 20 characters)"
          value={formState.fieldKey}
          error={errors.fieldKey ? String(errors.fieldKey) : undefined}
          required
          disabled={isEdit}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_FIELD_KEY, value: e.target.value.toUpperCase() })}
          onBlur={() => validate('fieldKey')}
        />
        <GovUKFieldset.Input
          id="field-label"
          name="label"
          label="Label"
          hint="The question text shown to users"
          value={formState.label}
          error={errors.label ? String(errors.label) : undefined}
          required
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_LABEL, value: e.target.value })}
          onBlur={() => validate('label')}
        />
        <GovUKFieldset.Select
          id="field-type"
          name="fieldType"
          label="Field Type"
          hint="The type of input for this field"
          value={formState.fieldType}
          options={fieldTypeSelectOptions}
          required
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_FIELD_TYPE, value: e.target.value })}
        />
      </GovUKFieldset>

      <GovUKFieldset legend="Additional Settings" legendSize="m">
        <GovUKFieldset.Input
          id="field-placeholder"
          name="placeholder"
          label="Placeholder"
          hint="Optional placeholder text for text inputs"
          value={formState.placeholder}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_PLACEHOLDER, value: e.target.value })}
        />
        <GovUKFieldset.Input
          id="field-help-text"
          name="helpText"
          label="Help Text"
          hint="Optional help text shown below the question"
          value={formState.helpText}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_HELP_TEXT, value: e.target.value })}
        />
        <GovUKFieldset.Input
          id="field-default-value"
          name="defaultValue"
          label="Default Value"
          hint="Pre-filled value for this field"
          value={formState.defaultValue}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_DEFAULT_VALUE, value: e.target.value })}
        />
      </GovUKFieldset>

      <ConditionalRulesBuilder
        value={formState.conditionalRules}
        onChange={(value) => dispatch({ type: QuestionFormAction.SET_CONDITIONAL_RULES, value })}
        availableFields={availableFields}
        currentFieldKey={formState.fieldKey}
        configuration={formState.configuration}
        onConfigurationChange={(value) => dispatch({ type: QuestionFormAction.SET_CONFIGURATION, value })}
      />

      <GovUKFieldset legend="Settings" legendSize="m">
        <GovUKFieldset.Checkbox
          id="field-is-required"
          name="isRequired"
          label="Required"
          checked={formState.isRequired}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_IS_REQUIRED, value: e.target.checked })}
        />
        {isEdit && (
          <GovUKFieldset.Checkbox
            id="field-is-active"
            name="isActive"
            label="Active"
            checked={formState.isActive}
            onChange={(e) => dispatch({ type: QuestionFormAction.SET_IS_ACTIVE, value: e.target.checked })}
          />
        )}
      </GovUKFieldset>

      {showOptions && (
        <GovUKFieldset legend="Answer Options" legendSize="m">
          <OptionsManager options={formState.options} dispatch={dispatch} error={errors.options} />
        </GovUKFieldset>
      )}
    </>
  );
};

export default QuestionForm;
