import axios from 'axios';
import { FormField, FormFieldOption } from '../Common/types';

export interface CreateFormFieldRequest {
  formModuleId: string;
  formSectionId: string | null;
  fieldKey: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  placeholder: string | null;
  helpText: string | null;
  defaultValue: string | null;
  validationRules: string | null;
  conditionalRules: string | null;
  configuration: string | null;
  options: FormFieldOption[];
}

export const createFormField = async (request: CreateFormFieldRequest): Promise<FormField> => {
  const response = await axios.post<FormField>('/data-collection-form-questions', request);
  return response.data;
};
