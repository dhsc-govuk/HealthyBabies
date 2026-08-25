export interface ShowWhenCondition {
  fieldKey?: string;
  equals?: string;
  gt?: string;
  gte?: string;
  lt?: string;
  lte?: string;
  eq?: string;
  neq?: string;
  in?: string[];
  allOf?: ShowWhenCondition[];
  anyOf?: ShowWhenCondition[];
}

export interface ConditionalRule {
  showWhen?: ShowWhenCondition;
  displayInline?: boolean;
  parentOption?: string;
}

export interface FormFieldOption {
  value: string;
  label: string;
  displayOrder: number;
  isDefault: boolean;
  // True for options loaded from the server. Their value is system-owned and locked:
  // retained as-is and never regenerated when the label is edited. Newly added options
  // (isPersisted falsy) auto-generate their value from the label.
  isPersisted?: boolean;
}

export interface FormField {
  id: string;
  formModuleId: string;
  formSectionId: string | null;
  fieldKey: string;
  label: string;
  fieldType: string;
  displayOrder: number;
  isRequired: boolean;
  placeholder: string | null;
  helpText: string | null;
  defaultValue: string | null;
  validationRules: string | null;
  conditionalRules: string | null;
  configuration: string | null;
  isActive: boolean;
  options: FormFieldOption[];
}

export interface FormSection {
  id: string;
  formModuleId: string;
  sectionNumber: number;
  title: string;
  description: string | null;
  helpText: string | null;
  helpUrl: string | null;
  isActive: boolean;
}

export type FormModuleType = 'management-information' | 'delivery-plan';

export const formModuleTypeOptions: { value: FormModuleType; label: string }[] = [
  { value: 'management-information', label: 'Management Information' },
  { value: 'delivery-plan', label: 'Delivery Plan' },
];

export interface FormModule {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  lastChangedOn: string;
  isActive: boolean;
  type: FormModuleType;
}

export interface FormModuleWithFields extends FormModule {
  sections: FormSection[];
  fields: FormField[];
}

export interface QuestionFormState {
  fieldKey: string;
  label: string;
  fieldType: string;
  formModuleId: string;
  formSectionId: string;
  displayOrder: number;
  isRequired: boolean;
  isActive: boolean;
  placeholder: string;
  helpText: string;
  defaultValue: string;
  validationRules: string;
  conditionalRules: string;
  configuration: string;
  options: FormFieldOption[];
}

export interface ValidationErrors {
  fieldKey: string | boolean;
  label: string | boolean;
  options: string | boolean;
  conditionalRules: string | boolean;
}

export const validationMessages = {
  fieldKey: 'Field key is required',
  fieldKeyFormat: 'Field key must contain only alphanumeric characters and underscores (max 20)',
  label: 'Label is required',
  options: 'Options are required for this field type',
  optionsInvalidChars: 'Option labels and values must not contain < or > characters',
  conditionalRulesDuplicate: 'The same question cannot be used in more than one AND condition',
  conditionalRulesForwardRef: 'A condition can only reference a question that appears earlier in the form',
};

export const containsWafBlockedChars = (text: string): boolean => /[<>]/.test(text);

export const fieldTypeLabels: Record<string, string> = {
  text: 'Text',
  number: 'Number',
  email: 'Email',
  phone: 'Phone',
  textarea: 'Textarea',
  radio: 'Radio',
  checkbox: 'Checkbox',
  select: 'Select',
  multi_select: 'Multi Select',
  file: 'File',
  address: 'Address',
  post_code: 'Post Code',
  currency: 'Currency',
  calculation: 'Calculation',
  read_only: 'Read Only',
};

export const fieldTypeOptions = [
  { value: 'text', label: 'Text' },
  { value: 'number', label: 'Number' },
  { value: 'email', label: 'Email' },
  { value: 'phone', label: 'Phone' },
  { value: 'textarea', label: 'Textarea' },
  { value: 'radio', label: 'Radio' },
  { value: 'checkbox', label: 'Checkbox' },
  { value: 'select', label: 'Select' },
  { value: 'multi_select', label: 'Multi Select' },
  { value: 'file', label: 'File' },
  { value: 'currency', label: 'Currency' },
  { value: 'calculation', label: 'Calculation' },
  { value: 'read_only', label: 'Read Only' },
  { value: 'date', label: 'Date' },
];

export const initialFormState: QuestionFormState = {
  fieldKey: '',
  label: '',
  fieldType: 'text',
  formModuleId: '',
  formSectionId: '',
  displayOrder: 1,
  isRequired: false,
  isActive: true,
  placeholder: '',
  helpText: '',
  defaultValue: '',
  validationRules: '',
  conditionalRules: '',
  configuration: '',
  options: [],
};

export const requiresOptions = (fieldType: string): boolean => {
  return fieldType === 'radio' || fieldType === 'checkbox' || fieldType === 'select' || fieldType === 'multi_select';
};
