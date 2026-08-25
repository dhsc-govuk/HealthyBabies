import axios from 'axios';
import { SiteFormQuestion } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getAllSiteFormQuestions = () =>
  axios.get<SiteFormQuestion[]>(API_ROUTES.siteFormQuestions.all);
