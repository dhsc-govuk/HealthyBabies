import axios from 'axios';
import {
  ServiceCategoryDto,
  ServiceCategoryListDto,
  ServiceCategoryFormQuestionDto,
  WiderServiceCategoryLookup,
} from './types';

export const serviceCategoriesCacheKey = () => ['service-categories'];
export const serviceCategoryCacheKey = (id: string) => ['service-categories', id];
export const serviceCategoryFormQuestionsCacheKey = () => ['service-category-form-questions'];
export const widerServiceCategoriesLookupCacheKey = () => ['wider-service-categories-lookup'];

export const getServiceCategories = () =>
  axios.get<ServiceCategoryListDto[]>('service-categories');

export const getServiceCategory = (id: string) =>
  axios.get<ServiceCategoryDto>(`service-categories/${id}`);

export const getServiceCategoryFormQuestions = () =>
  axios.get<ServiceCategoryFormQuestionDto[]>('service-category-form-questions');

export const getServiceCategoryFormQuestionsByStep = (step: number) =>
  axios.get<ServiceCategoryFormQuestionDto[]>(`service-category-form-questions/step/${step}`);

export const getWiderServiceCategoriesLookup = () =>
  axios.get<WiderServiceCategoryLookup[]>('global-data/entity/WIDER_SERVICE_CATEGORIES');

export type TemplateFormat = 'csv' | 'xlsx';

export const downloadServiceCategoriesTemplate = (format: TemplateFormat = 'csv') =>
  axios.get('/service-categories/bulk-upload/template', {
    params: { format },
    responseType: 'blob',
  });
