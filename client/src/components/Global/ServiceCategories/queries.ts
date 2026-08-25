import axios from 'axios';
import {
  ServiceCategoryDto,
  ServiceCategoryListDto,
  ServiceCategoryFormQuestionDto,
  WiderServiceCategoryLookup,
} from './types';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const serviceCategoriesCacheKey = () => ['service-categories'];
export const serviceCategoryCacheKey = (id: string) => ['service-categories', id];
export const serviceCategoryFormQuestionsCacheKey = () => ['service-category-form-questions'];
export const widerServiceCategoriesLookupCacheKey = () => ['wider-service-categories-lookup'];

export const getServiceCategories = () =>
  axios.get<ServiceCategoryListDto[]>(API_ROUTES.serviceCategories.list);

export const getServiceCategory = (id: string) =>
  axios.get<ServiceCategoryDto>(API_ROUTES.serviceCategories.byId(id));

export const getServiceCategoryFormQuestions = () =>
  axios.get<ServiceCategoryFormQuestionDto[]>(API_ROUTES.serviceCategoryFormQuestions.list);

export const getServiceCategoryFormQuestionsByStep = (step: number) =>
  axios.get<ServiceCategoryFormQuestionDto[]>(API_ROUTES.serviceCategoryFormQuestions.step(step));

export const getWiderServiceCategoriesLookup = () =>
  axios.get<WiderServiceCategoryLookup[]>(API_ROUTES.globalData.entity('WIDER_SERVICE_CATEGORIES'));

export type TemplateFormat = 'csv' | 'xlsx';

export const downloadServiceCategoriesTemplate = (format: TemplateFormat = 'csv') =>
  axios.get(API_ROUTES.serviceCategories.bulkUploadTemplate, {
    params: { format },
    responseType: 'blob',
  });
