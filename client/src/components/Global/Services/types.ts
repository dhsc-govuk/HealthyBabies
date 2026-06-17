// Service Form Question types
export interface ServiceFormQuestionOptionDto {
  id: string;
  value: string;
  label: string;
  displayOrder: number;
}

export interface ServiceFormQuestionDto {
  id: string;
  code: string;
  label: string;
  hint?: string;
  placeholder?: string;
  questionType: number;
  step: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary?: string;
  helpText?: string;
  conditionalQuestionCode?: string;
  conditionalValue?: string;
  isActive: boolean;
  options: ServiceFormQuestionOptionDto[];
}

export enum ServiceFormQuestionType {
  Text = 0,
  Radio = 1,
  Checkbox = 2,
  Select = 3,
  Date = 4,
}

// Service Answer types
export interface ServiceAnswerDto {
  id: string;
  questionCode: string;
  questionLabel: string;
  questionHint?: string;
  questionType: number;
  step: number;
  displayOrder: number;
  value?: string;
  displayValue?: string;
  optionsSnapshot?: string;
}

export interface AnswerInputRequest {
  questionCode: string;
  value?: string;
}

// Service types
export interface ServiceDto {
  id?: string;
  organisationId: string;
  name: string;
  status: number;
  currentStep: number;
  answers: ServiceAnswerDto[];
}

export interface ServiceListDto {
  id: string;
  name: string;
  status: number;
  currentStep: number;
}

export interface CreateServiceRequest {
  name: string;
}

export interface UpdateServiceStepOneRequest {
  name: string;
  answers: AnswerInputRequest[];
  advanceStep?: boolean;
}

export interface UpdateServiceStepTwoRequest {
  answers: AnswerInputRequest[];
  advanceStep?: boolean;
}

// Enums matching backend
export enum ServiceStatus {
  Draft = 0,
  Complete = 1,
}

export const statusLabels: Record<ServiceStatus, string> = {
  [ServiceStatus.Draft]: 'Draft',
  [ServiceStatus.Complete]: 'Complete',
};
