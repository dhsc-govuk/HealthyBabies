import axios from 'axios';

export const deleteSiteFormQuestion = (id: string) =>
  axios.delete(`/site-form-questions/${id}`);

export interface ReorderRequest {
  questions: { id: string; displayOrder: number }[];
}

export const reorderSiteFormQuestions = (data: ReorderRequest) =>
  axios.post('/site-form-questions/reorder', data);
