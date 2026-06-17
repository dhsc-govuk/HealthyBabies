import {
  BulkUploadState,
  UploadStep,
  Delimiter,
  ParsedData,
  CellError,
  ServiceSelection,
  initialBulkUploadState,
} from './types';
import { BulkUploadValidationResultDto, BulkUploadResultDto } from '../queries';

export enum BulkUploadActionType {
  SET_STEP = 'SET_STEP',
  SET_FILE = 'SET_FILE',
  SET_FILE_ERROR = 'SET_FILE_ERROR',
  SET_DELIMITER = 'SET_DELIMITER',
  SET_PARSED_DATA = 'SET_PARSED_DATA',
  SET_FILE_CONTENT = 'SET_FILE_CONTENT',
  SET_VALIDATION_RESULT = 'SET_VALIDATION_RESULT',
  SET_PROCESS_RESULT = 'SET_PROCESS_RESULT',
  SET_CELL_ERRORS = 'SET_CELL_ERRORS',
  SET_SHOW_ERRORS_ONLY = 'SET_SHOW_ERRORS_ONLY',
  SET_EDITING_CELL = 'SET_EDITING_CELL',
  UPDATE_CELL = 'UPDATE_CELL',
  SET_SERVICE_SELECTIONS = 'SET_SERVICE_SELECTIONS',
  TOGGLE_SERVICE_SELECTION = 'TOGGLE_SERVICE_SELECTION',
  SET_IS_DOWNLOADING = 'SET_IS_DOWNLOADING',
  SET_FULL_SCREEN_OPEN = 'SET_FULL_SCREEN_OPEN',
  SET_STAGING_ID = 'SET_STAGING_ID',
  RESET = 'RESET',
}

export type BulkUploadAction =
  | { type: BulkUploadActionType.SET_STEP; payload: UploadStep }
  | { type: BulkUploadActionType.SET_FILE; payload: File | null }
  | { type: BulkUploadActionType.SET_FILE_ERROR; payload: string | null }
  | { type: BulkUploadActionType.SET_DELIMITER; payload: Delimiter }
  | { type: BulkUploadActionType.SET_PARSED_DATA; payload: ParsedData | null }
  | { type: BulkUploadActionType.SET_FILE_CONTENT; payload: string }
  | { type: BulkUploadActionType.SET_VALIDATION_RESULT; payload: BulkUploadValidationResultDto | null }
  | { type: BulkUploadActionType.SET_PROCESS_RESULT; payload: BulkUploadResultDto | null }
  | { type: BulkUploadActionType.SET_CELL_ERRORS; payload: CellError[] }
  | { type: BulkUploadActionType.SET_SHOW_ERRORS_ONLY; payload: boolean }
  | { type: BulkUploadActionType.SET_EDITING_CELL; payload: { rowIndex: number; cellIndex: number } | null }
  | { type: BulkUploadActionType.UPDATE_CELL; payload: { rowIndex: number; cellIndex: number; value: string } }
  | { type: BulkUploadActionType.SET_SERVICE_SELECTIONS; payload: ServiceSelection[] }
  | { type: BulkUploadActionType.TOGGLE_SERVICE_SELECTION; payload: { serviceName: string; updateFromFile: boolean } }
  | { type: BulkUploadActionType.SET_IS_DOWNLOADING; payload: boolean }
  | { type: BulkUploadActionType.SET_FULL_SCREEN_OPEN; payload: boolean }
  | { type: BulkUploadActionType.SET_STAGING_ID; payload: string | null }
  | { type: BulkUploadActionType.RESET };

export const bulkUploadReducer = (
  state: BulkUploadState,
  action: BulkUploadAction
): BulkUploadState => {
  switch (action.type) {
    case BulkUploadActionType.SET_STEP:
      return { ...state, currentStep: action.payload };

    case BulkUploadActionType.SET_FILE:
      return { ...state, file: action.payload, fileError: null };

    case BulkUploadActionType.SET_FILE_ERROR:
      return { ...state, fileError: action.payload };

    case BulkUploadActionType.SET_DELIMITER:
      return { ...state, delimiter: action.payload };

    case BulkUploadActionType.SET_PARSED_DATA:
      return { ...state, parsedData: action.payload };

    case BulkUploadActionType.SET_FILE_CONTENT:
      return { ...state, fileContent: action.payload };

    case BulkUploadActionType.SET_VALIDATION_RESULT:
      return { ...state, validationResult: action.payload };

    case BulkUploadActionType.SET_PROCESS_RESULT:
      return { ...state, processResult: action.payload };

    case BulkUploadActionType.SET_CELL_ERRORS:
      return { ...state, cellErrors: action.payload };

    case BulkUploadActionType.SET_SHOW_ERRORS_ONLY:
      return { ...state, showErrorsOnly: action.payload };

    case BulkUploadActionType.SET_EDITING_CELL:
      return { ...state, editingCell: action.payload };

    case BulkUploadActionType.UPDATE_CELL: {
      const { rowIndex, cellIndex, value } = action.payload;
      if (!state.parsedData) return state;
      const updatedRows = state.parsedData.rows.map((row, idx) => {
        if (idx === rowIndex) {
          const newRow = [...row];
          newRow[cellIndex] = value;
          return newRow;
        }
        return row;
      });
      const filteredEdits = state.editedCells.filter(
        (edit) => !(edit.rowIndex === rowIndex && edit.columnIndex === cellIndex)
      );
      return {
        ...state,
        parsedData: { ...state.parsedData, rows: updatedRows },
        editedCells: [...filteredEdits, { rowIndex, columnIndex: cellIndex, value }],
      };
    }

    case BulkUploadActionType.SET_SERVICE_SELECTIONS:
      return { ...state, serviceSelections: action.payload };

    case BulkUploadActionType.TOGGLE_SERVICE_SELECTION: {
      const { serviceName, updateFromFile } = action.payload;
      const updatedSelections = state.serviceSelections.map((selection) => {
        if (selection.serviceName === serviceName) {
          return { ...selection, updateFromFile };
        }
        return selection;
      });
      return { ...state, serviceSelections: updatedSelections };
    }

    case BulkUploadActionType.SET_IS_DOWNLOADING:
      return { ...state, isDownloading: action.payload };

    case BulkUploadActionType.SET_FULL_SCREEN_OPEN:
      return { ...state, fullScreenOpen: action.payload };

    case BulkUploadActionType.SET_STAGING_ID:
      return { ...state, stagingId: action.payload };

    case BulkUploadActionType.RESET:
      return { ...initialBulkUploadState };

    default:
      return state;
  }
};
