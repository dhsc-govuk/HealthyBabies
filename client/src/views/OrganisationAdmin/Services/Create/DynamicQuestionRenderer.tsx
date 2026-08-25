import React, { useMemo } from 'react';
import { Box } from '@mui/material';
import { Details } from 'govuk-react';
import { GovUKFieldset, GovUKRadio, GovUKCheckbox, GovUKDateField } from '../../../../components/GovUKComponents';
import { ServiceFormQuestionDto, ServiceFormQuestionType, DateValidationRule } from '../../../../components/Global/Services';
import { ServiceFormState, ServiceReducerAction, ValidationErrors, getEffectiveServiceAnswers, isServiceQuestionVisible } from '../Common';

interface DynamicQuestionRendererProps {
  /** The questions to render — a single step's slice. */
  questions: ServiceFormQuestionDto[];
  /** The whole form. Only used to prune answers, since a conditional parent may live on another step. */
  allFormQuestions: ServiceFormQuestionDto[];
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
  effectiveAnswers: Record<string, string | undefined>;
}

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

const QuestionRenderer = ({ question, value, error, onChange, allQuestions, state, dispatch, errors, effectiveAnswers }: QuestionRendererProps): React.ReactElement | null => {
  // Find conditional child questions for this question
  const conditionalChildren = allQuestions.filter((q) => q.conditionalQuestionCode === question.code);

  const renderConditionalContent = (triggerValue: string): React.ReactNode => {
    const matches = conditionalChildren.filter((q) => {
      if (!q.conditionalValue) return false;
      if (!q.isActive || !isServiceQuestionVisible(q, effectiveAnswers)) return false;
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
            effectiveAnswers={effectiveAnswers}
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
          const parts = stored
            .split(',')
            .map((v) => v.trim())
            .filter(Boolean);
          const result: string[] = [];
          let i = 0;
          while (i < parts.length) {
            let matched = false;
            for (let j = parts.length; j > i; j--) {
              const candidate = parts.slice(i, j).join(', ');
              const matchedOption = options.find((o) => o.value.trim() === candidate || o.label.trim() === candidate);
              if (matchedOption) {
                result.push(matchedOption.value); // original value preserves trailing space
                i = j;
                matched = true;
                break;
              }
            }
            if (!matched) {
              result.push(parts[i]);
              i++;
            }
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
            allowFuture={question.dateValidationRule === DateValidationRule.NotInPast}
            disallowPast={question.dateValidationRule === DateValidationRule.NotInPast ? true : undefined}
            onChange={onChange}
          />
        );

      default:
        return null;
    }
  };

  // Radios reveal their children inline via the option's `conditional` above; every other parent type
  // renders them as sibling blocks instead. Doing this here rather than in the top-level loop means it
  // also fires for a parent that is itself a child — without which SMD10a, whose parent SMD10 is
  // nested under SMD08, could never be rendered at all. Selects are included because SMD10 is a Select
  // in the code seeder and a Checkbox in the exported schema, and SMD10a has to reach the page either way.
  const siblingChildren =
    question.questionType === ServiceFormQuestionType.Radio
      ? []
      : conditionalChildren.filter((child) => child.isActive && isServiceQuestionVisible(child, effectiveAnswers));

  return (
    <>
      <HelpBlock question={question} />
      {renderQuestionInput()}
      {siblingChildren.map((childQuestion) => (
        <Box key={childQuestion.id} sx={{ mt: 3 }}>
          <QuestionRenderer
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
            effectiveAnswers={effectiveAnswers}
          />
        </Box>
      ))}
    </>
  );
};

const DynamicQuestionRenderer = ({ questions, allFormQuestions, state, dispatch, errors }: DynamicQuestionRendererProps): React.ReactElement => {
  // Visibility is decided against the pruned answers so a question whose ancestor has changed stops
  // showing even while its own stale answer is still held in state (BLUM-7221). Memoised because this
  // component re-renders on every keystroke.
  const effectiveAnswers = useMemo(() => getEffectiveServiceAnswers(allFormQuestions, state.answers), [allFormQuestions, state.answers]);

  // Filter to only top-level questions: questions with no conditional parent, or whose conditional
  // parent lives in a different step. Conditionals whose parent is in this step are rendered by that
  // parent's QuestionRenderer — inline for radios, as sibling blocks for every other parent type.
  const topLevelQuestions = questions
    .filter((q) => q.isActive && q.code !== 'SMD01' && (!q.conditionalQuestionCode || !questions.some((p) => p.code === q.conditionalQuestionCode)))
    .sort((a, b) => a.displayOrder - b.displayOrder);

  const renderQuestion = (question: ServiceFormQuestionDto, index: number) => {
    // Check if question should be visible based on conditional logic
    if (!isServiceQuestionVisible(question, effectiveAnswers)) {
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
          effectiveAnswers={effectiveAnswers}
        />
      </Box>
    );
  };

  return <Box>{topLevelQuestions.map((question, index) => renderQuestion(question, index))}</Box>;
};

export default DynamicQuestionRenderer;
