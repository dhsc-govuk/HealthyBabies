import React from 'react';
import { Box } from '@mui/material';
import { Details } from 'govuk-react';
import { GovUKFieldset, GovUKRadio, GovUKCheckbox, GovUKDateField } from '../../../../components/GovUKComponents';
import { ServiceFormQuestionDto, ServiceFormQuestionType } from '../../../../components/Global/Services';
import { ServiceFormState, ServiceReducerAction, ValidationErrors } from '../Common';

interface DynamicQuestionRendererProps {
  questions: ServiceFormQuestionDto[];
  state: ServiceFormState;
  dispatch: React.Dispatch<{ type: ServiceReducerAction; payload?: any }>;
  errors: ValidationErrors;
}

interface QuestionRendererProps {
  question: ServiceFormQuestionDto;
  value: string | undefined;
  error: string | undefined;
  onChange: (value: string) => void;
  allQuestions: ServiceFormQuestionDto[];
  state: ServiceFormState;
  dispatch: React.Dispatch<{ type: ServiceReducerAction; payload?: any }>;
  errors: ValidationErrors;
}

const isQuestionVisible = (question: ServiceFormQuestionDto, answers: Record<string, string | undefined>): boolean => {
  if (!question.conditionalQuestionCode || !question.conditionalValue) {
    return true;
  }
  const parentValue = answers[question.conditionalQuestionCode] || '';
  const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());

  // For checkbox parent questions, check if any of the selected values match any conditional value
  const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
  return conditionalValues.some((cv) => parentSelectedValues.includes(cv));
};

// Helper function to convert markdown-like formatting to HTML
const formatHelpText = (text: string): string => {
  return text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\n\n/g, '</p><p>')
    .replace(/^/, '<p>')
    .replace(/$/, '</p>');
};

// Render help block if question has help text
const HelpBlock = ({ question }: { question: ServiceFormQuestionDto }): React.ReactElement | null => {
  if (!question.helpTextSummary || !question.helpText) {
    return null;
  }

  return (
    <Box sx={{ mb: 2 }}>
      <Details summary={question.helpTextSummary}>
        <div dangerouslySetInnerHTML={{ __html: formatHelpText(question.helpText) }} />
      </Details>
    </Box>
  );
};

const QuestionRenderer = ({ question, value, error, onChange, allQuestions, state, dispatch, errors }: QuestionRendererProps): React.ReactElement | null => {
  // Find conditional child questions for this question
  const conditionalChildren = allQuestions.filter((q) => q.conditionalQuestionCode === question.code);

  const renderConditionalContent = (triggerValue: string): React.ReactNode => {
    const matches = conditionalChildren.filter((q) => {
      if (!q.conditionalValue) return false;
      return q.conditionalValue
        .split(',')
        .map((v) => v.trim())
        .includes(triggerValue);
    });

    if (matches.length === 0) return undefined;

    return (
      <>
        {matches.map((childQuestion) => (
          <QuestionRenderer
            key={childQuestion.id}
            question={childQuestion}
            value={state.answers[childQuestion.code]}
            error={errors[childQuestion.code]}
            onChange={(v) =>
              dispatch({
                type: ServiceReducerAction.SET_ANSWER,
                payload: { code: childQuestion.code, value: v },
              })
            }
            allQuestions={allQuestions}
            state={state}
            dispatch={dispatch}
            errors={errors}
          />
        ))}
      </>
    );
  };

  const renderQuestionInput = () => {
    switch (question.questionType) {
      case ServiceFormQuestionType.Text:
        return (
          <GovUKFieldset.Input
            id={question.code.toLowerCase()}
            name={question.code}
            label={question.label}
            questionCode={question.code}
            value={value || ''}
            error={error}
            hint={question.hint}
            onChange={(e) => onChange(e.target.value)}
          />
        );

      case ServiceFormQuestionType.Radio: {
        const options = question.options
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((opt) => ({
            value: opt.value,
            label: opt.label,
            conditional: renderConditionalContent(opt.value),
          }));

        return (
          <GovUKRadio
            id={question.code.toLowerCase()}
            name={question.code}
            legend={question.label}
            questionCode={question.code}
            options={options}
            value={value}
            error={error}
            hint={question.hint}
            onChange={(v) => onChange(String(v))}
          />
        );
      }

      case ServiceFormQuestionType.Checkbox: {
        const options = question.options
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((opt) => ({
            value: opt.value,
            label: opt.label,
          }));

        // Checkbox values are stored as comma-separated string.
        // Greedy parsing handles two classes of bad DB data:
        //   1. Option values containing commas  (e.g. "Strengthening Families, Strengthening Communities")
        //   2. Option values with trailing spaces (e.g. "As needed ", "A telephone helpline ")
        // For each match we push the *original* option.value so GovUKCheckbox can
        // resolve the checked state via selectedValues.includes(option.value).
        const parseSelectedValues = (stored: string): string[] => {
          if (!stored.trim()) return [];
          const parts = stored.split(',').map((v) => v.trim()).filter(Boolean);
          const result: string[] = [];
          let i = 0;
          while (i < parts.length) {
            let matched = false;
            for (let j = parts.length; j > i; j--) {
              const candidate = parts.slice(i, j).join(', ');
              const matchedOption = options.find(
                (o) => o.value.trim() === candidate || o.label.trim() === candidate
              );
              if (matchedOption) {
                result.push(matchedOption.value); // original value preserves trailing space
                i = j;
                matched = true;
                break;
              }
            }
            if (!matched) { result.push(parts[i]); i++; }
          }
          return result;
        };
        const selectedValues = value ? parseSelectedValues(value) : [];

        return (
          <GovUKCheckbox
            id={question.code.toLowerCase()}
            name={question.code}
            legend={question.label}
            questionCode={question.code}
            options={options}
            value={selectedValues}
            error={error}
            hint={question.hint}
            onChange={(values) => onChange(values.join(','))}
          />
        );
      }

      case ServiceFormQuestionType.Select: {
        const options = [
          { value: '', label: 'Select an option...' },
          ...question.options
            .sort((a, b) => a.displayOrder - b.displayOrder)
            .map((opt) => ({
              value: opt.value,
              label: opt.label,
            })),
        ];

        return (
          <GovUKFieldset.Select
            id={question.code.toLowerCase()}
            name={question.code}
            label={question.label}
            questionCode={question.code}
            value={value || ''}
            error={error}
            hint={question.hint}
            options={options}
            onChange={(e) => onChange(e.target.value)}
          />
        );
      }

      case ServiceFormQuestionType.Date:
        return (
          <GovUKDateField
            id={question.code.toLowerCase()}
            legend={question.label}
            questionCode={question.code}
            value={value || ''}
            error={error}
            hint={question.hint}
            onChange={onChange}
          />
        );

      default:
        return null;
    }
  };

  return (
    <>
      <HelpBlock question={question} />
      {renderQuestionInput()}
    </>
  );
};

const DynamicQuestionRenderer = ({ questions, state, dispatch, errors }: DynamicQuestionRendererProps): React.ReactElement => {
  // Find the parent question type for conditional questions
  const getParentQuestionType = (conditionalQuestionCode: string | null | undefined) => {
    if (!conditionalQuestionCode) return null;
    const parent = questions.find((q) => q.code === conditionalQuestionCode);
    return parent?.questionType;
  };

  // Filter to only top-level questions: questions with no conditional parent, or whose conditional
  // parent lives in a different step (cross-step conditionals like SMD17 depending on SMD03).
  // In-step radio/select conditionals are rendered inline by QuestionRenderer.renderConditionalContent.
  // In-step checkbox conditionals are handled separately via checkboxConditionalQuestions below.
  const topLevelQuestions = questions
    .filter(
      (q) =>
        q.isActive &&
        q.code !== 'SMD01' &&
        (!q.conditionalQuestionCode || !questions.some((p) => p.code === q.conditionalQuestionCode))
    )
    .sort((a, b) => a.displayOrder - b.displayOrder);

  // Find checkbox-conditional questions (like SMD19 depending on SMD17 which is a checkbox)
  // These are questions whose parent is a checkbox type
  const checkboxConditionalQuestions = questions.filter(
    (q) => q.isActive && q.conditionalQuestionCode && getParentQuestionType(q.conditionalQuestionCode) === ServiceFormQuestionType.Checkbox
  );

  const renderQuestion = (question: ServiceFormQuestionDto, index: number) => {
    // Check if question should be visible based on conditional logic
    if (!isQuestionVisible(question, state.answers)) {
      return null;
    }

    return (
      <Box key={question.id} sx={{ mt: index > 0 ? 3 : 0 }}>
        <QuestionRenderer
          question={question}
          value={state.answers[question.code]}
          error={errors[question.code]}
          onChange={(value) =>
            dispatch({
              type: ServiceReducerAction.SET_ANSWER,
              payload: { code: question.code, value },
            })
          }
          allQuestions={questions}
          state={state}
          dispatch={dispatch}
          errors={errors}
        />
      </Box>
    );
  };

  return (
    <Box>
      {topLevelQuestions.map((question, index) => {
        const elements: React.ReactNode[] = [];

        // Render the main question
        const mainQuestion = renderQuestion(question, index);
        if (mainQuestion) {
          elements.push(mainQuestion);
        }

        // After rendering a checkbox question, render all visible conditional children
        const conditionalChildren = checkboxConditionalQuestions.filter((cq) => cq.conditionalQuestionCode === question.code);
        conditionalChildren.forEach((child, childIndex) => {
          if (isQuestionVisible(child, state.answers)) {
            const childQuestion = renderQuestion(child, index + childIndex + 1);
            if (childQuestion) {
              elements.push(childQuestion);
            }
          }
        });

        return <React.Fragment key={question.id}>{elements}</React.Fragment>;
      })}
    </Box>
  );
};

export default DynamicQuestionRenderer;
