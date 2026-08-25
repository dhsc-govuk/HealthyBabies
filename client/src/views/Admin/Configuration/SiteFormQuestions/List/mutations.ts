import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const deleteSiteFormQuestion = (id: string) =>
  axios.delete(API_ROUTES.siteFormQuestions.byId(id));

export interface ReorderRequest {
  questions: { id: string; displayOrder: number }[];
}

export const reorderSiteFormQuestions = (data: ReorderRequest) =>
  axios.post(API_ROUTES.siteFormQuestions.reorder, data);
