import axios from 'axios';
import { ServiceFormQuestion } from '../Common/types';

export const getAllServiceFormQuestions = () =>
  axios.get<ServiceFormQuestion[]>('/service-form-questions/all');
