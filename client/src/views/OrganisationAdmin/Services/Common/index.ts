import { ServiceDto, ServiceFormQuestionDto, ServiceFormQuestionType, AnswerInputRequest, DateValidationRule } from '../../../../components/Global/Services';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { evaluateConditionalRules } from '../../../../helpers/conditionalRules';
import { validateDateParts } from '../../../../helpers/validateDate';

// A question is visible when its rich conditional_rules pass (if set), otherwise it falls back to the
// legacy single-parent conditionalQuestionCode/conditionalValue check. No conditions => always visible.
export const isServiceQuestionVisible = (question: ServiceFormQuestionDto, answers: Record<string, string | undefined>): boolean => {
  if (question.conditionalRules) {
    return evaluateConditionalRules(question.conditionalRules, answers);
  }
  if (question.conditionalQuestionCode && question.conditionalValue) {
    const parentValue = answers[question.conditionalQuestionCode] || '';
    const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());
    const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
    return conditionalValues.some((cv) => parentSelectedValues.includes(cv));
  }
  return true;
};

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

export const serviceReducer = (state: ServiceFormState, action: { type: ServiceReducerAction; payload?: any }): ServiceFormState => {
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

export const shouldSkipServiceCharacteristics = (state: ServiceFormState): boolean => state.answers.SMD03 === 'no_longer_offered';

export const validateStep = (state: ServiceFormState, questions: ServiceFormQuestionDto[]): ValidationErrors => {
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

    // Skip validation for questions hidden by their conditional rules
    if (!isServiceQuestionVisible(question, state.answers)) {
      continue;
    }

    const value = state.answers[question.code];

    if (value === undefined || value === null || value === '') {
      errors[question.code] = getErrorMessage(question);
      continue;
    }

    if (question.questionType === ServiceFormQuestionType.Date && question.dateValidationRule) {
      const [day, month, year] = value.split('/');
      const result = validateDateParts(day, month, year, {
        allowFuture: question.dateValidationRule !== DateValidationRule.NotInFuture,
        disallowPast: question.dateValidationRule === DateValidationRule.NotInPast,
        legendText: question.label,
      });
      if (result.error) {
        errors[question.code] = result.error;
      }
    }
  }

  const ageRangeError = validateAgeRange(state, questions);
  if (ageRangeError) {
    errors.SMD14 = ageRangeError;
  }

  return errors;
};

const validateAgeRange = (state: ServiceFormState, questions: ServiceFormQuestionDto[]): string | undefined => {
  const lowestValue = state.answers.SMD13;
  const highestValue = state.answers.SMD14;

  if (!lowestValue || !highestValue || lowestValue.toLowerCase() === 'unknown' || highestValue.toLowerCase() === 'unknown') {
    return undefined;
  }

  const lowestQuestion = questions.find((q) => q.code === 'SMD13');
  const highestQuestion = questions.find((q) => q.code === 'SMD14');

  const lowestOrder = lowestQuestion?.options.find((o) => o.value.toLowerCase() === lowestValue.toLowerCase())?.displayOrder;
  const highestOrder = highestQuestion?.options.find((o) => o.value.toLowerCase() === highestValue.toLowerCase())?.displayOrder;

  if (lowestOrder === undefined || highestOrder === undefined || !lowestQuestion || !highestQuestion) {
    return undefined;
  }

  if (lowestOrder > highestOrder) {
    return `'${highestQuestion.label}' cannot be lower than '${lowestQuestion.label}'.`;
  }

  return undefined;
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

export const buildAnswersForRequest = (state: ServiceFormState, questions: ServiceFormQuestionDto[]): AnswerInputRequest[] => {
  const answers: AnswerInputRequest[] = [];

  for (const question of questions) {
    // Skip SMD01 (name) as it's handled separately
    if (question.code === 'SMD01') continue;

    const value = state.answers[question.code];

    // Don't include answers for questions hidden by their conditional rules
    if (!isServiceQuestionVisible(question, state.answers)) {
      continue;
    }

    answers.push({
      questionCode: question.code,
      value: encodeNullableForWaf(value) ?? undefined,
    });
  }

  return answers;
};
