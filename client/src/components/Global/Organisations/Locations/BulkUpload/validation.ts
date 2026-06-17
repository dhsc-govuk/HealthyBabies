import {
  SiteFormQuestionDto,
  SiteFormQuestionOptionDto,
  SiteFormQuestionType,
} from '../../../SiteForms';
import { CellError, RowData } from './types';
import { LocationMatchResult } from './mutations';

const PREDEFINED_NAME_CODE = 'FHS01';

/**
 * Finds an option by matching either value or label (case-insensitive)
 */
const findOptionMatch = (
  options: SiteFormQuestionOptionDto[],
  inputValue: string
): SiteFormQuestionOptionDto | undefined => {
  const normalizedInput = inputValue.trim().toLowerCase();
  return options.find(
    (o) =>
      o.value.toLowerCase() === normalizedInput ||
      o.label.toLowerCase() === normalizedInput
  );
};

/**
 * Validates a single cell value against the question rules
 */
export const validateCell = (
  value: string | undefined,
  question: SiteFormQuestionDto,
  allAnswers: Record<string, string>
): string | null => {
  // Check if this is a conditional question
  if (question.conditionalQuestionCode && question.conditionalValue) {
    const parentValue = allAnswers[question.conditionalQuestionCode];
    if (parentValue !== question.conditionalValue) {
      // Question is not visible/applicable, skip validation
      return null;
    }
  }

  // Check required fields
  if (question.isRequired && (value === undefined || value === null || value.trim() === '')) {
    return `Missing value for "${question.label}"`;
  }

  // If no value and not required, skip further validation
  if (value === undefined || value === null || value.trim() === '') {
    return null;
  }

  // Validate based on question type
  switch (question.questionType) {
    case SiteFormQuestionType.Radio:
    case SiteFormQuestionType.Select: {
      const match = findOptionMatch(question.options, value);
      if (!match) {
        const validLabels = question.options.map((o) => o.label);
        return `Invalid option "${value}". Valid options: ${validLabels.join(', ')}`;
      }
      break;
    }

    case SiteFormQuestionType.Checkbox: {
      const values = value.split(',').map((v) => v.trim());
      const invalidValues = values.filter((v) => !findOptionMatch(question.options, v));
      if (invalidValues.length > 0) {
        const validLabels = question.options.map((o) => o.label);
        return `Invalid options: ${invalidValues.join(', ')}. Valid options: ${validLabels.join(', ')}`;
      }
      break;
    }

    case SiteFormQuestionType.Date: {
      // Validate date format DD-MM-YYYY, DD/MM/YYYY, or YYYY-MM-DD
      const ddmmyyyyRegex = /^(\d{2})[-/](\d{2})[-/](\d{4})$/;
      const yyyymmddRegex = /^(\d{4})[-/](\d{2})[-/](\d{2})$/;
      
      let day: number, month: number, year: number;
      
      const ddmmMatch = value.match(ddmmyyyyRegex);
      const yyyymmMatch = value.match(yyyymmddRegex);
      
      if (ddmmMatch) {
        // DD-MM-YYYY format
        day = parseInt(ddmmMatch[1], 10);
        month = parseInt(ddmmMatch[2], 10);
        year = parseInt(ddmmMatch[3], 10);
      } else if (yyyymmMatch) {
        // YYYY-MM-DD format
        year = parseInt(yyyymmMatch[1], 10);
        month = parseInt(yyyymmMatch[2], 10);
        day = parseInt(yyyymmMatch[3], 10);
      } else {
        return `Invalid date format. Expected DD-MM-YYYY or YYYY-MM-DD`;
      }
      
      // Check if it's a valid date
      const date = new Date(year, month - 1, day);
      if (isNaN(date.getTime()) || date.getDate() !== day || date.getMonth() !== month - 1 || date.getFullYear() !== year) {
        return `Invalid date value`;
      }
      break;
    }

    case SiteFormQuestionType.Text:
      // Text values are valid as long as they're not empty (checked above)
      break;
  }

  return null;
};

/**
 * Validates a row and returns all errors
 */
export const validateRow = (
  rowData: Record<string, string>,
  questions: SiteFormQuestionDto[],
  matchResult?: LocationMatchResult
): CellError[] => {
  const errors: CellError[] = [];
  const rowIndex = -1; // Will be set by the caller

  // Normalize labels to underlying values for consistent conditional checks
  // (CSV data may contain labels but conditionalValue stores underlying values)
  const normalizedAnswers = normalizeRowData(rowData, questions);

  // Check if site name is provided
  const siteName = rowData[PREDEFINED_NAME_CODE];
  if (!siteName || siteName.trim() === '') {
    errors.push({
      rowIndex,
      columnIndex: 0,
      questionCode: PREDEFINED_NAME_CODE,
      message: `Site name is required`,
    });
  }

  // Validate each question
  for (const question of questions) {
    if (!question.isActive) continue;

    const value = rowData[question.code];
    const error = validateCell(value, question, normalizedAnswers);

    if (error) {
      const columnIndex = questions.findIndex((q) => q.code === question.code);
      errors.push({
        rowIndex,
        columnIndex,
        questionCode: question.code,
        message: error,
      });
    }
  }

  return errors;
};

/**
 * Validates all rows and updates each row with its errors
 */
export const validateAllRows = (
  rows: RowData[],
  questions: SiteFormQuestionDto[],
  matchResults: LocationMatchResult[]
): RowData[] => {
  return rows.map((row) => {
    const matchResult = matchResults.find(
      (m) => m.searchName.toLowerCase() === row.data[PREDEFINED_NAME_CODE]?.toLowerCase()
    );

    const errors = validateRow(row.data, questions, matchResult).map((error) => ({
      ...error,
      rowIndex: row.rowIndex,
    }));

    return {
      ...row,
      errors,
      matchResult,
    };
  });
};

/**
 * Gets the total number of errors across all rows
 */
export const getTotalErrorCount = (rows: RowData[]): number => {
  return rows.reduce((total, row) => total + row.errors.length, 0);
};

/**
 * Gets rows that have errors
 */
export const getRowsWithErrors = (rows: RowData[]): RowData[] => {
  return rows.filter((row) => row.errors.length > 0);
};

/**
 * Gets rows that have no errors (valid for processing)
 */
export const getValidRows = (rows: RowData[]): RowData[] => {
  return rows.filter((row) => row.errors.length === 0);
};

/**
 * Check if a specific cell has an error
 */
export const getCellError = (
  row: RowData,
  questionCode: string
): CellError | undefined => {
  return row.errors.find((error) => error.questionCode === questionCode);
};

/**
 * Normalizes row data by converting human-readable labels to their underlying values.
 * This ensures backwards compatibility where users can enter either labels or values.
 */
export const normalizeRowData = (
  rowData: Record<string, string>,
  questions: SiteFormQuestionDto[]
): Record<string, string> => {
  const normalized: Record<string, string> = { ...rowData };

  for (const question of questions) {
    if (!question.isActive) continue;
    const value = rowData[question.code];
    if (!value || question.options.length === 0) continue;

    if (
      question.questionType === SiteFormQuestionType.Radio ||
      question.questionType === SiteFormQuestionType.Select
    ) {
      const match = findOptionMatch(question.options, value);
      if (match) normalized[question.code] = match.value;
    } else if (question.questionType === SiteFormQuestionType.Checkbox) {
      const values = value.split(',').map((v) => v.trim());
      const normalizedValues = values
        .map((v) => findOptionMatch(question.options, v)?.value)
        .filter((v): v is string => v !== undefined);
      normalized[question.code] = normalizedValues.join(',');
    }
  }

  return normalized;
};
