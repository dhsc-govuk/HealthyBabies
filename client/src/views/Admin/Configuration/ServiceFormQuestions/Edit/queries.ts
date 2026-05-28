import axios from 'axios';
import { ServiceFormQuestion } from '../Common/types';

export const getServiceFormQuestionById = (id: string) =>
  axios.get<ServiceFormQuestion>(`/service-form-questions/${id}`);
