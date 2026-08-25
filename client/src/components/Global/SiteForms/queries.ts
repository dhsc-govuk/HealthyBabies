import axios from 'axios';
import { SiteFormQuestionDto } from './types';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const getSiteFormQuestions = () =>
  axios.get<SiteFormQuestionDto[]>(API_ROUTES.siteFormQuestions.list);

export const getAllSiteFormQuestions = () =>
  axios.get<SiteFormQuestionDto[]>(API_ROUTES.siteFormQuestions.all);

export const getSiteFormQuestionById = (id: string) =>
  axios.get<SiteFormQuestionDto>(API_ROUTES.siteFormQuestions.byId(id));

export const siteFormQuestionsCacheKey = () => ['site-form-questions'];
export const allSiteFormQuestionsCacheKey = () => ['site-form-questions', 'all'];
export const siteFormQuestionCacheKey = (id: string) => ['site-form-question', id];
