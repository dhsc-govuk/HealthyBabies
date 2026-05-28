import { FormFieldOption, QuestionFormState } from './types';

export enum QuestionFormAction {
  SET_FIELD_KEY = 'SET_FIELD_KEY',
  SET_LABEL = 'SET_LABEL',
  SET_FIELD_TYPE = 'SET_FIELD_TYPE',
  SET_FORM_MODULE_ID = 'SET_FORM_MODULE_ID',
  SET_FORM_SECTION_ID = 'SET_FORM_SECTION_ID',
  SET_DISPLAY_ORDER = 'SET_DISPLAY_ORDER',
  SET_IS_REQUIRED = 'SET_IS_REQUIRED',
  SET_IS_ACTIVE = 'SET_IS_ACTIVE',
  SET_PLACEHOLDER = 'SET_PLACEHOLDER',
  SET_HELP_TEXT = 'SET_HELP_TEXT',
  SET_DEFAULT_VALUE = 'SET_DEFAULT_VALUE',
  SET_VALIDATION_RULES = 'SET_VALIDATION_RULES',
  SET_CONDITIONAL_RULES = 'SET_CONDITIONAL_RULES',
  SET_CONFIGURATION = 'SET_CONFIGURATION',
  SET_OPTIONS = 'SET_OPTIONS',
  ADD_OPTION = 'ADD_OPTION',
  REMOVE_OPTION = 'REMOVE_OPTION',
  UPDATE_OPTION = 'UPDATE_OPTION',
  REORDER_OPTIONS = 'REORDER_OPTIONS',
  INIT = 'INIT',
}

type Action =
  | { type: QuestionFormAction.SET_FIELD_KEY; value: string }
  | { type: QuestionFormAction.SET_LABEL; value: string }
  | { type: QuestionFormAction.SET_FIELD_TYPE; value: string }
  | { type: QuestionFormAction.SET_FORM_MODULE_ID; value: string }
  | { type: QuestionFormAction.SET_FORM_SECTION_ID; value: string }
  | { type: QuestionFormAction.SET_DISPLAY_ORDER; value: number }
  | { type: QuestionFormAction.SET_IS_REQUIRED; value: boolean }
  | { type: QuestionFormAction.SET_IS_ACTIVE; value: boolean }
  | { type: QuestionFormAction.SET_PLACEHOLDER; value: string }
  | { type: QuestionFormAction.SET_HELP_TEXT; value: string }
  | { type: QuestionFormAction.SET_DEFAULT_VALUE; value: string }
  | { type: QuestionFormAction.SET_VALIDATION_RULES; value: string }
  | { type: QuestionFormAction.SET_CONDITIONAL_RULES; value: string }
  | { type: QuestionFormAction.SET_CONFIGURATION; value: string }
  | { type: QuestionFormAction.SET_OPTIONS; value: FormFieldOption[] }
  | { type: QuestionFormAction.ADD_OPTION }
  | { type: QuestionFormAction.REMOVE_OPTION; index: number }
  | { type: QuestionFormAction.UPDATE_OPTION; index: number; field: 'value' | 'label'; value: string }
  | { type: QuestionFormAction.REORDER_OPTIONS; fromIndex: number; toIndex: number }
  | { type: QuestionFormAction.INIT; value: QuestionFormState };

export const questionFormReducer = (state: QuestionFormState, action: Action): QuestionFormState => {
  switch (action.type) {
    case QuestionFormAction.SET_FIELD_KEY:
      return { ...state, fieldKey: action.value };
    case QuestionFormAction.SET_LABEL:
      return { ...state, label: action.value };
    case QuestionFormAction.SET_FIELD_TYPE:
      return { ...state, fieldType: action.value };
    case QuestionFormAction.SET_FORM_MODULE_ID:
      return { ...state, formModuleId: action.value };
    case QuestionFormAction.SET_FORM_SECTION_ID:
      return { ...state, formSectionId: action.value };
    case QuestionFormAction.SET_DISPLAY_ORDER:
      return { ...state, displayOrder: action.value };
    case QuestionFormAction.SET_IS_REQUIRED:
      return { ...state, isRequired: action.value };
    case QuestionFormAction.SET_IS_ACTIVE:
      return { ...state, isActive: action.value };
    case QuestionFormAction.SET_PLACEHOLDER:
      return { ...state, placeholder: action.value };
    case QuestionFormAction.SET_HELP_TEXT:
      return { ...state, helpText: action.value };
    case QuestionFormAction.SET_DEFAULT_VALUE:
      return { ...state, defaultValue: action.value };
    case QuestionFormAction.SET_VALIDATION_RULES:
      return { ...state, validationRules: action.value };
    case QuestionFormAction.SET_CONDITIONAL_RULES:
      return { ...state, conditionalRules: action.value };
    case QuestionFormAction.SET_CONFIGURATION:
      return { ...state, configuration: action.value };
    case QuestionFormAction.SET_OPTIONS:
      return { ...state, options: action.value };
    case QuestionFormAction.ADD_OPTION: {
      const newOption: FormFieldOption = {
        value: '',
        label: '',
        displayOrder: state.options.length + 1,
        isDefault: false,
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
