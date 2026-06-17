import { QuestionFormState, ServiceFormQuestionOption } from './types';

export enum QuestionFormAction {
  SET_CODE = 'SET_CODE',
  SET_LABEL = 'SET_LABEL',
  SET_HINT = 'SET_HINT',
  SET_PLACEHOLDER = 'SET_PLACEHOLDER',
  SET_QUESTION_TYPE = 'SET_QUESTION_TYPE',
  SET_STEP = 'SET_STEP',
  SET_DISPLAY_ORDER = 'SET_DISPLAY_ORDER',
  SET_IS_REQUIRED = 'SET_IS_REQUIRED',
  SET_IS_PREDEFINED = 'SET_IS_PREDEFINED',
  SET_IS_ACTIVE = 'SET_IS_ACTIVE',
  SET_HELP_TEXT_SUMMARY = 'SET_HELP_TEXT_SUMMARY',
  SET_HELP_TEXT = 'SET_HELP_TEXT',
  SET_CONDITIONAL_QUESTION_CODE = 'SET_CONDITIONAL_QUESTION_CODE',
  SET_CONDITIONAL_VALUE = 'SET_CONDITIONAL_VALUE',
  SET_OPTIONS = 'SET_OPTIONS',
  ADD_OPTION = 'ADD_OPTION',
  REMOVE_OPTION = 'REMOVE_OPTION',
  UPDATE_OPTION = 'UPDATE_OPTION',
  REORDER_OPTIONS = 'REORDER_OPTIONS',
  INIT = 'INIT',
}

type Action =
  | { type: QuestionFormAction.SET_CODE; value: string }
  | { type: QuestionFormAction.SET_LABEL; value: string }
  | { type: QuestionFormAction.SET_HINT; value: string }
  | { type: QuestionFormAction.SET_PLACEHOLDER; value: string }
  | { type: QuestionFormAction.SET_QUESTION_TYPE; value: number }
  | { type: QuestionFormAction.SET_STEP; value: number }
  | { type: QuestionFormAction.SET_DISPLAY_ORDER; value: number }
  | { type: QuestionFormAction.SET_IS_REQUIRED; value: boolean }
  | { type: QuestionFormAction.SET_IS_PREDEFINED; value: boolean }
  | { type: QuestionFormAction.SET_IS_ACTIVE; value: boolean }
  | { type: QuestionFormAction.SET_HELP_TEXT_SUMMARY; value: string }
  | { type: QuestionFormAction.SET_HELP_TEXT; value: string }
  | { type: QuestionFormAction.SET_CONDITIONAL_QUESTION_CODE; value: string }
  | { type: QuestionFormAction.SET_CONDITIONAL_VALUE; value: string }
  | { type: QuestionFormAction.SET_OPTIONS; value: ServiceFormQuestionOption[] }
  | { type: QuestionFormAction.ADD_OPTION }
  | { type: QuestionFormAction.REMOVE_OPTION; index: number }
  | { type: QuestionFormAction.UPDATE_OPTION; index: number; field: 'value' | 'label'; value: string }
  | { type: QuestionFormAction.REORDER_OPTIONS; fromIndex: number; toIndex: number }
  | { type: QuestionFormAction.INIT; value: QuestionFormState };

export const questionFormReducer = (state: QuestionFormState, action: Action): QuestionFormState => {
  switch (action.type) {
    case QuestionFormAction.SET_CODE:
      return { ...state, code: action.value };
    case QuestionFormAction.SET_LABEL:
      return { ...state, label: action.value };
    case QuestionFormAction.SET_HINT:
      return { ...state, hint: action.value };
    case QuestionFormAction.SET_PLACEHOLDER:
      return { ...state, placeholder: action.value };
    case QuestionFormAction.SET_QUESTION_TYPE:
      return { ...state, questionType: action.value };
    case QuestionFormAction.SET_STEP:
      return { ...state, step: action.value };
    case QuestionFormAction.SET_DISPLAY_ORDER:
      return { ...state, displayOrder: action.value };
    case QuestionFormAction.SET_IS_REQUIRED:
      return { ...state, isRequired: action.value };
    case QuestionFormAction.SET_IS_PREDEFINED:
      return { ...state, isPredefined: action.value };
    case QuestionFormAction.SET_IS_ACTIVE:
      return { ...state, isActive: action.value };
    case QuestionFormAction.SET_HELP_TEXT_SUMMARY:
      return { ...state, helpTextSummary: action.value };
    case QuestionFormAction.SET_HELP_TEXT:
      return { ...state, helpText: action.value };
    case QuestionFormAction.SET_CONDITIONAL_QUESTION_CODE:
      return { ...state, conditionalQuestionCode: action.value, conditionalValue: '' };
    case QuestionFormAction.SET_CONDITIONAL_VALUE:
      return { ...state, conditionalValue: action.value };
    case QuestionFormAction.SET_OPTIONS:
      return { ...state, options: action.value };
    case QuestionFormAction.ADD_OPTION: {
      const newOption: ServiceFormQuestionOption = {
        value: '',
        label: '',
        displayOrder: state.options.length + 1,
      };
      return { ...state, options: [...state.options, newOption] };
    }
    case QuestionFormAction.REMOVE_OPTION: {
      const newOptions = state.options
        .filter((_, i) => i !== action.index)
        .map((opt, i) => ({ ...opt, displayOrder: i + 1 }));
      return { ...state, options: newOptions };
    }
    case QuestionFormAction.UPDATE_OPTION: {
      const updatedOptions = state.options.map((opt, i) => {
        if (i === action.index) {
          return { ...opt, [action.field]: action.value };
        }
        return opt;
      });
      return { ...state, options: updatedOptions };
    }
    case QuestionFormAction.REORDER_OPTIONS: {
      const options = [...state.options];
      const [removed] = options.splice(action.fromIndex, 1);
      options.splice(action.toIndex, 0, removed);
      const reorderedOptions = options.map((opt, i) => ({ ...opt, displayOrder: i + 1 }));
      return { ...state, options: reorderedOptions };
    }
    case QuestionFormAction.INIT:
      return action.value;
    default:
      return state;
  }
};
