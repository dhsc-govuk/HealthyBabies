import axios from 'axios';
import { SiteFormQuestion, SiteFormQuestionOption } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

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
  dateValidationRule?: string | null;
  conditionalRules?: string | null;
  options: Omit<SiteFormQuestionOption, 'id'>[];
}

export const createSiteFormQuestion = (data: CreateSiteFormQuestionRequest) =>
  axios.post<SiteFormQuestion>(API_ROUTES.siteFormQuestions.create, data);
