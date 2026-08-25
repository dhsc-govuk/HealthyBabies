import axios from 'axios';
import { ServiceFormQuestion, ServiceFormQuestionOption } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface UpdateServiceFormQuestionRequest {
  id: string;
  label: string;
  hint: string | null;
  placeholder: string | null;
  questionType: number;
  step: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  isActive: boolean;
  helpTextSummary: string | null;
  helpText: string | null;
  conditionalQuestionCode: string | null;
  conditionalValue: string | null;
  dateValidationRule: string | null;
  conditionalRules: string | null;
  options: ServiceFormQuestionOption[];
}

export const updateServiceFormQuestion = (data: UpdateServiceFormQuestionRequest) => axios.put<ServiceFormQuestion>(API_ROUTES.serviceFormQuestions.byId(data.id), data);
