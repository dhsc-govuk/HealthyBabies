import axios from 'axios';

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

export const getDeliveryPlanSubmissions = () =>
  axios.get<DeliveryPlanSubmissionDto[]>('delivery-plan/submissions');

export const getDeliveryPlanSubmission = (submissionId: string) =>
  axios.get<DeliveryPlanSubmissionDetailDto>(`delivery-plan/submissions/${submissionId}`);

export const getDeliveryPlanFormModule = (submissionId: string, moduleId: string) =>
  axios.get<DeliveryPlanFormModuleDetailDto>(`delivery-plan/submissions/${submissionId}/modules/${moduleId}`);

export const saveDeliveryPlanFormModule = (submissionId: string, moduleId: string, request: SaveDeliveryPlanFormModuleRequest) =>
  axios.post<DeliveryPlanFormModuleDetailDto>(`delivery-plan/submissions/${submissionId}/modules/${moduleId}`, request);

export const submitDeliveryPlanSubmission = (submissionId: string) =>
  axios.post<DeliveryPlanSubmissionDetailDto>(`delivery-plan/submissions/${submissionId}/submit`);

export const getActivitiesModule = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivitiesModuleDetailDto>(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities`);

export const createActivity = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.post<ActivityFormDetailDto>(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities`);

export const getActivityTemplate = (submissionId: string, moduleId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivityFormDetailDto>(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities/new`);

export const getActivityForm = (submissionId: string, moduleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.get<ActivityFormDetailDto>(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}`);

export const saveActivityForm = (submissionId: string, moduleId: string, activityId: string, request: SaveActivityFormRequest, apiBase = 'delivery-plan') =>
  axios.post<ActivityFormDetailDto>(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}`, request);

export const deleteActivity = (submissionId: string, moduleId: string, activityId: string, apiBase = 'delivery-plan') =>
  axios.delete(`${apiBase}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}`);
