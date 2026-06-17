import axios from 'axios';

export interface SubmissionDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
}

export interface SubmissionSectionDto {
  id: string;
  number: number;
  title: string;
  description: string | null;
  status: string;
}

export interface SubmissionFormModuleDto {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  sections: SubmissionSectionDto[];
}

export interface SubmissionDetailDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
  formModules: SubmissionFormModuleDto[];
}

export interface SectionFieldOptionDto {
  value: string;
  label: string;
  displayOrder: number;
}

export interface SectionFieldDto {
  id: string;
  code: string;
  label: string;
  helpText: string | null;
  fieldType: string;
  isRequired: boolean;
  displayOrder: number;
  value: string | null;
  options: SectionFieldOptionDto[];
}

export interface SectionDetailDto {
  id: string;
  submissionId: string;
  number: number;
  title: string;
  description: string | null;
  status: string;
  formDefinitionId: string;
  fields: SectionFieldDto[];
}

export interface SaveSectionRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export const getSubmissions = () =>
  axios.get<SubmissionDto[]>('organisation-admin/submissions');

export const getSubmission = (submissionId: string) =>
  axios.get<SubmissionDetailDto>(`organisation-admin/submissions/${submissionId}`);

export const getSection = (submissionId: string, sectionId: string) =>
  axios.get<SectionDetailDto>(`organisation-admin/submissions/${submissionId}/sections/${sectionId}`);

export const saveSection = (submissionId: string, sectionId: string, request: SaveSectionRequest) =>
  axios.post<SectionDetailDto>(`organisation-admin/submissions/${submissionId}/sections/${sectionId}`, request);

export const submitSubmission = (submissionId: string) =>
  axios.post<SubmissionDetailDto>(`organisation-admin/submissions/${submissionId}/submit`);

export interface ConditionalRule {
  showWhen?: {
    fieldKey?: string;
    equals?: string;
    notEquals?: string;
    in?: string[];
    allOf?: Array<{
      fieldKey: string;
      equals?: string;
      notEquals?: string;
    }>;
  };
  displayInline?: boolean;
  parentOption?: string;
}

export interface FieldConfiguration {
  suffix?: string;
  prefix?: string;
  width?: string;
  size?: 'small';
  inputType?: string;
  accept?: string;
  maxSize?: number;
  group?: string;
  groupLabel?: string;
  groupHint?: string;
  exclusiveOptions?: string[];
  // Validation rules
  min?: number;
  max?: number;
  maxSumField?: string; // Field code whose value is the max sum for this group
  sumGroup?: string; // Group name for sum validation (all fields in same sumGroup should sum to <= maxSumField)
  mustEqualField?: string; // Field code whose value this group must equal
}

export interface FormModuleFieldDto {
  id: string;
  code: string;
  label: string;
  helpText: string | null;
  fieldType: string;
  isRequired: boolean;
  displayOrder: number;
  stepNumber: number | null;
  value: string | null;
  conditionalRules: string | null;
  configuration: string | null;
  placeholder: string | null;
  options: SectionFieldOptionDto[];
}

export interface FormModuleSectionDto {
  id: string;
  sectionNumber: number;
  title: string;
  description: string | null;
  helpText: string | null;
  helpUrl: string | null;
}

export interface FormModuleDetailDto {
  id: string;
  submissionId: string;
  submissionName: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  startDate: string;
  endDate: string;
  formDefinitionId: string | null;
  currentStep: number;
  totalSteps: number;
  sections: FormModuleSectionDto[];
  fields: FormModuleFieldDto[];
}

export interface SaveFormModuleRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
  currentStep?: number;
}

export const getFormModule = (submissionId: string, moduleId: string) =>
  axios.get<FormModuleDetailDto>(`organisation-admin/submissions/${submissionId}/modules/${moduleId}`);

export const saveFormModule = (submissionId: string, moduleId: string, request: SaveFormModuleRequest) =>
  axios.post<FormModuleDetailDto>(`organisation-admin/submissions/${submissionId}/modules/${moduleId}`, request);

export const deleteFormModule = (submissionId: string, moduleId: string) =>
  axios.delete(`organisation-admin/submissions/${submissionId}/modules/${moduleId}`);

export interface FileUploadResultDto {
  fileName: string;
  blobUrl: string;
}

export const uploadFile = (submissionId: string, moduleId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', file);
  return axios.post<FileUploadResultDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/upload`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
};

// Service Users Module Types
export interface ServiceFormStatusDto {
  serviceId: string;
  serviceName: string;
  fundingType: string;
  status: string;
}

export interface ServiceUsersModuleDetailDto {
  id: string;
  submissionId: string;
  submissionName: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  totalServices: number;
  completedServices: number;
  services: ServiceFormStatusDto[];
}

export interface ServiceFormDetailDto {
  serviceId: string;
  serviceName: string;
  fundingType: string;
  formModuleId: string;
  status: string;
  sections: FormModuleSectionDto[];
  fields: FormModuleFieldDto[];
}

export interface SaveServiceFormRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export const getServiceUsersModule = (submissionId: string, moduleId: string) =>
  axios.get<ServiceUsersModuleDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`
  );

export const getServiceForm = (submissionId: string, moduleId: string, serviceId: string) =>
  axios.get<ServiceFormDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}`
  );

export const saveServiceForm = (
  submissionId: string,
  moduleId: string,
  serviceId: string,
  request: SaveServiceFormRequest
) =>
  axios.post<ServiceFormDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}`,
    request
  );

export const deleteServiceForm = (
  submissionId: string,
  moduleId: string,
  serviceId: string
) =>
  axios.delete(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}`
  );

// Wider Service Users Module Types
export interface WiderServiceCategoryStatusDto {
  serviceCategoryId: string;
  categoryName: string;
  status: 'Not started' | 'In progress' | 'Completed';
  userCount?: number;
}

export interface WiderServiceUsersModuleDetailDto {
  id: string;
  submissionId: string;
  submissionName: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  startDate: string;
  endDate: string;
  totalCategories: number;
  completedCategories: number;
  categories: WiderServiceCategoryStatusDto[];
}

export interface WiderServiceCategoryFormDto {
  serviceCategoryId: string;
  categoryName: string;
  status: string;
  startDate: string;
  endDate: string;
  userCount: number | null;
  label: string;
  helpText: string | null;
}

export interface SaveWiderServiceCategoryFormRequest {
  userCount: number | null;
  markComplete: boolean;
}

export const getWiderServiceUsersModule = (submissionId: string, moduleId: string) =>
  axios.get<WiderServiceUsersModuleDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services`
  );

export const getWiderServiceCategoryForm = (
  submissionId: string,
  moduleId: string,
  categoryId: string
) =>
  axios.get<WiderServiceCategoryFormDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services/${categoryId}`
  );

export const saveWiderServiceCategoryForm = (
  submissionId: string,
  moduleId: string,
  categoryId: string,
  request: SaveWiderServiceCategoryFormRequest
) =>
  axios.post<WiderServiceCategoryFormDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services/${categoryId}`,
    request
  );

// Outcome Scores Module Types
export interface OutcomeScoreRecordDto {
  recordId: string;
  anonymisedId: string;
  serviceId: string;
  serviceName: string;
  status: string;
  lastModified: string | null;
}

export interface AvailableServiceDto {
  serviceId: string;
  serviceName: string;
}

export interface ServiceRecordRequirementDto {
  serviceId: string;
  serviceName: string;
  expectedRecords: number;
  actualRecords: number;
  isComplete: boolean;
}

export interface OutcomeScoresModuleDetailDto {
  id: string;
  submissionId: string;
  submissionName: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  totalRecords: number;
  totalExpectedRecords: number;
  isSection2Complete: boolean;
  records: OutcomeScoreRecordDto[];
  availableServices: AvailableServiceDto[];
  serviceRequirements: ServiceRecordRequirementDto[];
}

export interface OutcomeScoreFormDetailDto {
  recordId: string;
  formModuleId: string;
  serviceId: string | null;
  serviceName: string | null;
  status: string;
  sections: FormModuleSectionDto[];
  fields: FormModuleFieldDto[];
  availableServices: AvailableServiceDto[];
}

export interface SaveOutcomeScoreFormRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export const getOutcomeScoresModule = (submissionId: string, moduleId: string) =>
  axios.get<OutcomeScoresModuleDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`
  );

export const createOutcomeScoreRecord = (submissionId: string, moduleId: string) =>
  axios.post<OutcomeScoreFormDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`
  );

export const getOutcomeScoreRecord = (submissionId: string, moduleId: string, recordId: string) =>
  axios.get<OutcomeScoreFormDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}`
  );

export const saveOutcomeScoreRecord = (
  submissionId: string,
  moduleId: string,
  recordId: string,
  request: SaveOutcomeScoreFormRequest
) =>
  axios.post<OutcomeScoreFormDetailDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}`,
    request
  );

export const deleteOutcomeScoreRecord = (submissionId: string, moduleId: string, recordId: string) =>
  axios.delete(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}`
  );

// Bulk Upload Types
export type TemplateFormat = 'csv' | 'xlsx';

export interface BulkUploadFieldErrorDto {
  fieldCode: string;
  fieldLabel: string;
  errorMessage: string;
}

export interface BulkUploadRowValidationDto {
  rowNumber: number;
  serviceName: string | null;
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
}

export interface BulkUploadCellEditDto {
  rowIndex: number;
  columnIndex: number;
  value: string;
}

export interface ProcessStagedBulkUploadRequestDto {
  stagingId: string;
  selectedServiceNames: string[];
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

// Bulk Upload API Functions
export const downloadBulkUploadTemplate = async (
  submissionId: string,
  moduleId: string,
  format: TemplateFormat = 'csv'
): Promise<Blob> => {
  const response = await axios.get(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/bulk-upload/template`,
    {
      params: { format },
      responseType: 'blob',
    }
  );
  return response.data;
};

export const getBulkUploadTemplateFileName = (format: TemplateFormat, moduleType: string): string => {
  const baseName = moduleType.replace(/-/g, '_');
  return format === 'xlsx' ? `${baseName}_template.xlsx` : `${baseName}_template.csv`;
};

export const getBulkUploadTemplateMimeType = (format: TemplateFormat): string => {
  return format === 'xlsx'
    ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    : 'text/csv';
};

export const validateBulkUpload = (
  submissionId: string,
  moduleId: string,
  file: File
) => {
  const formData = new FormData();
  formData.append('file', file);
  return axios.post<BulkUploadValidationResultDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/bulk-upload/validate`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
};

export const processBulkUpload = (
  submissionId: string,
  moduleId: string,
  file: File
) => {
  const formData = new FormData();
  formData.append('file', file);
  return axios.post<BulkUploadResultDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/bulk-upload`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
};

export const processStagedBulkUpload = (
  submissionId: string,
  moduleId: string,
  payload: ProcessStagedBulkUploadRequestDto
) => {
  return axios.post<BulkUploadResultDto>(
    `organisation-admin/submissions/${submissionId}/modules/${moduleId}/bulk-upload/staged`,
    payload
  );
};
