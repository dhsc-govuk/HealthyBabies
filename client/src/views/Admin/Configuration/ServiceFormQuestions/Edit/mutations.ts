import axios from 'axios';
import { ServiceFormQuestion, ServiceFormQuestionOption } from '../Common/types';

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
  options: ServiceFormQuestionOption[];
}

export const updateServiceFormQuestion = (data: UpdateServiceFormQuestionRequest) =>
  axios.put<ServiceFormQuestion>(`/service-form-questions/${data.id}`, data);
