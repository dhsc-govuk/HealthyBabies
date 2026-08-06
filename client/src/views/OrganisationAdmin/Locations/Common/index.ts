import { SiteFormQuestionDto, SiteFormQuestionType, SiteAnswerInputRequest, DateValidationRule } from '../../../../components/Global/SiteForms';
import { ParsedAddress } from './addressLookupQueries';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { evaluateConditionalRules } from '../../../../helpers/conditionalRules';
import { validateDateParts } from '../../../../helpers/validateDate';

// A question is visible when its rich conditional_rules pass (if set), else the legacy single-parent
// conditionalQuestionCode/conditionalValue exact-match. No conditions => always visible.
export const isLocationQuestionVisible = (
  question: SiteFormQuestionDto,
  answers: Record<string, string | undefined>
): boolean => {
  if (question.conditionalRules) {
    return evaluateConditionalRules(question.conditionalRules, answers);
  }
  if (question.conditionalQuestionCode && question.conditionalValue) {
    return answers[question.conditionalQuestionCode] === question.conditionalValue;
  }
  return true;
};

// Predefined question codes
export const PREDEFINED_NAME_CODE = 'FHS01';
export const PREDEFINED_POSTCODE_CODE = 'FHS02';
export const PREDEFINED_REFERENCE_CODE = 'FHS03';
export const PREDEFINED_ADDRESS_LINE1_CODE = 'FHS16';
export const PREDEFINED_ADDRESS_LINE2_CODE = 'FHS13';
export const PREDEFINED_TOWN_OR_CITY_CODE = 'FHS14';
export const PREDEFINED_COUNTY_CODE = 'FHS15';

export interface LocationFormState {
  id?: string;
  answers: Record<string, string | undefined>;
}

export enum LocationReducerAction {
  SET_ANSWER,
  INIT,
  INIT_FROM_LOCATION,
  SET_ADDRESS_FROM_LOOKUP,
}

export const initialLocationState: LocationFormState = {
  answers: {},
};

export const locationReducer = (state: LocationFormState, action: { type: LocationReducerAction; payload?: any }): LocationFormState => {
  switch (action.type) {
    case LocationReducerAction.SET_ANSWER:
      return {
        ...state,
        answers: {
          ...state.answers,
          [action.payload.code]: action.payload.value,
        },
      };
    case LocationReducerAction.INIT:
      return { ...initialLocationState };
    case LocationReducerAction.INIT_FROM_LOCATION:
      return action.payload as LocationFormState;
    case LocationReducerAction.SET_ADDRESS_FROM_LOOKUP: {
      const address = action.payload as ParsedAddress;
      return {
        ...state,
        answers: {
          ...state.answers,
          [PREDEFINED_REFERENCE_CODE]: address.uprn,
          [PREDEFINED_ADDRESS_LINE1_CODE]: address.addressLine1,
          [PREDEFINED_ADDRESS_LINE2_CODE]: address.addressLine2,
          [PREDEFINED_TOWN_OR_CITY_CODE]: address.townOrCity,
          [PREDEFINED_COUNTY_CODE]: address.county,
        },
      };
    }
    default:
      throw new Error();
  }
};

export interface ValidationErrors {
  [questionCode: string]: string | undefined;
}

export const validateLocation = (state: LocationFormState, questions: SiteFormQuestionDto[]): ValidationErrors => {
  const errors: ValidationErrors = {};

  // Validate each active question (including predefined FHS01/02/03)
  for (const question of questions) {
    if (!question.isActive) continue;

    // Skip validation for questions hidden by their conditional rules
    if (!isLocationQuestionVisible(question, state.answers)) {
      continue;
    }

    const value = state.answers[question.code];
    const isEmpty = value === undefined || value === null || value === '';

    if (question.isRequired && isEmpty) {
      errors[question.code] = getErrorMessage(question);
      continue;
    }

    if (!isEmpty && question.questionType === SiteFormQuestionType.Date && question.dateValidationRule) {
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

  return errors;
};

const getErrorMessage = (question: SiteFormQuestionDto): string => {
  switch (question.questionType) {
    case SiteFormQuestionType.Text:
      return `Please enter ${question.label.toLowerCase()}`;
    case SiteFormQuestionType.Radio:
      return `Please select an option for "${question.label}"`;
    case SiteFormQuestionType.Checkbox:
      return `Please select at least one option for "${question.label}"`;
    case SiteFormQuestionType.Select:
      return `Please select an option for "${question.label}"`;
    case SiteFormQuestionType.Date:
      return `Please enter a date for "${question.label}"`;
    default:
      return `Please answer "${question.label}"`;
  }
};

export const buildAnswersForRequest = (state: LocationFormState, questions: SiteFormQuestionDto[]): SiteAnswerInputRequest[] => {
  const answers: SiteAnswerInputRequest[] = [];

  for (const question of questions) {
    if (!question.isActive) continue;

    const value = state.answers[question.code];

    // Clear any previously-saved value for questions hidden by their conditional rules
    if (!isLocationQuestionVisible(question, state.answers)) {
      answers.push({ questionCode: question.code, value: undefined });
      continue;
    }

    answers.push({
      questionCode: question.code,
      value: encodeNullableForWaf(value) ?? undefined,
    });
  }

  return answers;
};
