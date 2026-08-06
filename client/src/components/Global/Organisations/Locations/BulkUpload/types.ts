import { SiteFormQuestionDto } from '../../../SiteForms';
import { LocationMatchResult, BulkUploadLocationsResultDto, BulkUploadLocationCellEdit } from './mutations';

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
  matchResult?: LocationMatchResult;
}

export interface LocationUpdateSelection {
  locationId?: string;
  siteName: string;
  updateFromFile: boolean;
  hasExistingData: boolean;
  isActive: boolean;
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
  questions: SiteFormQuestionDto[];
  matchResults: LocationMatchResult[];
  updateSelections: LocationUpdateSelection[];
  isLoading: boolean;
  error: string | null;
  stagingId: string | null;
  editedCells: BulkUploadLocationCellEdit[];
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

export type { LocationMatchResult, BulkUploadLocationsResultDto, BulkUploadLocationCellEdit };
