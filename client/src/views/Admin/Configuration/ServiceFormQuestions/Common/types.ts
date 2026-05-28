export interface ServiceFormQuestionOption {
  value: string;
  label: string;
  displayOrder: number;
}

export interface ServiceFormQuestion {
  id: string;
  code: string;
  label: string;
  hint: string | null;
  placeholder: string | null;
  questionType: number;
  step: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary: string | null;
  helpText: string | null;
  conditionalQuestionCode: string | null;
  conditionalValue: string | null;
  isActive: boolean;
  options: ServiceFormQuestionOption[];
}

export interface QuestionFormState {
  code: string;
  label: string;
  hint: string;
  placeholder: string;
  questionType: number;
  step: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  isActive: boolean;
  helpTextSummary: string;
  helpText: string;
  conditionalQuestionCode: string;
  conditionalValue: string;
  options: ServiceFormQuestionOption[];
}

export interface ValidationErrors {
  code: string | boolean;
  label: string | boolean;
  options: string | boolean;
}

export const validationMessages = {
  code: 'Code is required',
  codeFormat: 'Code must contain only alphanumeric characters (max 10)',
  label: 'Label is required',
  options: 'Options are required for this question type',
  optionsInvalidChars: 'Option labels and values must not contain < or > characters',
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

export const stepOptions = [
  { value: 1, label: 'Step 1 - Service Details' },
  { value: 2, label: 'Step 2 - Additional Information' },
];

export const initialFormState: QuestionFormState = {
  code: '',
  label: '',
  hint: '',
  placeholder: '',
  questionType: 0,
  step: 1,
  displayOrder: 1,
  isRequired: false,
  isPredefined: true,
  isActive: true,
  helpTextSummary: '',
  helpText: '',
  conditionalQuestionCode: '',
  conditionalValue: '',
  options: [],
};

export const requiresOptions = (questionType: number): boolean => {
  return questionType === 1 || questionType === 2 || questionType === 3;
};
