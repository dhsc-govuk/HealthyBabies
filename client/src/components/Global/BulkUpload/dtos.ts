export type TemplateFormat = 'csv' | 'xlsx';

export interface BulkUploadFieldErrorDto {
  fieldCode: string;
  fieldLabel: string;
  errorMessage: string;
}

export interface BulkUploadRowValidationDto {
  rowNumber: number;
  entityName: string | null;
  isValid: boolean;
  errors: BulkUploadFieldErrorDto[];
}

export interface BulkUploadFieldOptionDto {
  value: string;
  label: string;
}

export interface BulkUploadFieldMetadataDto {
  fieldCode: string;
  fieldType: string;
  isRequired: boolean;
  options: BulkUploadFieldOptionDto[];
  conditionalRules: string | null;
  configuration: string | null;
}

export interface BulkUploadValidationResultDto {
  isValid: boolean;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  rowValidations: BulkUploadRowValidationDto[];
  fieldMetadata: BulkUploadFieldMetadataDto[];
  stagingId: string;
  // The column naming what each row is about: 'ServiceName' for service users, 'PPS01' for outcome
  // scores, the strand's configured activity-name field for activities. Comes from the server so
  // the wizard matches rows on the same column the server does.
  nameFieldCode?: string;
  moduleError?: string;
}

export interface BulkUploadCellEditDto {
  rowIndex: number;
  columnIndex: number;
  value: string;
}

export interface ProcessStagedBulkUploadRequestDto {
  stagingId: string;
  selectedRowIndices: number[];
  cellEdits: BulkUploadCellEditDto[];
}

export interface BulkUploadRowResultDto {
  rowNumber: number;
  serviceName: string | null;
  success: boolean;
  errorMessage: string | null;
}

export interface BulkUploadResultDto {
  success: boolean;
  totalRows: number;
  successfulRows: number;
  failedRows: number;
  rowResults: BulkUploadRowResultDto[];
}

export interface OutcomeScoreFieldValueCheckDto {
  fieldKey: string;
  value: string;
}

export interface OutcomeScoreRowCheckDto {
  rowIndex: number;
  serviceName: string;
  fieldValues: OutcomeScoreFieldValueCheckDto[];
}

export interface OutcomeScoreDuplicateCheckItemDto {
  rowIndex: number;
  serviceName: string;
  hasExistingData: boolean;
}

export interface ServiceUserRowCheckDto {
  rowIndex: number;
  serviceName: string;
}

export interface ServiceUserDuplicateCheckItemDto {
  rowIndex: number;
  serviceName: string;
  hasExistingData: boolean;
}

export interface ActivityRowCheckDto {
  rowIndex: number;
  activityName: string;
}

export interface ActivityDuplicateCheckItemDto {
  rowIndex: number;
  activityName: string;
  hasExistingData: boolean;
}

export const getBulkUploadTemplateFileName = (format: TemplateFormat, moduleType: string): string => {
  const baseName = moduleType.replace(/-/g, '_');
  return format === 'xlsx' ? `${baseName}_template.xlsx` : `${baseName}_template.csv`;
};

export const getBulkUploadTemplateMimeType = (format: TemplateFormat): string => {
  return format === 'xlsx'
    ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    : 'text/csv';
};
