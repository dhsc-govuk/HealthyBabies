import axios from 'axios';

export interface BulkUploadLocationResult {
  rowNumber: number;
  siteName?: string;
  isSuccess: boolean;
  errorMessage?: string;
}

export interface BulkUploadLocationsResult {
  totalRows: number;
  successCount: number;
  errorCount: number;
  results: BulkUploadLocationResult[];
}

export interface ConfirmBulkUploadLocationResult {
  rowNumber: number;
  siteName?: string;
  isSaved: boolean;
  locationId?: string;
  skipReason?: string;
}

export interface ConfirmBulkUploadLocationsResult {
  totalRows: number;
  savedCount: number;
  skippedCount: number;
  results: ConfirmBulkUploadLocationResult[];
}

export type TemplateFormat = 'csv' | 'xlsx';

export const validateBulkUpload = (organisationId: string, file: File, apiBasePath?: string) => {
  const formData = new FormData();
  formData.append('file', file);

  const url = apiBasePath ? `${apiBasePath}/bulk-upload/validate` : `/organisations/${organisationId}/locations/bulk-upload/validate`;
  return axios.post<BulkUploadLocationsResult>(
    url,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
};

export const confirmBulkUpload = (organisationId: string, file: File, apiBasePath?: string) => {
  const formData = new FormData();
  formData.append('file', file);

  const url = apiBasePath ? `${apiBasePath}/bulk-upload/confirm` : `/organisations/${organisationId}/locations/bulk-upload/confirm`;
  return axios.post<ConfirmBulkUploadLocationsResult>(
    url,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
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

// Bulk update types
export interface BulkUpdateLocationItemRequest {
  locationId?: string;
  name: string;
  answers: { questionCode: string; value?: string }[];
}

export interface BulkUpdateLocationsRequest {
  locations: BulkUpdateLocationItemRequest[];
}

export interface BulkUpdateLocationResult {
  locationId?: string;
  siteName: string;
  isSuccess: boolean;
  isNew: boolean;
  errorMessage?: string;
}

export interface BulkUpdateLocationsResult {
  totalCount: number;
  successCount: number;
  errorCount: number;
  results: BulkUpdateLocationResult[];
}

// Match locations by name or UPRN
export const matchLocationsByName = (organisationId: string, siteNames: string[]) =>
  axios.post<LocationMatchResult[]>(`/organisations/${organisationId}/locations/bulk-upload/match`, { siteNames });

// Bulk update locations (create new or update existing)
export const bulkUpdateLocations = (organisationId: string, request: BulkUpdateLocationsRequest) =>
  axios.post<BulkUpdateLocationsResult>(`/organisations/${organisationId}/locations/bulk-upload/update`, request);
