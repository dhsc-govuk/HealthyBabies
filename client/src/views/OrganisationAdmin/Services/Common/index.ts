import {
  ServiceDto,
  ServiceFormQuestionDto,
  ServiceFormQuestionType,
  AnswerInputRequest,
} from '../../../../components/Global/Services';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';

export interface ServiceFormState {
  id?: string;
  name: string;
  answers: Record<string, string | undefined>;
}

export enum ServiceReducerAction {
  SET_NAME,
  SET_ANSWER,
  INIT,
  INIT_FROM_SERVICE,
}

export const initialServiceState: ServiceFormState = {
  name: '',
  answers: {},
};

export const serviceReducer = (
  state: ServiceFormState,
  action: { type: ServiceReducerAction; payload?: any }
): ServiceFormState => {
  switch (action.type) {
    case ServiceReducerAction.SET_NAME:
      return { ...state, name: action.payload };
    case ServiceReducerAction.SET_ANSWER:
      return {
        ...state,
        answers: {
          ...state.answers,
          [action.payload.code]: action.payload.value,
        },
      };
    case ServiceReducerAction.INIT:
      return { ...initialServiceState };
    case ServiceReducerAction.INIT_FROM_SERVICE:
      return action.payload as ServiceFormState;
    default:
      throw new Error();
  }
};

export interface ValidationErrors {
  name?: string;
  [questionCode: string]: string | undefined;
}

export const shouldSkipServiceCharacteristics = (state: ServiceFormState): boolean =>
  state.answers.SMD03 === 'no_longer_offered';

export const validateStep = (
  state: ServiceFormState,
  questions: ServiceFormQuestionDto[]
): ValidationErrors => {
  const errors: ValidationErrors = {};

  // Validate name (always required, always first in step 1)
  if (!state.name || state.name.trim() === '') {
    errors.name = 'Please provide a service name';
  }

  // Validate each required question
  for (const question of questions) {
    if (!question.isRequired || !question.isActive) continue;

    // Skip SMD01 (name) as it's handled separately
    if (question.code === 'SMD01') continue;

    // Check if conditional question should be visible
    if (question.conditionalQuestionCode && question.conditionalValue) {
      const parentValue = state.answers[question.conditionalQuestionCode] || '';
      const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());
      const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
      const isVisible = conditionalValues.some((cv) => parentSelectedValues.includes(cv));
      if (!isVisible) {
        // Question is not visible, skip validation
        continue;
      }
    }

    const value = state.answers[question.code];

    if (value === undefined || value === null || value === '') {
      errors[question.code] = getErrorMessage(question);
    }
  }

  return errors;
};

const getErrorMessage = (question: ServiceFormQuestionDto): string => {
  switch (question.questionType) {
    case ServiceFormQuestionType.Text:
      return `Please enter ${question.label.toLowerCase()}`;
    case ServiceFormQuestionType.Radio:
      return `Please select an option for "${question.label}"`;
    case ServiceFormQuestionType.Checkbox:
      return `Please select at least one option for "${question.label}"`;
    case ServiceFormQuestionType.Select:
      return `Please select an option for "${question.label}"`;
    case ServiceFormQuestionType.Date:
      return `Please enter a date for "${question.label}"`;
    default:
      return `Please answer "${question.label}"`;
  }
};

export const mapServiceToFormState = (service: ServiceDto): ServiceFormState => {
  const answers: Record<string, string | undefined> = {};

  for (const answer of service.answers) {
    answers[answer.questionCode] = answer.value ?? undefined;
  }

  return {
    id: service.id,
    name: service.name,
    answers,
  };
};

export const buildAnswersForRequest = (
  state: ServiceFormState,
  questions: ServiceFormQuestionDto[]
): AnswerInputRequest[] => {
  const answers: AnswerInputRequest[] = [];

  for (const question of questions) {
    // Skip SMD01 (name) as it's handled separately
    if (question.code === 'SMD01') continue;

    const value = state.answers[question.code];

    // Check if conditional question should be included
    if (question.conditionalQuestionCode && question.conditionalValue) {
      const parentValue = state.answers[question.conditionalQuestionCode] || '';
      const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());
      const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
      const isVisible = conditionalValues.some((cv) => parentSelectedValues.includes(cv));
      if (!isVisible) {
        // Question is not visible, don't include in request
        continue;
      }
    }

    answers.push({
      questionCode: question.code,
      value: encodeNullableForWaf(value) ?? undefined,
    });
  }

  return answers;
};