export interface ServiceCategoryDto {
  id: string | null;
  organisationId: string;
  categoryCode: string;
  categoryName: string;
  status: ServiceCategoryStatus;
  currentStep: number;
  answers: ServiceCategoryAnswerDto[];
}

export interface ServiceCategoryListDto {
  id: string;
  categoryCode: string;
  categoryName: string;
  status: ServiceCategoryStatus;
  currentStep: number;
}

export interface ServiceCategoryAnswerDto {
  id: string;
  questionCode: string;
  questionLabel: string;
  questionHint: string | null;
  questionType: number;
  step: number;
  displayOrder: number;
  value: string | null;
  displayValue: string | null;
  optionsSnapshot: string | null;
}

export interface ServiceCategoryFormQuestionDto {
  id: string;
  code: string;
  label: string;
  hint: string | null;
  placeholder: string | null;
  questionType: ServiceCategoryFormQuestionType;
  step: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary: string | null;
  helpText: string | null;
  conditionalQuestionCode: string | null;
  conditionalValue: string | null;
  isActive: boolean;
  options: ServiceCategoryFormQuestionOptionDto[];
}

export interface ServiceCategoryFormQuestionOptionDto {
  id: string;
  value: string;
  label: string;
  displayOrder: number;
}

export interface WiderServiceCategoryLookup {
  id: string;
  entity: string;
  value: string;
  description: string;
}

export enum ServiceCategoryStatus {
  Draft = 0,
  Complete = 1,
}

export enum ServiceCategoryFormQuestionType {
  Text = 0,
  Radio = 1,
  Checkbox = 2,
  Select = 3,
  Date = 4,
}

export interface CreateServiceCategoryRequest {
  categoryCode: string;
  categoryName: string;
}

export interface ServiceCategoryAnswerInputRequest {
  questionCode: string;
  value: string | null;
}

export interface UpdateServiceCategoryStepOneRequest {
  answers: ServiceCategoryAnswerInputRequest[];
  advanceStep: boolean;
}
