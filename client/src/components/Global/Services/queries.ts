import axios from 'axios';
import { ServiceDto, ServiceListDto, ServiceFormQuestionDto } from './types';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const getServices = () =>
  axios.get<ServiceListDto[]>(API_ROUTES.services.list);

export const getService = (serviceId: string) =>
  axios.get<ServiceDto>(API_ROUTES.services.byId(serviceId));

export const getServiceFormQuestions = () =>
  axios.get<ServiceFormQuestionDto[]>(API_ROUTES.serviceFormQuestions.list);

export const getServiceFormQuestionsByStep = (step: number) =>
  axios.get<ServiceFormQuestionDto[]>(API_ROUTES.serviceFormQuestions.step(step));

export const servicesCacheKey = () => ['services-list'];
export const serviceCacheKey = (serviceId: string) => ['service', serviceId];
export const serviceFormQuestionsCacheKey = () => ['service-form-questions'];
export const serviceFormQuestionsByStepCacheKey = (step: number) => ['service-form-questions', 'step', step];
