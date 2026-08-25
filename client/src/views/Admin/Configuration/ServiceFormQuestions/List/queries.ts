import axios from 'axios';
import { ServiceFormQuestion } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getAllServiceFormQuestions = () =>
  axios.get<ServiceFormQuestion[]>(API_ROUTES.serviceFormQuestions.all);
