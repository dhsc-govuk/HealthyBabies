// Site Form Question types
export interface SiteFormQuestionOptionDto {
  id: string;
  value: string;
  label: string;
  displayOrder: number;
}

export interface SiteFormQuestionDto {
  id: string;
  code: string;
  label: string;
  hint?: string;
  placeholder?: string;
  questionType: number;
  displayOrder: number;
  isRequired: boolean;
  isPredefined: boolean;
  helpTextSummary?: string;
  helpText?: string;
  conditionalQuestionCode?: string;
  conditionalValue?: string;
  isActive: boolean;
  options: SiteFormQuestionOptionDto[];
}

export enum SiteFormQuestionType {
  Text = 0,
  Radio = 1,
  Checkbox = 2,
  Select = 3,
  Date = 4,
}

// Site Answer types
export interface SiteAnswerDto {
  id: string;
  questionCode: string;
  questionLabel: string;
  questionHint?: string;
  questionType: number;
  displayOrder: number;
  value?: string;
  displayValue?: string;
  optionsSnapshot?: string;
}

export interface SiteAnswerInputRequest {
  questionCode: string;
  value?: string;
}

// Location types with answers
export interface LocationWithAnswersDto {
  id: string;
  organisationId: string;
  name: string;
  postCode?: string;
  referenceNumber?: string;
  isActive: boolean;
  answers: SiteAnswerDto[];
}

export interface CreateLocationWithAnswersRequest {
  name: string;
  postCode?: string;
  referenceNumber?: string;
  answers: SiteAnswerInputRequest[];
}

export interface UpdateLocationWithAnswersRequest {
  name: string;
  postCode?: string;
  referenceNumber?: string;
  answers: SiteAnswerInputRequest[];
}

export const questionTypeLabels: Record<number, string> = {
  0: 'Text',
  1: 'Radio',
  2: 'Checkbox',
  3: 'Select',
  4: 'Date',
};
