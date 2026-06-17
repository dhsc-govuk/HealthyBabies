import { BulkUploadCellEditDto, BulkUploadResultDto, BulkUploadValidationResultDto } from '../queries';

export type Delimiter = ',' | ';' | '|' | '\t';

export interface DelimiterOption {
  value: Delimiter;
  label: string;
}

export const DELIMITER_OPTIONS: DelimiterOption[] = [
  { value: ',', label: 'Comma\u00A0(,)' },
  { value: ';', label: 'Semicolon\u00A0(;)' },
  { value: '|', label: 'Pipe\u00A0(|)' },
  { value: '\t', label: 'Tab' },
];

export type UploadStep = 'upload' | 'preview' | 'check' | 'select' | 'processing' | 'complete';

export interface CellError {
  rowIndex: number;
  cellIndex: number;
  fieldCode: string;
  message: string;
}

export interface ServiceSelection {
  serviceName: string;
  updateFromFile: boolean;
  status: string;
  lastUpdated?: string;
}

export interface ParsedData {
  headers: string[];
  labels: string[];
  requiredFields: boolean[];
  rows: string[][];
}

export interface BulkUploadState {
  currentStep: UploadStep;
  file: File | null;
  delimiter: Delimiter;
  fileError: string | null;
  parsedData: ParsedData | null;
  fileContent: string;
  validationResult: BulkUploadValidationResultDto | null;
  processResult: BulkUploadResultDto | null;
  cellErrors: CellError[];
  showErrorsOnly: boolean;
  editingCell: { rowIndex: number; cellIndex: number } | null;
  serviceSelections: ServiceSelection[];
  isDownloading: boolean;
  fullScreenOpen: boolean;
  stagingId: string | null;
  editedCells: BulkUploadCellEditDto[];
}

export const initialBulkUploadState: BulkUploadState = {
  currentStep: 'upload',
  file: null,
  delimiter: ',',
  fileError: null,
  parsedData: null,
  fileContent: '',
  validationResult: null,
  processResult: null,
  cellErrors: [],
  showErrorsOnly: false,
  editingCell: null,
  serviceSelections: [],
  isDownloading: false,
  fullScreenOpen: false,
  stagingId: null,
  editedCells: [],
};

export interface BulkUploadProps {
  moduleType: 'service-users' | 'outcome-scores';
  moduleName: string;
  backUrl: string;
}

export const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
