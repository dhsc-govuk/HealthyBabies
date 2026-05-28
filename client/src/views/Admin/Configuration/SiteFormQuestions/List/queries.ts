import axios from 'axios';
import { SiteFormQuestion } from '../Common/types';

export const getAllSiteFormQuestions = () =>
  axios.get<SiteFormQuestion[]>('/site-form-questions/all');
