import { QuestionFormState, SiteFormQuestionOption } from './types';

export enum ActionType {
  SET_CODE = 'SET_CODE',
  SET_LABEL = 'SET_LABEL',
  SET_HINT = 'SET_HINT',
  SET_PLACEHOLDER = 'SET_PLACEHOLDER',
  SET_QUESTION_TYPE = 'SET_QUESTION_TYPE',
  SET_DISPLAY_ORDER = 'SET_DISPLAY_ORDER',
  SET_IS_REQUIRED = 'SET_IS_REQUIRED',
  SET_IS_ACTIVE = 'SET_IS_ACTIVE',
  SET_HELP_TEXT_SUMMARY = 'SET_HELP_TEXT_SUMMARY',
  SET_HELP_TEXT = 'SET_HELP_TEXT',
  SET_CONDITIONAL_QUESTION_CODE = 'SET_CONDITIONAL_QUESTION_CODE',
  SET_CONDITIONAL_VALUE = 'SET_CONDITIONAL_VALUE',
  SET_OPTIONS = 'SET_OPTIONS',
  ADD_OPTION = 'ADD_OPTION',
  UPDATE_OPTION = 'UPDATE_OPTION',
  REMOVE_OPTION = 'REMOVE_OPTION',
  INIT_FROM_QUESTION = 'INIT_FROM_QUESTION',
  RESET = 'RESET',
}

export type Action =
  | { type: ActionType.SET_CODE; payload: string }
  | { type: ActionType.SET_LABEL; payload: string }
  | { type: ActionType.SET_HINT; payload: string }
  | { type: ActionType.SET_PLACEHOLDER; payload: string }
  | { type: ActionType.SET_QUESTION_TYPE; payload: number }
  | { type: ActionType.SET_DISPLAY_ORDER; payload: number }
  | { type: ActionType.SET_IS_REQUIRED; payload: boolean }
  | { type: ActionType.SET_IS_ACTIVE; payload: boolean }
  | { type: ActionType.SET_HELP_TEXT_SUMMARY; payload: string }
  | { type: ActionType.SET_HELP_TEXT; payload: string }
  | { type: ActionType.SET_CONDITIONAL_QUESTION_CODE; payload: string }
  | { type: ActionType.SET_CONDITIONAL_VALUE; payload: string }
  | { type: ActionType.SET_OPTIONS; payload: SiteFormQuestionOption[] }
  | { type: ActionType.ADD_OPTION; payload: SiteFormQuestionOption }
  | { type: ActionType.UPDATE_OPTION; payload: { index: number; option: SiteFormQuestionOption } }
  | { type: ActionType.REMOVE_OPTION; payload: number }
  | { type: ActionType.INIT_FROM_QUESTION; payload: QuestionFormState }
  | { type: ActionType.RESET };

export const questionFormReducer = (state: QuestionFormState, action: Action): QuestionFormState => {
  switch (action.type) {
    case ActionType.SET_CODE:
      return { ...state, code: action.payload };
    case ActionType.SET_LABEL:
      return { ...state, label: action.payload };
    case ActionType.SET_HINT:
      return { ...state, hint: action.payload };
    case ActionType.SET_PLACEHOLDER:
      return { ...state, placeholder: action.payload };
    case ActionType.SET_QUESTION_TYPE:
      return { ...state, questionType: action.payload };
    case ActionType.SET_DISPLAY_ORDER:
      return { ...state, displayOrder: action.payload };
    case ActionType.SET_IS_REQUIRED:
      return { ...state, isRequired: action.payload };
    case ActionType.SET_IS_ACTIVE:
      return { ...state, isActive: action.payload };
    case ActionType.SET_HELP_TEXT_SUMMARY:
      return { ...state, helpTextSummary: action.payload };
    case ActionType.SET_HELP_TEXT:
      return { ...state, helpText: action.payload };
    case ActionType.SET_CONDITIONAL_QUESTION_CODE:
      return { ...state, conditionalQuestionCode: action.payload };
    case ActionType.SET_CONDITIONAL_VALUE:
      return { ...state, conditionalValue: action.payload };
    case ActionType.SET_OPTIONS:
      return { ...state, options: action.payload };
    case ActionType.ADD_OPTION:
      return { ...state, options: [...state.options, action.payload] };
    case ActionType.UPDATE_OPTION:
      return {
        ...state,
        options: state.options.map((opt, i) =>
          i === action.payload.index ? action.payload.option : opt
        ),
      };
    case ActionType.REMOVE_OPTION:
      return {
        ...state,
        options: state.options.filter((_, i) => i !== action.payload),
      };
    case ActionType.INIT_FROM_QUESTION:
      return action.payload;
    case ActionType.RESET:
      return {
        code: '',
        label: '',
        hint: '',
        placeholder: '',
        questionType: 0,
        displayOrder: 1,
        isRequired: false,
        isActive: true,
        helpTextSummary: '',
        helpText: '',
        conditionalQuestionCode: '',
        conditionalValue: '',
        options: [],
      };
    default:
      return state;
  }
};
