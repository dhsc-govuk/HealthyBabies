import axios from 'axios';
import { SiteFormQuestion, SiteFormQuestionOption } from '../Common/types';

export interface CreateSiteFormQuestionRequest {
  code: string;
  label: string;
  hint?: string | null;
  placeholder?: string | null;
  questionType: number;
  isRequired: boolean;
  helpTextSummary?: string | null;
  helpText?: string | null;
  conditionalQuestionCode?: string | null;
  conditionalValue?: string | null;
  options: Omit<SiteFormQuestionOption, 'id'>[];
}

export const createSiteFormQuestion = (data: CreateSiteFormQuestionRequest) =>
  axios.post<SiteFormQuestion>('/site-form-questions', data);
