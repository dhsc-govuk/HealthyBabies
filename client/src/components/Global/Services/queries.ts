import axios from 'axios';
import { ServiceDto, ServiceListDto, ServiceFormQuestionDto } from './types';

export const getServices = () =>
  axios.get<ServiceListDto[]>('/services');

export const getService = (serviceId: string) =>
  axios.get<ServiceDto>(`/services/${serviceId}`);

export const getServiceFormQuestions = () =>
  axios.get<ServiceFormQuestionDto[]>('/service-form-questions');

export const getServiceFormQuestionsByStep = (step: number) =>
  axios.get<ServiceFormQuestionDto[]>(`/service-form-questions/step/${step}`);

export const servicesCacheKey = () => ['services-list'];
export const serviceCacheKey = (serviceId: string) => ['service', serviceId];
export const serviceFormQuestionsCacheKey = () => ['service-form-questions'];
export const serviceFormQuestionsByStepCacheKey = (step: number) => ['service-form-questions', 'step', step];
