import { ServiceFormQuestionDto } from '../../../../components/Global/Services';

export type Delimiter = ',' | ';' | '|' | '\t';

export interface DelimiterOption {
  value: Delimiter;
  label: string;
}

export const DELIMITER_OPTIONS: DelimiterOption[] = [
  { value: ',', label: 'Comma (,)' },
  { value: ';', label: 'Semicolon (;)' },
  { value: '|', label: 'Pipe (|)' },
  { value: '\t', label: 'Tab' },
];

export interface CellError {
  rowIndex: number;
  columnIndex: number;
  questionCode: string;
  message: string;
}

export interface RowData {
  rowIndex: number;
  data: Record<string, string>;
  errors: CellError[];
  matchResult?: ServiceMatchResult;
}

export interface ServiceMatchResult {
  searchName: string;
  serviceId?: string;
  matchedName?: string;
  status?: number;
  lastUpdated?: string;
  hasExistingData: boolean;
}

export interface ServiceUpdateSelection {
  serviceId?: string;
  serviceName: string;
  updateFromFile: boolean;
  hasExistingData: boolean;
  status?: number;
  lastUpdated?: string;
  isNew: boolean;
}

export type BulkUploadStep = 'upload' | 'preview' | 'check' | 'update';

export interface BulkUploadState {
  currentStep: BulkUploadStep;
  file: File | null;
  delimiter: Delimiter;
  rawData: string[][];
  headers: string[];
  labels: string[];
  rows: RowData[];
  questions: ServiceFormQuestionDto[];
  matchResults: ServiceMatchResult[];
  updateSelections: ServiceUpdateSelection[];
  isLoading: boolean;
  error: string | null;
}

export const initialBulkUploadState: BulkUploadState = {
  currentStep: 'upload',
  file: null,
  delimiter: ',',
  rawData: [],
  headers: [],
  labels: [],
  rows: [],
  questions: [],
  matchResults: [],
  updateSelections: [],
  isLoading: false,
  error: null,
};

// API types
export interface BulkUpdateServiceItemRequest {
  serviceId?: string | null;
  name: string;
  answers: { questionCode: string; value?: string }[];
}

export interface BulkUpdateServicesRequest {
  services: BulkUpdateServiceItemRequest[];
}

export interface BulkUpdateServiceResult {
  serviceId?: string;
  serviceName: string;
  isSuccess: boolean;
  isNew: boolean;
  errorMessage?: string;
}

export interface BulkUpdateServicesResult {
  totalCount: number;
  successCount: number;
  errorCount: number;
  results: BulkUpdateServiceResult[];
}
