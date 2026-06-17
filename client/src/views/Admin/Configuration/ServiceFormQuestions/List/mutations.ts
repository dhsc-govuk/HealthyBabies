import axios from 'axios';

export const deleteServiceFormQuestion = (id: string) =>
  axios.delete(`/service-form-questions/${id}`);

export interface ReorderRequest {
  step: number;
  questions: { id: string; displayOrder: number }[];
}

export const reorderServiceFormQuestions = (data: ReorderRequest) =>
  axios.post('/service-form-questions/reorder', data);
