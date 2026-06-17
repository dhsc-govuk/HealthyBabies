import axios from 'axios';
import { ServiceFormQuestion, ServiceFormQuestionOption } from '../Common/types';

export interface CreateServiceFormQuestionRequest {
  code: string;
  label: string;
  hint: string | null;
  placeholder: string | null;
  questionType: number;
  step: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary: string | null;
  helpText: string | null;
  conditionalQuestionCode: string | null;
  conditionalValue: string | null;
  options: ServiceFormQuestionOption[];
}

export const createServiceFormQuestion = (data: CreateServiceFormQuestionRequest) =>
  axios.post<ServiceFormQuestion>('/service-form-questions', data);
