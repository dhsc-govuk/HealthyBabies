import { AxiosResponse } from 'axios';
import {
  BulkUploadCellEditDto,
  BulkUploadResultDto,
  BulkUploadValidationResultDto,
  ProcessStagedBulkUploadRequestDto,
  TemplateFormat,
  OutcomeScoreRowCheckDto,
  OutcomeScoreDuplicateCheckItemDto,
  ServiceUserRowCheckDto,
  ServiceUserDuplicateCheckItemDto,
  ActivityRowCheckDto,
  ActivityDuplicateCheckItemDto,
} from './dtos';

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
  rowIndex: number;
  serviceName: string;
  updateFromFile: boolean;
  hasExistingData: boolean;
  status: string;
  lastUpdated?: string;
}

export interface ParsedData {
  headers: string[];
  labels: string[];
  requiredFields: boolean[];
  rows: string[][];
  /** 1-based line number of the first data row in the source CSV. */
  dataStartLine: number;
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

export type BulkUploadModuleType = 'service-users' | 'outcome-scores' | 'activity';

// Dependency-injects the API calls a moduleType needs, so this component doesn't import a
// specific queries.ts (it's shared between OrganisationAdmin/Submissions and DeliveryPlan/Submissions,
// which hit different routes). Each check*Duplicates method is optional because a given moduleType
// only ever supplies the one matching its own row-reconciliation semantics.
export interface BulkUploadApi {
  downloadTemplate: (submissionId: string, moduleId: string, format: TemplateFormat) => Promise<Blob>;
  validate: (submissionId: string, moduleId: string, file: File) => Promise<AxiosResponse<BulkUploadValidationResultDto>>;
  processStaged: (submissionId: string, moduleId: string, payload: ProcessStagedBulkUploadRequestDto) => Promise<AxiosResponse<BulkUploadResultDto>>;
  checkOutcomeScoreDuplicates?: (submissionId: string, moduleId: string, rows: OutcomeScoreRowCheckDto[]) => Promise<AxiosResponse<OutcomeScoreDuplicateCheckItemDto[]>>;
  checkServiceUserDuplicates?: (submissionId: string, moduleId: string, rows: ServiceUserRowCheckDto[]) => Promise<AxiosResponse<ServiceUserDuplicateCheckItemDto[]>>;
  checkActivityDuplicates?: (submissionId: string, moduleId: string, rows: ActivityRowCheckDto[]) => Promise<AxiosResponse<ActivityDuplicateCheckItemDto[]>>;
}

export interface BulkUploadProps {
  moduleType: BulkUploadModuleType;
  moduleName: string;
  backUrl: string;
  api: BulkUploadApi;
}

export const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
