export interface FormFieldOption {
  value: string;
  label: string;
  displayOrder: number;
  isDefault: boolean;
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

export interface FormModule {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  lastChangedOn: string;
  isActive: boolean;
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
}

export const validationMessages = {
  fieldKey: 'Field key is required',
  fieldKeyFormat: 'Field key must contain only alphanumeric characters and underscores (max 20)',
  label: 'Label is required',
  options: 'Options are required for this field type',
  optionsInvalidChars: 'Option labels and values must not contain < or > characters',
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
