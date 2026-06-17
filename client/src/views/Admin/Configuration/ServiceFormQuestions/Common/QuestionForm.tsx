import React, { useCallback, useMemo } from 'react';
import { Box, FormControlLabel, Checkbox } from '@mui/material';
import { GovUKFieldset, ErrorSummary } from '../../../../../components/GovUKComponents';
import {
  QuestionFormState,
  ValidationErrors,
  validationMessages,
  questionTypeOptions,
  stepOptions,
  requiresOptions,
  ServiceFormQuestion,
} from './types';
import { QuestionFormAction } from './reducer';
import OptionsManager from './OptionsManager';

interface Props {
  formState: QuestionFormState;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<any>;
  isEdit?: boolean;
  allQuestions?: ServiceFormQuestion[];
}

const QuestionForm = ({
  formState,
  errors,
  setErrors,
  dispatch,
  isEdit = false,
  allQuestions = [],
}: Props): React.ReactElement => {
  const questionTypeSelectOptions = useMemo(
    () => [
      { value: '', label: 'Select question type' },
      ...questionTypeOptions.map((opt) => ({ value: String(opt.value), label: opt.label })),
    ],
    []
  );

  const stepSelectOptions = useMemo(
    () => stepOptions.map((opt) => ({ value: String(opt.value), label: opt.label })),
    []
  );

  const conditionalQuestionOptions = useMemo(() => {
    const options = [{ value: '', label: 'None (always show)' }];
    allQuestions
      .filter((q) => q.code !== formState.code && requiresOptions(q.questionType))
      .forEach((q) => {
        options.push({ value: q.code, label: `${q.code} - ${q.label}` });
      });
    return options;
  }, [allQuestions, formState.code]);

  const conditionalValueOptions = useMemo(() => {
    const options = [{ value: '', label: 'Select a value' }];
    if (formState.conditionalQuestionCode) {
      const parentQuestion = allQuestions.find((q) => q.code === formState.conditionalQuestionCode);
      if (parentQuestion) {
        parentQuestion.options.forEach((opt) => {
          options.push({ value: opt.value, label: opt.label });
        });
      }
    }
    return options;
  }, [allQuestions, formState.conditionalQuestionCode]);

  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'code') {
        if (!formState.code) {
          errorItems.code = validationMessages.code;
        } else if (!/^[A-Za-z0-9]+$/.test(formState.code) || formState.code.length > 10) {
          errorItems.code = validationMessages.codeFormat;
        } else {
          errorItems.code = false;
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
    if (errors.code) list.push({ targetName: 'question-code', text: String(errors.code) });
    if (errors.label) list.push({ targetName: 'question-label', text: String(errors.label) });
    if (errors.options) list.push({ targetName: 'question-options', text: String(errors.options) });
    return list;
  }, [errors]);

  const showOptions = requiresOptions(formState.questionType);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="Question Details" legendSize="m">
        <GovUKFieldset.Input
          id="question-code"
          name="code"
          label="Code"
          hint="A unique identifier for this question (alphanumeric, max 10 characters)"
          value={formState.code}
          error={errors.code ? String(errors.code) : undefined}
          required
          disabled={isEdit}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_CODE, value: e.target.value.toUpperCase() })}
          onBlur={() => validate('code')}
        />
        <GovUKFieldset.Input
          id="question-label"
          name="label"
          label="Label"
          hint="The question text shown to users"
          value={formState.label}
          error={errors.label ? String(errors.label) : undefined}
          required
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_LABEL, value: e.target.value })}
          onBlur={() => validate('label')}
        />
        <GovUKFieldset.Input
          id="question-hint"
          name="hint"
          label="Hint"
          hint="Optional hint text shown below the question"
          value={formState.hint}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_HINT, value: e.target.value })}
        />
        <GovUKFieldset.Input
          id="question-placeholder"
          name="placeholder"
          label="Placeholder"
          hint="Optional placeholder text for text inputs"
          value={formState.placeholder}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_PLACEHOLDER, value: e.target.value })}
        />
        <GovUKFieldset.Select
          id="question-type"
          name="questionType"
          label="Question Type"
          hint="The type of input for this question"
          value={String(formState.questionType)}
          options={questionTypeSelectOptions}
          required
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_QUESTION_TYPE, value: Number(e.target.value) })}
        />
        <GovUKFieldset.Select
          id="question-step"
          name="step"
          label="Step"
          hint="Which step of the form this question appears on"
          value={String(formState.step)}
          options={stepSelectOptions}
          required
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_STEP, value: Number(e.target.value) })}
        />
        <GovUKFieldset.Input
          id="question-help-text-summary"
          name="helpTextSummary"
          label="Help Text Summary"
          hint="The clickable link text for expandable help (e.g., 'Help with services')"
          value={formState.helpTextSummary}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_HELP_TEXT_SUMMARY, value: e.target.value })}
        />
        <GovUKFieldset.Input
          id="question-help-text"
          name="helpText"
          label="Help Text"
          hint="Optional detailed help text (supports **bold** markdown)"
          value={formState.helpText}
          onChange={(e) => dispatch({ type: QuestionFormAction.SET_HELP_TEXT, value: e.target.value })}
        />
      </GovUKFieldset>

      <GovUKFieldset legend="Conditional Display" legendSize="m">
        <GovUKFieldset.Select
          id="conditional-question"
          name="conditionalQuestionCode"
          label="Show when question"
          hint="Optionally show this question only when another question has a specific answer"
          value={formState.conditionalQuestionCode}
          options={conditionalQuestionOptions}
          onChange={(e) =>
            dispatch({ type: QuestionFormAction.SET_CONDITIONAL_QUESTION_CODE, value: e.target.value })
          }
        />
        {formState.conditionalQuestionCode && (
          <GovUKFieldset.Select
            id="conditional-value"
            name="conditionalValue"
            label="Has value"
            hint="Select the value that triggers this question to show"
            value={formState.conditionalValue}
            options={conditionalValueOptions}
            onChange={(e) => dispatch({ type: QuestionFormAction.SET_CONDITIONAL_VALUE, value: e.target.value })}
          />
        )}
      </GovUKFieldset>

      <GovUKFieldset legend="Settings" legendSize="m">
        <Box sx={{ mb: 2 }}>
          <FormControlLabel
            control={
              <Checkbox
                checked={formState.isRequired}
                onChange={(e) => dispatch({ type: QuestionFormAction.SET_IS_REQUIRED, value: e.target.checked })}
              />
            }
            label="Required"
          />
        </Box>
        {showOptions && (
          <Box sx={{ mb: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={formState.isPredefined}
                  onChange={(e) => dispatch({ type: QuestionFormAction.SET_IS_PREDEFINED, value: e.target.checked })}
                />
              }
              label="Predefined options"
            />
            <Box sx={{ ml: 4, color: 'text.secondary', fontSize: '0.875rem' }}>
              Uncheck this if options are loaded dynamically (e.g., from locations or services)
            </Box>
          </Box>
        )}
        {showOptions && formState.isPredefined && (
          <GovUKFieldset legend="Answer Options" legendSize="m">
            <OptionsManager options={formState.options} dispatch={dispatch} error={errors.options} />
          </GovUKFieldset>
        )}
        {isEdit && (
          <Box sx={{ mb: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={formState.isActive}
                  onChange={(e) => dispatch({ type: QuestionFormAction.SET_IS_ACTIVE, value: e.target.checked })}
                />
              }
              label="Active"
            />
          </Box>
        )}
      </GovUKFieldset>
    </>
  );
};

export default QuestionForm;
