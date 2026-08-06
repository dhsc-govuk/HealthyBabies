export interface SiteFormQuestionOption {
  value: string;
  label: string;
  displayOrder: number;
  // True for options loaded from the server. Their value is system-owned and locked:
  // retained as-is and never regenerated when the label is edited. Newly added options
  // (isPersisted falsy) auto-generate their value from the label.
  isPersisted?: boolean;
}

export interface SiteFormQuestion {
  id: string;
  code: string;
  label: string;
  hint: string | null;
  placeholder: string | null;
  questionType: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary: string | null;
  helpText: string | null;
  conditionalQuestionCode: string | null;
  conditionalValue: string | null;
  conditionalRules: string | null;
  isActive: boolean;
  dateValidationRule: string | null;
  options: SiteFormQuestionOption[];
}

export interface QuestionFormState {
  code: string;
  label: string;
  hint: string;
  placeholder: string;
  questionType: number;
  displayOrder: number;
  isRequired: boolean;
  isActive: boolean;
  helpTextSummary: string;
  helpText: string;
  conditionalQuestionCode: string;
  conditionalValue: string;
  conditionalRules: string;
  dateValidationRule: string;
  options: SiteFormQuestionOption[];
}

export interface ValidationErrors {
  code: string | boolean;
  label: string | boolean;
  options: string | boolean;
  conditionalRules?: string | boolean;
}

export const validationMessages = {
  code: 'Code is required',
  codeFormat: 'Code must contain only alphanumeric characters (max 10)',
  label: 'Label is required',
  options: 'Options are required for this question type',
  optionsInvalidChars: 'Option labels and values must not contain < or > characters',
  conditionalRulesDuplicate: 'The same question cannot be used in more than one AND condition',
  conditionalRulesForwardRef: 'A condition can only reference a question that appears earlier in the form',
};

export const containsWafBlockedChars = (text: string): boolean => /[<>]/.test(text);

export const questionTypeLabels: Record<number, string> = {
  0: 'Text',
  1: 'Radio',
  2: 'Checkbox',
  3: 'Select',
  4: 'Date',
};

export const questionTypeOptions = [
  { value: 0, label: 'Text' },
  { value: 1, label: 'Radio' },
  { value: 2, label: 'Checkbox' },
  { value: 3, label: 'Select' },
  { value: 4, label: 'Date' },
];

export const initialFormState: QuestionFormState = {
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
  conditionalRules: '',
  dateValidationRule: '',
  options: [],
};

export const requiresOptions = (questionType: number): boolean => {
  return questionType === 1 || questionType === 2 || questionType === 3;
};

export const DATE_QUESTION_TYPE = 4;

export const isDateQuestion = (questionType: number): boolean => questionType === DATE_QUESTION_TYPE;

export const dateValidationRuleOptions = [
  { value: '', label: 'No date restriction' },
  { value: 'not_in_past', label: 'Must not be in the past (today or later)' },
  { value: 'not_in_future', label: 'Must not be in the future (today or earlier)' },
];
