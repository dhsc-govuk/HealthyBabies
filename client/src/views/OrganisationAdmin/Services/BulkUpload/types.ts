import { ServiceFormQuestionDto } from '../../../../components/Global/Services';
import { BulkUploadServiceCellEdit } from '../../../../components/Global/Services/mutations';

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
  stagingId: string | null;
  editedCells: BulkUploadServiceCellEdit[];
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
  stagingId: null,
  editedCells: [],
};

// The uploaded file always has 2 preamble rows (question codes, then labels) before real data
// begins - both the client's own papaparse-based parser and the server's file parser agree on
// this, so a client-local RowData.rowIndex (0-based) always maps to this fixed file row number.
export const dataRowFileOffset = 3;
