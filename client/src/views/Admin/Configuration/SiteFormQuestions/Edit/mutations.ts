import axios from 'axios';
import { SiteFormQuestion, SiteFormQuestionOption } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

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
  dateValidationRule?: string | null;
  conditionalRules?: string | null;
  options: Omit<SiteFormQuestionOption, 'id'>[];
}

export const updateSiteFormQuestion = (id: string, data: UpdateSiteFormQuestionRequest) =>
  axios.put<SiteFormQuestion>(API_ROUTES.siteFormQuestions.byId(id), data);

export const getSiteFormQuestionById = (id: string) =>
  axios.get<SiteFormQuestion>(API_ROUTES.siteFormQuestions.byId(id));
