import React from 'react';
import { Box } from '@mui/material';
import { Details } from 'govuk-react';
import { GovUKFieldset, GovUKRadio, GovUKCheckbox, GovUKDateField } from '../../../../components/GovUKComponents';
import { SiteFormQuestionDto, SiteFormQuestionType } from '../../../../components/Global/SiteForms';
import { LocationFormState, LocationReducerAction, ValidationErrors, PREDEFINED_POSTCODE_CODE } from '../Common';
import AddressLookup from '../Common/AddressLookup';

interface DynamicQuestionRendererProps {
  questions: SiteFormQuestionDto[];
  state: LocationFormState;
  dispatch: React.Dispatch<{ type: LocationReducerAction; payload?: any }>;
  errors: ValidationErrors;
}

interface QuestionRendererProps {
  question: SiteFormQuestionDto;
  value: string | undefined;
  error: string | undefined;
  onChange: (value: string) => void;
  allQuestions: SiteFormQuestionDto[];
  state: LocationFormState;
  dispatch: React.Dispatch<{ type: LocationReducerAction; payload?: any }>;
  errors: ValidationErrors;
}

const isQuestionVisible = (question: SiteFormQuestionDto, answers: Record<string, string | undefined>): boolean => {
  if (!question.conditionalQuestionCode || !question.conditionalValue) {
    return true;
  }
  const parentValue = answers[question.conditionalQuestionCode];
  return parentValue === question.conditionalValue;
};

const formatHelpText = (text: string): string => {
  return text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\n\n/g, '</p><p>')
    .replace(/^/, '<p>')
    .replace(/$/, '</p>');
};

const HelpBlock = ({ question }: { question: SiteFormQuestionDto }): React.ReactElement | null => {
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
                type: LocationReducerAction.SET_ANSWER,
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
      case SiteFormQuestionType.Text:
        if (question.code === PREDEFINED_POSTCODE_CODE) {
          return (
            <>
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
              <AddressLookup
                postcode={value || ''}
                onAddressSelected={(address) => {
                  dispatch({
                    type: LocationReducerAction.SET_ADDRESS_FROM_LOOKUP,
                    payload: address,
                  });
                }}
              />
            </>
          );
        }
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

      case SiteFormQuestionType.Radio: {
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

      case SiteFormQuestionType.Checkbox: {
        const options = question.options
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((opt) => ({
            value: opt.value,
            label: opt.label,
          }));

        const selectedValues = value ? value.split(',') : [];

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

      case SiteFormQuestionType.Select: {
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

      case SiteFormQuestionType.Date:
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
  // Filter to only top-level questions (those without conditional parent)
  // Now includes predefined fields (FHS01, FHS02, FHS03) as they are rendered dynamically
  const topLevelQuestions = questions.filter((q) => q.isActive && !q.conditionalQuestionCode).sort((a, b) => a.displayOrder - b.displayOrder);

  return (
    <Box>
      {topLevelQuestions.map((question, index) => {
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
                  type: LocationReducerAction.SET_ANSWER,
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
      })}
    </Box>
  );
};

export default DynamicQuestionRenderer;
