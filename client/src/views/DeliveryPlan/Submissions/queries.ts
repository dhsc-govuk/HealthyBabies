import axios from 'axios';
import { toBulkUploadFile } from '../../../helpers/csvUpload';
import {
  TemplateFormat,
  BulkUploadValidationResultDto,
  BulkUploadResultDto,
  ProcessStagedBulkUploadRequestDto,
  ActivityRowCheckDto,
  ActivityDuplicateCheckItemDto,
} from '../../../components/Global/BulkUpload/dtos';
import { API_ROUTES } from '../../../constants/apiRoutes';
import { ReviewAnswerGroup } from '../../../components/Global/ReviewAnswerGroups';
import { ReviewActivityRow } from '../../../components/Global/ReviewActivitiesTable';

export interface DeliveryPlanSubmissionDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
}

export interface DeliveryPlanFormModuleDto {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  allowsMultipleActivities: boolean;
  /** True when DHSC flagged this section on the most recent reopen. Advisory only. */
  isFlaggedForReview: boolean;
}

export interface DeliveryPlanSubmissionDetailDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  status: string;
  daysRemaining: number;
  formModules: DeliveryPlanFormModuleDto[];
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

export interface DeliveryPlanFieldOptionDto {
  value: string;
  label: string;
  displayOrder: number;
}

export interface DeliveryPlanFieldDto {
  id: string;
  code: string;
  label: string;
  helpText: string | null;
  fieldType: string;
  isRequired: boolean;
  displayOrder: number;
  stepNumber: number | null;
  value: string | null;
  /** The question label captured when this answer was saved; absent for rows with no snapshot. */
  questionLabel?: string | null;
  conditionalRules: string | null;
  configuration: string | null;
  placeholder: string | null;
  options: DeliveryPlanFieldOptionDto[];
}

export interface DeliveryPlanSectionDto {
  id: string;
  sectionNumber: number;
  title: string;
  description: string | null;
  helpText: string | null;
  helpUrl: string | null;
}

export interface DeliveryPlanFormModuleDetailDto {
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
  currentStep: number;
  totalSteps: number;
  allowsMultipleActivities: boolean;
  sections: DeliveryPlanSectionDto[];
  fields: DeliveryPlanFieldDto[];
  /** True once the round is submitted or its window has closed — render read-only. */
  isReadOnly: boolean;
}

export interface ActivityRecordDto {
  activityId: string;
  activityName: string;
  status: string;
  lastModified: string | null;
}

export interface ActivitiesModuleDetailDto {
  id: string;
  submissionId: string;
  submissionName: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  status: string;
  allowsMultipleActivities: boolean;
  activities: ActivityRecordDto[];
  /** True once the round is submitted or its window has closed — render read-only. */
  isReadOnly: boolean;
}

export interface ActivityFormDetailDto {
  activityId: string;
  submissionId: string;
  submissionName: string;
  formModuleId: string;
  moduleName: string;
  moduleCode: string;
  sectionNumber: number;
  activityName: string;
  status: string;
  sections: DeliveryPlanSectionDto[];
  fields: DeliveryPlanFieldDto[];
  availableServices: AvailableServiceDto[];
  strandSpend: StrandSpendSummaryDto[];
  /** True once the round is submitted or its window has closed — render read-only. */
  isReadOnly: boolean;
}

/** The strand's spend position for one financial year, excluding the activity being edited. */
export interface StrandSpendSummaryDto {
  yearRange: string;
  forecastFieldKey: string;
  allocation: number;
  spentByOtherActivities: number;
  lowerBound: number;
  upperBound: number;
  remainingToUpperBound: number;
  isCap: boolean;
}

export interface AvailableServiceDto {
  serviceId: string;
  serviceName: string;
}

export interface SaveActivityFormRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
}

export interface SaveDeliveryPlanFormModuleRequest {
  fieldValues: Record<string, string | null>;
  markComplete: boolean;
  currentStep?: number;
}

export const getDeliveryPlanSubmissions = () => axios.get<DeliveryPlanSubmissionDto[]>(API_ROUTES.deliveryPlan.submissions.list);

export const getDeliveryPlanSubmission = (submissionId: string) => axios.get<DeliveryPlanSubmissionDetailDto>(API_ROUTES.deliveryPlan.submissions.byId(submissionId));

export const getDeliveryPlanFormModule = (submissionId: string, moduleId: string) =>
  axios.get<DeliveryPlanFormModuleDetailDto>(API_ROUTES.deliveryPlan.submissions.module(submissionId, moduleId));

export const saveDeliveryPlanFormModule = (submissionId: string, moduleId: string, request: SaveDeliveryPlanFormModuleRequest) =>
  axios.post<DeliveryPlanFormModuleDetailDto>(API_ROUTES.deliveryPlan.submissions.module(submissionId, moduleId), request);

export const submitDeliveryPlanSubmission = (submissionId: string) => axios.post<DeliveryPlanSubmissionDetailDto>(API_ROUTES.deliveryPlan.submissions.submit(submissionId));

export const getActivitiesModule = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivitiesModuleDetailDto>(API_ROUTES.submissionActivities.create(apiBase, submissionId, moduleId));

export const createActivity = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.post<ActivityFormDetailDto>(API_ROUTES.submissionActivities.list(apiBase, submissionId, moduleId));

export const getActivityTemplate = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivityFormDetailDto>(API_ROUTES.submissionActivities.new(apiBase, submissionId, moduleId));

export const getActivityForm = (submissionId: string, moduleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivityFormDetailDto>(API_ROUTES.submissionActivities.byId(apiBase, submissionId, moduleId, activityId));

export const saveActivityForm = (submissionId: string, moduleId: string, activityId: string, request: SaveActivityFormRequest, apiBase = 'delivery-plan') =>
  axios.post<ActivityFormDetailDto>(API_ROUTES.submissionActivities.byId(apiBase, submissionId, moduleId, activityId), request);

export const deleteActivity = (submissionId: string, moduleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.delete(API_ROUTES.submissionActivities.byId(apiBase, submissionId, moduleId, activityId));

export const downloadBulkUploadTemplate = async (submissionId: string, moduleId: string, format: TemplateFormat = 'csv', apiBase = 'delivery-plan'): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.submissionActivities.bulkUploadTemplate(apiBase, submissionId, moduleId), {
    params: { format },
    responseType: 'blob',
  });
  return response.data;
};

export const validateBulkUpload = async (submissionId: string, moduleId: string, file: File, apiBase = 'delivery-plan') => {
  const formData = new FormData();
  formData.append('file', await toBulkUploadFile(file));
  return axios.post<BulkUploadValidationResultDto>(API_ROUTES.submissionActivities.bulkUploadValidate(apiBase, submissionId, moduleId), formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

export const checkActivityDuplicates = (submissionId: string, moduleId: string, rows: ActivityRowCheckDto[], apiBase = 'delivery-plan') =>
  axios.post<ActivityDuplicateCheckItemDto[]>(API_ROUTES.submissionActivities.bulkUploadCheckDuplicates(apiBase, submissionId, moduleId), { rows });

export const processStagedBulkUpload = (submissionId: string, moduleId: string, payload: ProcessStagedBulkUploadRequestDto, apiBase = 'delivery-plan') =>
  axios.post<BulkUploadResultDto>(API_ROUTES.submissionActivities.bulkUploadStaged(apiBase, submissionId, moduleId), payload);

/** One row on the "Previous submissions" list - a past round DHSC rejected, frozen at rejection. */
export interface SubmissionVersionSummaryDto {
  versionNumber: number;
  submittedBy: string | null;
  submittedAt: string | null;
}

/** One reviewable section within a previous submission. */
export interface ReviewSectionSummary {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  allowsMultipleActivities: boolean;
}

export interface SubmissionVersionDetailDto {
  versionNumber: number;
  submittedBy: string | null;
  submittedByEmail: string | null;
  submittedAt: string | null;
  reviewNotes: string | null;
  sectionsToReview: ReviewSectionSummary[];
  sections: ReviewSectionSummary[];
}

export interface ReviewNavigationLink {
  id: string;
  name: string;
  position: number;
}

export interface ReviewNavigation {
  position: number;
  total: number;
  previous: ReviewNavigationLink | null;
  next: ReviewNavigationLink | null;
}

export interface SubmissionVersionSectionDetailDto {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  dataCollectionName: string;
  allowsMultipleActivities: boolean;
  answerGroups: ReviewAnswerGroup[];
  activities: ReviewActivityRow[];
  navigation: ReviewNavigation;
}

export interface SubmissionVersionActivityDetailDto {
  activityId: string;
  activityName: string;
  formModuleId: string;
  moduleName: string;
  sectionNumber: number;
  dataCollectionName: string;
  answerGroups: ReviewAnswerGroup[];
  navigation: ReviewNavigation;
}

export const getSubmissionVersions = (submissionId: string, apiBase = 'delivery-plan') =>
  axios.get<SubmissionVersionSummaryDto[]>(API_ROUTES.submissionVersions.list(apiBase, submissionId));

export const getSubmissionVersionDetail = (submissionId: string, versionNumber: number, apiBase = 'delivery-plan') =>
  axios.get<SubmissionVersionDetailDto>(API_ROUTES.submissionVersions.byNumber(apiBase, submissionId, versionNumber));

export const getSubmissionVersionSection = (submissionId: string, versionNumber: number, formModuleId: string, apiBase = 'delivery-plan') =>
  axios.get<SubmissionVersionSectionDetailDto>(API_ROUTES.submissionVersions.section(apiBase, submissionId, versionNumber, formModuleId));

export const getSubmissionVersionActivity = (submissionId: string, versionNumber: number, formModuleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.get<SubmissionVersionActivityDetailDto>(API_ROUTES.submissionVersions.activity(apiBase, submissionId, versionNumber, formModuleId, activityId));

export const downloadSubmissionVersion = async (submissionId: string, versionNumber: number, apiBase = 'delivery-plan'): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.submissionVersions.download(apiBase, submissionId, versionNumber), {
    responseType: 'blob',
  });
  return response.data;
};

export const getSubmissionVersionDownloadFileName = (versionNumber: number): string => `submission-version-${versionNumber}.zip`;

/**
 * Read-only view of one section/activity within the caller's own accepted Delivery Plan
 * submission - the live current data, not a frozen snapshot. Mirrors the admin review shapes.
 */
export interface ReviewSectionDetail {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  dataCollectionName: string;
  localAuthorityName: string;
  allowsMultipleActivities: boolean;
  answerGroups: ReviewAnswerGroup[];
  activities: ReviewActivityRow[];
  navigation: ReviewNavigation;
}

export interface ReviewActivityDetail {
  activityId: string;
  activityName: string;
  formModuleId: string;
  moduleName: string;
  sectionNumber: number;
  dataCollectionName: string;
  localAuthorityName: string;
  answerGroups: ReviewAnswerGroup[];
  navigation: ReviewNavigation;
}

export const getReviewSection = (submissionId: string, formModuleId: string, apiBase = 'delivery-plan') =>
  axios.get<ReviewSectionDetail>(API_ROUTES.submissionReview.section(apiBase, submissionId, formModuleId));

export const getReviewActivity = (submissionId: string, formModuleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.get<ReviewActivityDetail>(API_ROUTES.submissionReview.activity(apiBase, submissionId, formModuleId, activityId));

export const downloadSubmission = async (submissionId: string, apiBase = 'delivery-plan'): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.submissionDownload(apiBase, submissionId), {
    responseType: 'blob',
  });
  return response.data;
};

export const getSubmissionDownloadFileName = (submissionId: string): string => `submission_${submissionId}.zip`;
