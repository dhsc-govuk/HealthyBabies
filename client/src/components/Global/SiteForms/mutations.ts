import axios from 'axios';
import { SiteFormQuestionDto, SiteFormQuestionOptionDto } from './types';

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
  options: Omit<SiteFormQuestionOptionDto, 'id'>[];
}

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
  options: Omit<SiteFormQuestionOptionDto, 'id'>[];
}

export const createSiteFormQuestion = (data: CreateSiteFormQuestionRequest) =>
  axios.post<SiteFormQuestionDto>('/site-form-questions', data);

export const updateSiteFormQuestion = (id: string, data: UpdateSiteFormQuestionRequest) =>
  axios.put<SiteFormQuestionDto>(`/site-form-questions/${id}`, data);

export const deleteSiteFormQuestion = (id: string) =>
  axios.delete(`/site-form-questions/${id}`);

export interface ReorderSiteFormQuestionsRequest {
  questions: { id: string; displayOrder: number }[];
}

export const reorderSiteFormQuestions = (data: ReorderSiteFormQuestionsRequest) =>
  axios.post('/site-form-questions/reorder', data);
