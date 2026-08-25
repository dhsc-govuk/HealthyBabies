import axios from 'axios';
import { toBulkUploadFile } from '../../../helpers/csvUpload';
import {
  TemplateFormat,
  BulkUploadFieldErrorDto,
  BulkUploadRowValidationDto,
  BulkUploadFieldOptionDto,
  BulkUploadFieldMetadataDto,
  BulkUploadValidationResultDto,
  BulkUploadCellEditDto,
  ProcessStagedBulkUploadRequestDto,
  BulkUploadRowResultDto,
  BulkUploadResultDto,
  getBulkUploadTemplateFileName,
  OutcomeScoreFieldValueCheckDto,
  OutcomeScoreRowCheckDto,
  OutcomeScoreDuplicateCheckItemDto,
  ServiceUserRowCheckDto,
  ServiceUserDuplicateCheckItemDto,
} from '../../../components/Global/BulkUpload/dtos';
import { API_ROUTES } from '../../../constants/apiRoutes';

export type {
  TemplateFormat,
  BulkUploadFieldErrorDto,
  BulkUploadRowValidationDto,
  BulkUploadFieldOptionDto,
  BulkUploadFieldMetadataDto,
  BulkUploadValidationResultDto,
  BulkUploadCellEditDto,
  ProcessStagedBulkUploadRequestDto,
  BulkUploadRowResultDto,
  BulkUploadResultDto,
  OutcomeScoreFieldValueCheckDto,
  OutcomeScoreRowCheckDto,
  OutcomeScoreDuplicateCheckItemDto,
  ServiceUserRowCheckDto,
  ServiceUserDuplicateCheckItemDto,
};
export { getBulkUploadTemplateFileName };

export interface SubmissionDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
  isWindowClosed: boolean;
  type: DataCollectionTypeValue;
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
  allowsMultipleActivities: boolean;
  sections: SubmissionSectionDto[];
  /** True when DHSC flagged this section on the most recent reopen. Advisory only. */
  isFlaggedForReview: boolean;
}

export type DataCollectionTypeValue = 'delivery-plan' | 'management-information';

/** One reviewable section of a submitted Delivery Plan. Empty for management information. */
export interface ReviewSectionSummary {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  allowsMultipleActivities: boolean;
}

export interface SubmissionDetailDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
  isWindowClosed: boolean;
  type: DataCollectionTypeValue;
  formModules: SubmissionFormModuleDto[];
  /** Number of previous submissions DHSC has rejected. 0 means "View previous submissions" should not show. */
  previousVersionCount: number;
  /** Set when DHSC has accepted or reopened this plan. */
  reviewedAt: string | null;
  /** The reason DHSC gave for reopening. Null when the plan was accepted. */
  reviewNotes: string | null;
  /** Set once DHSC has made a decision - who accepted or reopened it. */
  reviewedBy: string | null;
  submittedBy: string | null;
  submittedByEmail: string | null;
  submittedAt: string | null;
  /** The read-only section list shown once the plan is Accepted. Empty until then. */
  sections: ReviewSectionSummary[];
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
  displayValue?: string | null;
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

export const getSubmissions = () => axios.get<SubmissionDto[]>(API_ROUTES.organisationAdmin.submissions.list);

export const getSubmission = (submissionId: string) => axios.get<SubmissionDetailDto>(API_ROUTES.organisationAdmin.submissions.byId(submissionId));

export const getSection = (submissionId: string, sectionId: string) => axios.get<SectionDetailDto>(API_ROUTES.organisationAdmin.submissions.section(submissionId, sectionId));

export const saveSection = (submissionId: string, sectionId: string, request: SaveSectionRequest) =>
  axios.post<SectionDetailDto>(API_ROUTES.organisationAdmin.submissions.section(submissionId, sectionId), request);

export const submitSubmission = (submissionId: string) => axios.post<SubmissionDetailDto>(API_ROUTES.organisationAdmin.submissions.submit(submissionId));

export const reopenSubmission = (submissionId: string) => axios.post<SubmissionDetailDto>(API_ROUTES.organisationAdmin.submissions.reopen(submissionId));

export const downloadSubmission = async (submissionId: string): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.submissionDownload('organisation-admin', submissionId), {
    responseType: 'blob',
  });
  return response.data;
};

export const getSubmissionDownloadFileName = (submissionId: string): string => `submission_${submissionId}.zip`;

export interface ConditionalRule {
  showWhen?: {
    fieldKey?: string;
    equals?: string;
    notEquals?: string;
    gte?: string;
    in?: string[];
    allOf?: Array<{
      fieldKey: string;
      equals?: string;
      notEquals?: string;
      gte?: string;
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
  formula?: string; // Arithmetic formula for calculation fields, e.g. "DG09+DG11"
  warnIfNegative?: boolean; // Show overspend warning when calculated value is negative
  dateRule?: 'not_in_future' | 'not_in_past'; // Date fields: constrain relative to today
  notBefore?: string; // Date fields: this date must not fall before the named field's date
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
  displayValue?: string | null;
  /** The question label captured when this answer was saved; absent for rows with no snapshot. */
  questionLabel?: string | null;
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

export const getFormModule = (submissionId: string, moduleId: string) => axios.get<FormModuleDetailDto>(API_ROUTES.organisationAdmin.submissions.module(submissionId, moduleId));

export const saveFormModule = (submissionId: string, moduleId: string, request: SaveFormModuleRequest) =>
  axios.post<FormModuleDetailDto>(API_ROUTES.organisationAdmin.submissions.module(submissionId, moduleId), request);

export const deleteFormModule = (submissionId: string, moduleId: string) => axios.delete(API_ROUTES.organisationAdmin.submissions.module(submissionId, moduleId));

export interface FileUploadResultDto {
  fileName: string;
  blobUrl: string;
}

export const uploadFile = (submissionId: string, moduleId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', file);
  return axios.post<FileUploadResultDto>(API_ROUTES.organisationAdmin.submissions.moduleUpload(submissionId, moduleId), formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
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
  serviceAnswers: Record<string, string>;
}

export interface SaveServiceFormRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export const getServiceUsersModule = (submissionId: string, moduleId: string) =>
  axios.get<ServiceUsersModuleDetailDto>(API_ROUTES.organisationAdmin.submissions.services(submissionId, moduleId));

export const getServiceForm = (submissionId: string, moduleId: string, serviceId: string) =>
  axios.get<ServiceFormDetailDto>(API_ROUTES.organisationAdmin.submissions.service(submissionId, moduleId, serviceId));

export const saveServiceForm = (submissionId: string, moduleId: string, serviceId: string, request: SaveServiceFormRequest) =>
  axios.post<ServiceFormDetailDto>(API_ROUTES.organisationAdmin.submissions.service(submissionId, moduleId, serviceId), request);

export const deleteServiceForm = (submissionId: string, moduleId: string, serviceId: string) =>
  axios.delete(API_ROUTES.organisationAdmin.submissions.service(submissionId, moduleId, serviceId));

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
  sections: FormModuleSectionDto[];
  fields: FormModuleFieldDto[];
}

export interface SaveWiderServiceCategoryFormRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export const getWiderServiceUsersModule = (submissionId: string, moduleId: string) =>
  axios.get<WiderServiceUsersModuleDetailDto>(API_ROUTES.organisationAdmin.submissions.widerServices(submissionId, moduleId));

export const getWiderServiceCategoryForm = (submissionId: string, moduleId: string, categoryId: string) =>
  axios.get<WiderServiceCategoryFormDto>(API_ROUTES.organisationAdmin.submissions.widerService(submissionId, moduleId, categoryId));

export const saveWiderServiceCategoryForm = (submissionId: string, moduleId: string, categoryId: string, request: SaveWiderServiceCategoryFormRequest) =>
  axios.post<WiderServiceCategoryFormDto>(API_ROUTES.organisationAdmin.submissions.widerService(submissionId, moduleId, categoryId), request);

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
  axios.get<OutcomeScoresModuleDetailDto>(API_ROUTES.organisationAdmin.submissions.outcomeScores(submissionId, moduleId));

export const createOutcomeScoreRecord = (submissionId: string, moduleId: string) =>
  axios.post<OutcomeScoreFormDetailDto>(API_ROUTES.organisationAdmin.submissions.outcomeScores(submissionId, moduleId));

export const getOutcomeScoreRecord = (submissionId: string, moduleId: string, recordId: string) =>
  axios.get<OutcomeScoreFormDetailDto>(API_ROUTES.organisationAdmin.submissions.outcomeScore(submissionId, moduleId, recordId));

export const saveOutcomeScoreRecord = (submissionId: string, moduleId: string, recordId: string, request: SaveOutcomeScoreFormRequest) =>
  axios.post<OutcomeScoreFormDetailDto>(API_ROUTES.organisationAdmin.submissions.outcomeScore(submissionId, moduleId, recordId), request);

export const deleteOutcomeScoreRecord = (submissionId: string, moduleId: string, recordId: string) =>
  axios.delete(API_ROUTES.organisationAdmin.submissions.outcomeScore(submissionId, moduleId, recordId));

// Exports the module's actual submitted records as CSV (BLUM-7361) - distinct from
// downloadBulkUploadTemplate below, which generates a blank/incomplete-rows-only upload template.
export const downloadModuleExportCsv = async (submissionId: string, moduleId: string): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.organisationAdmin.submissions.exportCsv(submissionId, moduleId), {
    responseType: 'blob',
  });
  return response.data;
};

// Bulk Upload API Functions
export const downloadBulkUploadTemplate = async (submissionId: string, moduleId: string, format: TemplateFormat = 'csv'): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.organisationAdmin.submissions.bulkUploadTemplate(submissionId, moduleId), {
    params: { format },
    responseType: 'blob',
  });
  return response.data;
};

export const validateBulkUpload = async (submissionId: string, moduleId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', await toBulkUploadFile(file));
  return axios.post<BulkUploadValidationResultDto>(API_ROUTES.organisationAdmin.submissions.bulkUploadValidate(submissionId, moduleId), formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

export const processBulkUpload = async (submissionId: string, moduleId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', await toBulkUploadFile(file));
  return axios.post<BulkUploadResultDto>(API_ROUTES.organisationAdmin.submissions.bulkUpload(submissionId, moduleId), formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

export const checkServiceUserDuplicates = (submissionId: string, moduleId: string, rows: ServiceUserRowCheckDto[]) =>
  axios.post<ServiceUserDuplicateCheckItemDto[]>(API_ROUTES.organisationAdmin.submissions.bulkUploadCheckServiceUserDuplicates(submissionId, moduleId), { rows });

export const checkOutcomeScoreDuplicates = (submissionId: string, moduleId: string, rows: OutcomeScoreRowCheckDto[]) =>
  axios.post<OutcomeScoreDuplicateCheckItemDto[]>(API_ROUTES.organisationAdmin.submissions.bulkUploadCheckOutcomeScoreDuplicates(submissionId, moduleId), { rows });

export const processStagedBulkUpload = (submissionId: string, moduleId: string, payload: ProcessStagedBulkUploadRequestDto) => {
  return axios.post<BulkUploadResultDto>(API_ROUTES.organisationAdmin.submissions.bulkUploadStaged(submissionId, moduleId), payload);
};
