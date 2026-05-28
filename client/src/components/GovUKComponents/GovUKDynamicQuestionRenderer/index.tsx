import React from 'react';
import { Details } from 'govuk-react';
import { GovUKFieldset, GovUKRadio, GovUKCheckbox } from '../index';

export interface FormQuestion {
  id: string;
  code: string;
  label: string;
  hint?: string | null;
  helpText?: string | null;
  helpTextSummary?: string | null;
  questionType: string | number;
  isRequired: boolean;
  isActive: boolean;
  displayOrder: number;
  conditionalQuestionCode?: string | null;
  conditionalValue?: string | null;
  options: FormQuestionOption[];
}

export interface FormQuestionOption {
  value: string;
  label: string;
  displayOrder: number;
}

export interface FormState {
  answers: Record<string, string | undefined>;
}

export interface ValidationErrors {
  [key: string]: string | undefined;
}

export interface GovUKDynamicQuestionRendererProps {
  questions: FormQuestion[];
  answers: Record<string, string | undefined>;
  errors: ValidationErrors;
  onAnswerChange: (questionCode: string, value: string) => void;
  placeholderReplacer?: (text: string) => string;
}

interface QuestionRendererProps {
  question: FormQuestion;
  value: string | undefined;
  error: string | undefined;
  onChange: (value: string) => void;
  allQuestions: FormQuestion[];
  answers: Record<string, string | undefined>;
  errors: ValidationErrors;
  onAnswerChange: (questionCode: string, value: string) => void;
  placeholderReplacer: (text: string) => string;
}

const isQuestionVisible = (question: FormQuestion, answers: Record<string, string | undefined>): boolean => {
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

const HelpBlock = ({ question, placeholderReplacer }: { question: FormQuestion; placeholderReplacer: (text: string) => string }): React.ReactElement | null => {
  if (!question.helpTextSummary || !question.helpText) {
    return null;
  }

  const helpTextSummary = placeholderReplacer(question.helpTextSummary);
  const helpText = placeholderReplacer(question.helpText);

  return (
    <div className="govuk-!-margin-bottom-4">
      <Details summary={helpTextSummary}>
        <div dangerouslySetInnerHTML={{ __html: formatHelpText(helpText) }} />
      </Details>
    </div>
  );
};

const QuestionRenderer = ({
  question,
  value,
  error,
  onChange,
  allQuestions,
  answers,
  errors,
  onAnswerChange,
  placeholderReplacer,
}: QuestionRendererProps): React.ReactElement | null => {
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
            value={answers[childQuestion.code]}
            error={errors[childQuestion.code]}
            onChange={(v) => onAnswerChange(childQuestion.code, v)}
            allQuestions={allQuestions}
            answers={answers}
            errors={errors}
            onAnswerChange={onAnswerChange}
            placeholderReplacer={placeholderReplacer}
          />
        ))}
      </>
    );
  };

  const questionLabel = placeholderReplacer(question.label);
  const questionHint = question.hint ? placeholderReplacer(question.hint) : undefined;

  const renderQuestionInput = () => {
    const questionType = String(question.questionType);
    switch (questionType) {
      case 'Text':
      case '0':
        return (
          <GovUKFieldset.Input
            id={question.code.toLowerCase()}
            name={question.code}
            label={questionLabel}
            questionCode={question.code}
            value={value || ''}
            error={error}
            hint={questionHint}
            onChange={(e) => onChange(e.target.value)}
          />
        );

      case 'Radio':
      case '1': {
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
            legend={questionLabel}
            questionCode={question.code}
            options={options}
            value={value}
            error={error}
            hint={questionHint}
            onChange={(v) => onChange(String(v))}
          />
        );
      }

      case 'Checkbox':
      case '2': {
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
            legend={questionLabel}
            questionCode={question.code}
            options={options}
            value={selectedValues}
            error={error}
            hint={questionHint}
            onChange={(values) => onChange(values.join(','))}
          />
        );
      }

      case 'Select':
      case '3': {
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
            label={questionLabel}
            questionCode={question.code}
            value={value || ''}
            error={error}
            hint={questionHint}
            options={options}
            onChange={(e) => onChange(e.target.value)}
          />
        );
      }

      default:
        return null;
    }
  };

  return (
    <>
      <HelpBlock question={question} placeholderReplacer={placeholderReplacer} />
      {renderQuestionInput()}
    </>
  );
};

const GovUKDynamicQuestionRenderer = ({
  questions,
  answers,
  errors,
  onAnswerChange,
  placeholderReplacer = (text) => text,
}: GovUKDynamicQuestionRendererProps): React.ReactElement => {
  const topLevelQuestions = questions.filter((q) => q.isActive && !q.conditionalQuestionCode).sort((a, b) => a.displayOrder - b.displayOrder);

  return (
    <div>
      {topLevelQuestions.map((question, index) => {
        if (!isQuestionVisible(question, answers)) {
          return null;
        }

        return (
          <div key={question.id} className={index > 0 ? 'govuk-!-margin-top-6' : ''}>
            <QuestionRenderer
              question={question}
              value={answers[question.code]}
              error={errors[question.code]}
              onChange={(value) => onAnswerChange(question.code, value)}
              allQuestions={questions}
              answers={answers}
              errors={errors}
              onAnswerChange={onAnswerChange}
              placeholderReplacer={placeholderReplacer}
            />
          </div>
        );
      })}
    </div>
  );
};

export default GovUKDynamicQuestionRenderer;
