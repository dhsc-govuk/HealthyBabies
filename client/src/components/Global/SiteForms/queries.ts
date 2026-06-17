import axios from 'axios';
import { SiteFormQuestionDto } from './types';

export const getSiteFormQuestions = () =>
  axios.get<SiteFormQuestionDto[]>('/site-form-questions');

export const getAllSiteFormQuestions = () =>
  axios.get<SiteFormQuestionDto[]>('/site-form-questions/all');

export const getSiteFormQuestionById = (id: string) =>
  axios.get<SiteFormQuestionDto>(`/site-form-questions/${id}`);

export const siteFormQuestionsCacheKey = () => ['site-form-questions'];
export const allSiteFormQuestionsCacheKey = () => ['site-form-questions', 'all'];
export const siteFormQuestionCacheKey = (id: string) => ['site-form-question', id];
