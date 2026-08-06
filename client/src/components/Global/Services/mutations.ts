import axios from 'axios';
import {
  ServiceDto,
  CreateServiceRequest,
  UpdateServiceStepOneRequest,
  UpdateServiceStepTwoRequest,
} from './types';
import { sanitizeCsvForUpload, zipCsvForUpload, zipFileForUpload } from '../../../helpers/csvUpload';

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

export interface BulkUploadServiceFieldOptionDto {
  value: string;
  label: string;
}

export interface BulkUploadServiceFieldMetadataDto {
  fieldCode: string;
  fieldType: string;
  isRequired: boolean;
  options: BulkUploadServiceFieldOptionDto[];
  conditionalQuestionCode?: string | null;
  conditionalValue?: string | null;
}

export interface BulkUploadServiceFieldErrorDto {
  fieldCode: string;
  fieldLabel: string;
  errorMessage: string;
}

export interface BulkUploadServiceRowValidationDto {
  rowNumber: number;
  serviceName?: string;
  isValid: boolean;
  errors: BulkUploadServiceFieldErrorDto[];
}

export interface BulkUploadServicesValidationResultDto {
  isValid: boolean;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  rowValidations: BulkUploadServiceRowValidationDto[];
  fieldMetadata: BulkUploadServiceFieldMetadataDto[];
  stagingId: string;
}

export interface BulkUploadServiceCellEdit {
  rowNumber: number;
  questionCode: string;
  value: string;
}

export interface ProcessStagedBulkUploadServicesRequest {
  stagingId: string;
  selectedRowNumbers: number[];
  cellEdits: BulkUploadServiceCellEdit[];
}

export interface BulkUploadServiceRowResultDto {
  rowNumber: number;
  serviceName?: string;
  isSuccess: boolean;
  errorMessage?: string;
  serviceId?: string;
  isNew: boolean;
}

export interface BulkUploadServicesResultDto {
  success: boolean;
  totalRows: number;
  successCount: number;
  errorCount: number;
  results: BulkUploadServiceRowResultDto[];
}

// Bulk upload mutations
export type TemplateFormat = 'csv' | 'xlsx';

// Wraps the picked file in a zip before upload - see client/src/helpers/csvUpload.ts. CSV files are
// text-sanitized first (WAF-signature characters, empty-cell comma runs); the binary .xlsx template
// only needs the zip wrapper since it has no free-text-in-plain-CSV WAF risk to begin with.
const toUploadFile = async (file: File): Promise<File> => {
  const lowerName = file.name.toLowerCase();

  if (lowerName.endsWith('.csv')) {
    const sanitizedContent = sanitizeCsvForUpload(await file.text());
    const zippedContent = zipCsvForUpload(sanitizedContent, file.name);
    const zipFileName = `${file.name.slice(0, -'.csv'.length)}.zip`;
    return new File([zippedContent], zipFileName, { type: 'application/zip' });
  }

  if (lowerName.endsWith('.xlsx')) {
    const bytes = new Uint8Array(await file.arrayBuffer());
    const zippedContent = zipFileForUpload(bytes, file.name);
    const zipFileName = `${file.name.slice(0, -'.xlsx'.length)}.zip`;
    return new File([zippedContent], zipFileName, { type: 'application/zip' });
  }

  return file;
};

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

export const validateBulkUpload = async (file: File) => {
  const formData = new FormData();
  formData.append('file', await toUploadFile(file));

  return axios.post<BulkUploadServicesValidationResultDto>('/services/bulk-upload/validate', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

export const processStagedBulkUpload = (request: ProcessStagedBulkUploadServicesRequest) =>
  axios.post<BulkUploadServicesResultDto>('/services/bulk-upload/staged', request);
