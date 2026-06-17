import axios from 'axios';
import {
  ServiceCategoryDto,
  CreateServiceCategoryRequest,
  UpdateServiceCategoryStepOneRequest,
} from './types';

export const createServiceCategory = (request: CreateServiceCategoryRequest) =>
  axios.post<ServiceCategoryDto>('service-categories/create', request);

export const updateServiceCategoryStepOne = (
  id: string,
  request: UpdateServiceCategoryStepOneRequest
) => axios.put<ServiceCategoryDto>(`service-categories/${id}/step-one`, request);

export const completeServiceCategory = (id: string) =>
  axios.post<ServiceCategoryDto>(`service-categories/${id}/complete`);

export const deleteServiceCategory = (id: string) =>
  axios.delete(`service-categories/${id}`);
