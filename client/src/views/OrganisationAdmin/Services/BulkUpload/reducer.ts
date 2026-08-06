import { ServiceFormQuestionDto } from '../../../../components/Global/Services';
import { BulkUploadServiceCellEdit, BulkUploadServiceRowValidationDto } from '../../../../components/Global/Services/mutations';
import {
  BulkUploadState,
  BulkUploadStep,
  Delimiter,
  RowData,
  ServiceMatchResult,
  ServiceUpdateSelection,
  initialBulkUploadState,
  dataRowFileOffset,
} from './types';

export enum BulkUploadActionType {
  SET_STEP = 'SET_STEP',
  SET_FILE = 'SET_FILE',
  SET_DELIMITER = 'SET_DELIMITER',
  SET_RAW_DATA = 'SET_RAW_DATA',
  SET_PARSED_DATA = 'SET_PARSED_DATA',
  SET_QUESTIONS = 'SET_QUESTIONS',
  SET_ROWS = 'SET_ROWS',
  UPDATE_CELL = 'UPDATE_CELL',
  SET_MATCH_RESULTS = 'SET_MATCH_RESULTS',
  SET_UPDATE_SELECTIONS = 'SET_UPDATE_SELECTIONS',
  TOGGLE_UPDATE_SELECTION = 'TOGGLE_UPDATE_SELECTION',
  SET_LOADING = 'SET_LOADING',
  SET_ERROR = 'SET_ERROR',
  SET_STAGING_ID = 'SET_STAGING_ID',
  MERGE_ROW_VALIDATIONS = 'MERGE_ROW_VALIDATIONS',
  RESET = 'RESET',
}

export type BulkUploadAction =
  | { type: BulkUploadActionType.SET_STEP; payload: BulkUploadStep }
  | { type: BulkUploadActionType.SET_FILE; payload: File | null }
  | { type: BulkUploadActionType.SET_DELIMITER; payload: Delimiter }
  | { type: BulkUploadActionType.SET_RAW_DATA; payload: string[][] }
  | {
      type: BulkUploadActionType.SET_PARSED_DATA;
      payload: { headers: string[]; labels: string[]; rows: RowData[] };
    }
  | { type: BulkUploadActionType.SET_QUESTIONS; payload: ServiceFormQuestionDto[] }
  | { type: BulkUploadActionType.SET_ROWS; payload: RowData[] }
  | {
      type: BulkUploadActionType.UPDATE_CELL;
      payload: { rowIndex: number; questionCode: string; value: string };
    }
  | { type: BulkUploadActionType.SET_MATCH_RESULTS; payload: ServiceMatchResult[] }
  | { type: BulkUploadActionType.SET_UPDATE_SELECTIONS; payload: ServiceUpdateSelection[] }
  | { type: BulkUploadActionType.TOGGLE_UPDATE_SELECTION; payload: { serviceName: string; updateFromFile: boolean } }
  | { type: BulkUploadActionType.SET_LOADING; payload: boolean }
  | { type: BulkUploadActionType.SET_ERROR; payload: string | null }
  | { type: BulkUploadActionType.SET_STAGING_ID; payload: string }
  | { type: BulkUploadActionType.MERGE_ROW_VALIDATIONS; payload: { rowValidations: BulkUploadServiceRowValidationDto[]; questions: ServiceFormQuestionDto[] } }
  | { type: BulkUploadActionType.RESET };

export const bulkUploadReducer = (
  state: BulkUploadState,
  action: BulkUploadAction
): BulkUploadState => {
  switch (action.type) {
    case BulkUploadActionType.SET_STEP:
      return { ...state, currentStep: action.payload };

    case BulkUploadActionType.SET_FILE:
      return { ...state, file: action.payload, error: null };

    case BulkUploadActionType.SET_DELIMITER:
      return { ...state, delimiter: action.payload };

    case BulkUploadActionType.SET_RAW_DATA:
      return { ...state, rawData: action.payload };

    case BulkUploadActionType.SET_PARSED_DATA:
      return {
        ...state,
        headers: action.payload.headers,
        labels: action.payload.labels,
        rows: action.payload.rows,
      };

    case BulkUploadActionType.SET_QUESTIONS:
      return { ...state, questions: action.payload };

    case BulkUploadActionType.SET_ROWS:
      return { ...state, rows: action.payload };

    case BulkUploadActionType.UPDATE_CELL: {
      const { rowIndex, questionCode, value } = action.payload;
      const updatedRows = state.rows.map((row) => {
        if (row.rowIndex === rowIndex) {
          return {
            ...row,
            data: {
              ...row.data,
              [questionCode]: value,
            },
          };
        }
        return row;
      });

      // Track edited cells as a diff (rowNumber + questionCode + value), so the confirm step can
      // send just what changed instead of re-uploading the whole file.
      const rowNumber = rowIndex + dataRowFileOffset;
      const existingEditIndex = state.editedCells.findIndex(
        (edit) => edit.rowNumber === rowNumber && edit.questionCode === questionCode
      );
      const editedCells = [...state.editedCells];
      const newEdit: BulkUploadServiceCellEdit = { rowNumber, questionCode, value };
      if (existingEditIndex >= 0) {
        editedCells[existingEditIndex] = newEdit;
      } else {
        editedCells.push(newEdit);
      }

      return { ...state, rows: updatedRows, editedCells };
    }

    case BulkUploadActionType.SET_MATCH_RESULTS:
      return { ...state, matchResults: action.payload };

    case BulkUploadActionType.SET_UPDATE_SELECTIONS:
      return { ...state, updateSelections: action.payload };

    case BulkUploadActionType.TOGGLE_UPDATE_SELECTION: {
      const { serviceName, updateFromFile } = action.payload;
      const updatedSelections = state.updateSelections.map((selection) => {
        if (selection.serviceName === serviceName) {
          return { ...selection, updateFromFile };
        }
        return selection;
      });
      return { ...state, updateSelections: updatedSelections };
    }

    case BulkUploadActionType.SET_LOADING:
      return { ...state, isLoading: action.payload };

    case BulkUploadActionType.SET_ERROR:
      return { ...state, error: action.payload };

    case BulkUploadActionType.SET_STAGING_ID:
      return { ...state, stagingId: action.payload };

    case BulkUploadActionType.MERGE_ROW_VALIDATIONS: {
      const { rowValidations, questions } = action.payload;
      const updatedRows = state.rows.map((row) => {
        const rowNumber = row.rowIndex + dataRowFileOffset;
        const serverRow = rowValidations.find((rv) => rv.rowNumber === rowNumber);
        if (!serverRow || serverRow.isValid) {
          return row;
        }

        const existingMessages = new Set(row.errors.map((e) => `${e.questionCode}:${e.message}`));
        const serverErrors = serverRow.errors
          .filter((e) => !existingMessages.has(`${e.fieldCode}:${e.errorMessage}`))
          .map((e) => ({
            rowIndex: row.rowIndex,
            columnIndex: questions.findIndex((q) => q.code === e.fieldCode),
            questionCode: e.fieldCode,
            message: e.errorMessage,
          }));

        return serverErrors.length > 0 ? { ...row, errors: [...row.errors, ...serverErrors] } : row;
      });

      return { ...state, rows: updatedRows };
    }

    case BulkUploadActionType.RESET:
      return { ...initialBulkUploadState };

    default:
      return state;
  }
};
