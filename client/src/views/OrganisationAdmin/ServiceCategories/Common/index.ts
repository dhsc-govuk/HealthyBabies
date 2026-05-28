import {
  ServiceCategoryFormQuestionDto,
  ServiceCategoryDto,
  ServiceCategoryAnswerInputRequest,
} from '../../../../components/Global/ServiceCategories';

export interface ServiceCategoryFormState {
  name: string;
  categoryCode: string;
  answers: Record<string, string | undefined>;
}

export const initialServiceCategoryState: ServiceCategoryFormState = {
  name: '',
  categoryCode: '',
  answers: {},
};

export enum ServiceCategoryReducerAction {
  SET_NAME = 'SET_NAME',
  SET_CATEGORY_CODE = 'SET_CATEGORY_CODE',
  SET_ANSWER = 'SET_ANSWER',
  INIT_FROM_SERVICE_CATEGORY = 'INIT_FROM_SERVICE_CATEGORY',
  RESET = 'RESET',
}

interface ServiceCategoryAction {
  type: ServiceCategoryReducerAction;
  payload?: any;
}

export const serviceCategoryReducer = (
  state: ServiceCategoryFormState,
  action: ServiceCategoryAction
): ServiceCategoryFormState => {
  switch (action.type) {
    case ServiceCategoryReducerAction.SET_NAME:
      return { ...state, name: action.payload };
    case ServiceCategoryReducerAction.SET_CATEGORY_CODE:
      return { ...state, categoryCode: action.payload };
    case ServiceCategoryReducerAction.SET_ANSWER:
      return {
        ...state,
        answers: {
          ...state.answers,
          [action.payload.code]: action.payload.value,
        },
      };
    case ServiceCategoryReducerAction.INIT_FROM_SERVICE_CATEGORY:
      return action.payload;
    case ServiceCategoryReducerAction.RESET:
      return initialServiceCategoryState;
    default:
      return state;
  }
};

export type ValidationErrors = Record<string, string | undefined>;

export const validateStep = (
  state: ServiceCategoryFormState,
  questions: ServiceCategoryFormQuestionDto[]
): ValidationErrors => {
  const errors: ValidationErrors = {};

  const visibleQuestions = questions.filter((q) => {
    if (!q.conditionalQuestionCode) return true;
    const parentValue = state.answers[q.conditionalQuestionCode];
    return parentValue === q.conditionalValue;
  });

  visibleQuestions.forEach((question) => {
    if (question.isRequired) {
      const value = state.answers[question.code];
      if (!value || value.trim() === '') {
        errors[question.code] = `${question.label.replace(/{widerServiceCategoryName}/g, state.name)} is required`;
      }
    }
  });

  return errors;
};

export const buildAnswersForRequest = (
  state: ServiceCategoryFormState,
  questions: ServiceCategoryFormQuestionDto[]
): ServiceCategoryAnswerInputRequest[] => {
  const visibleQuestions = questions.filter((q) => {
    if (!q.conditionalQuestionCode) return true;
    const parentValue = state.answers[q.conditionalQuestionCode];
    return parentValue === q.conditionalValue;
  });

  return visibleQuestions.map((question) => ({
    questionCode: question.code,
    value: state.answers[question.code] ?? null,
  }));
};

export const mapServiceCategoryToFormState = (
  serviceCategory: ServiceCategoryDto
): ServiceCategoryFormState => {
  const answers: Record<string, string | undefined> = {};
  serviceCategory.answers.forEach((answer) => {
    answers[answer.questionCode] = answer.value ?? undefined;
  });

  return {
    name: serviceCategory.categoryName,
    categoryCode: serviceCategory.categoryCode,
    answers,
  };
};

export const replaceCategoryNamePlaceholder = (
  text: string,
  categoryName: string
): string => {
  return text.replace(/{widerServiceCategoryName}/g, categoryName);
};
