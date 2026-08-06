import axios from 'axios';
import { sanitizeCsvForUpload, zipCsvForUpload, zipFileForUpload } from '../../../../../helpers/csvUpload';

export interface BulkUploadFieldOptionDto {
  value: string;
  label: string;
}

export interface BulkUploadFieldMetadataDto {
  fieldCode: string;
  fieldType: string;
  isRequired: boolean;
  options: BulkUploadFieldOptionDto[];
  conditionalQuestionCode?: string | null;
  conditionalValue?: string | null;
}

export interface BulkUploadFieldErrorDto {
  fieldCode: string;
  fieldLabel: string;
  errorMessage: string;
}

export interface BulkUploadRowValidationDto {
  rowNumber: number;
  siteName?: string;
  isValid: boolean;
  errors: BulkUploadFieldErrorDto[];
}

export interface BulkUploadLocationsValidationResultDto {
  isValid: boolean;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  rowValidations: BulkUploadRowValidationDto[];
  fieldMetadata: BulkUploadFieldMetadataDto[];
  stagingId: string;
}

export interface BulkUploadLocationCellEdit {
  rowNumber: number;
  questionCode: string;
  value: string;
}

export interface ProcessStagedBulkUploadLocationsRequest {
  stagingId: string;
  selectedRowNumbers: number[];
  cellEdits: BulkUploadLocationCellEdit[];
}

export interface BulkUploadLocationRowResultDto {
  rowNumber: number;
  siteName?: string;
  isSuccess: boolean;
  errorMessage?: string;
  locationId?: string;
  isNew: boolean;
}

export interface BulkUploadLocationsResultDto {
  success: boolean;
  totalRows: number;
  successCount: number;
  errorCount: number;
  results: BulkUploadLocationRowResultDto[];
}

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

export const validateBulkUpload = async (organisationId: string, file: File, apiBasePath?: string) => {
  const formData = new FormData();
  formData.append('file', await toUploadFile(file));

  const url = apiBasePath ? `${apiBasePath}/bulk-upload/validate` : `/organisations/${organisationId}/locations/bulk-upload/validate`;
  return axios.post<BulkUploadLocationsValidationResultDto>(
    url,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
};

export const processStagedBulkUpload = (organisationId: string, request: ProcessStagedBulkUploadLocationsRequest, apiBasePath?: string) => {
  const url = apiBasePath ? `${apiBasePath}/bulk-upload/staged` : `/organisations/${organisationId}/locations/bulk-upload/staged`;
  return axios.post<BulkUploadLocationsResultDto>(url, request);
};

export const downloadTemplate = (organisationId: string, format: TemplateFormat = 'csv', apiBasePath?: string) => {
  const baseUrl = apiBasePath ? `${apiBasePath}/bulk-upload/template` : `/organisations/${organisationId}/locations/bulk-upload/template`;
  const url = `${baseUrl}?format=${format}`;
  return axios.get(url, {
    responseType: 'blob',
  });
};

export const getTemplateFileName = (format: TemplateFormat): string => {
  return format === 'xlsx' ? 'locations_upload_template.xlsx' : 'locations_upload_template.csv';
};

export const getTemplateMimeType = (format: TemplateFormat): string => {
  return format === 'xlsx'
    ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    : 'text/csv';
};

// Match locations types
export interface LocationMatchResult {
  searchName: string;
  locationId?: string;
  matchedName?: string;
  matchedUprn?: string;
  isActive: boolean;
  hasExistingData: boolean;
}

// Match locations by name or UPRN
export const matchLocationsByName = (organisationId: string, siteNames: string[]) =>
  axios.post<LocationMatchResult[]>(`/organisations/${organisationId}/locations/bulk-upload/match`, { siteNames });
