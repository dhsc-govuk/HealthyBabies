import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const deleteServiceFormQuestion = (id: string) =>
  axios.delete(API_ROUTES.serviceFormQuestions.byId(id));

export interface ReorderRequest {
  step: number;
  questions: { id: string; displayOrder: number }[];
}

export const reorderServiceFormQuestions = (data: ReorderRequest) =>
  axios.post(API_ROUTES.serviceFormQuestions.reorder, data);
