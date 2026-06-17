import axios from 'axios';
import { FormField, FormFieldOption } from '../Common/types';

export interface UpdateFormFieldRequest {
  formSectionId: string | null;
  label: string;
  fieldType: string;
  displayOrder: number;
  isRequired: boolean;
  isActive: boolean;
  placeholder: string | null;
  helpText: string | null;
  defaultValue: string | null;
  validationRules: string | null;
  conditionalRules: string | null;
  configuration: string | null;
  options: FormFieldOption[];
}

export const updateFormField = async (id: string, request: UpdateFormFieldRequest): Promise<FormField> => {
  const response = await axios.put<FormField>(`/data-collection-form-questions/${id}`, request);
  return response.data;
};
