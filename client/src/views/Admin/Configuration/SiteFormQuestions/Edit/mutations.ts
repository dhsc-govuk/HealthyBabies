import axios from 'axios';
import { SiteFormQuestion, SiteFormQuestionOption } from '../Common/types';

export interface UpdateSiteFormQuestionRequest {
  label: string;
  hint?: string | null;
  placeholder?: string | null;
  questionType: number;
  displayOrder: number;
  isRequired: boolean;
  isActive: boolean;
  helpTextSummary?: string | null;
  helpText?: string | null;
  conditionalQuestionCode?: string | null;
  conditionalValue?: string | null;
  options: Omit<SiteFormQuestionOption, 'id'>[];
}

export const updateSiteFormQuestion = (id: string, data: UpdateSiteFormQuestionRequest) =>
  axios.put<SiteFormQuestion>(`/site-form-questions/${id}`, data);

export const getSiteFormQuestionById = (id: string) =>
  axios.get<SiteFormQuestion>(`/site-form-questions/${id}`);
