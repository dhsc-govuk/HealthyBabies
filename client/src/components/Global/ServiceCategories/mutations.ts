import axios from 'axios';
import {
  ServiceCategoryDto,
  CreateServiceCategoryRequest,
  UpdateServiceCategoryStepOneRequest,
} from './types';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const createServiceCategory = (request: CreateServiceCategoryRequest) =>
  axios.post<ServiceCategoryDto>(API_ROUTES.serviceCategories.create, request);

export const updateServiceCategoryStepOne = (
  id: string,
  request: UpdateServiceCategoryStepOneRequest
) => axios.put<ServiceCategoryDto>(API_ROUTES.serviceCategories.stepOne(id), request);

export const completeServiceCategory = (id: string) =>
  axios.post<ServiceCategoryDto>(API_ROUTES.serviceCategories.complete(id));

export const deleteServiceCategory = (id: string) =>
  axios.delete(API_ROUTES.serviceCategories.byId(id));
