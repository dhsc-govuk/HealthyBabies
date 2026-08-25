import axios from 'axios';
import { FormField, FormFieldOption } from '../../DataCollectionFormQuestions/Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

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
  const response = await axios.put<FormField>(API_ROUTES.dataCollectionFormQuestions.byId(id), request);
  return response.data;
};
