import axios from 'axios';
import {
  ServiceDto,
  CreateServiceRequest,
  UpdateServiceStepOneRequest,
  UpdateServiceStepTwoRequest,
} from './types';

export const createService = (request: CreateServiceRequest) =>
  axios.post<ServiceDto>('/services/create', request);

export const updateServiceStepOne = (serviceId: string, request: UpdateServiceStepOneRequest) =>
  axios.put<ServiceDto>(`/services/${serviceId}/step-one`, request);

export const updateServiceStepTwo = (serviceId: string, request: UpdateServiceStepTwoRequest) =>
  axios.put<ServiceDto>(`/services/${serviceId}/step-two`, request);

export const completeService = (serviceId: string) =>
  axios.post<ServiceDto>(`/services/${serviceId}/complete`);

export const deleteService = (serviceId: string) =>
  axios.delete(`/services/${serviceId}`);

// Bulk upload types
export interface ServiceMatchResult {
  searchName: string;
  serviceId?: string;
  matchedName?: string;
  status?: number;
  lastUpdated?: string;
  hasExistingData: boolean;
}

export interface BulkUpdateServiceItemRequest {
  serviceId?: string | null;
  name: string;
  answers: { questionCode: string; value?: string }[];
}

export interface BulkUpdateServicesRequest {
  dataCollectionId?: string | null;
  services: BulkUpdateServiceItemRequest[];
}

export interface BulkUpdateServiceResult {
  serviceId?: string;
  serviceName: string;
  isSuccess: boolean;
  isNew: boolean;
  errorMessage?: string;
}

export interface BulkUpdateServicesResult {
  totalCount: number;
  successCount: number;
  errorCount: number;
  results: BulkUpdateServiceResult[];
}

// Bulk upload mutations
export type TemplateFormat = 'csv' | 'xlsx';

export const downloadServicesTemplate = (format: TemplateFormat = 'csv') =>
  axios.get('/services/bulk-upload/template', {
    params: { format },
    responseType: 'blob',
  });

export const getServicesTemplateFileName = (format: TemplateFormat): string => {
  return format === 'xlsx' ? 'services_upload_template.xlsx' : 'services_upload_template.csv';
};

export const getServicesTemplateMimeType = (format: TemplateFormat): string => {
  return format === 'xlsx'
    ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    : 'text/csv';
};

export const matchServicesByName = (serviceNames: string[]) =>
  axios.post<ServiceMatchResult[]>('/services/bulk-upload/match', { serviceNames });

export const bulkUpdateServices = (request: BulkUpdateServicesRequest) =>
  axios.post<BulkUpdateServicesResult>('/services/bulk-upload', request);
