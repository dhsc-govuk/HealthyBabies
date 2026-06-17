import {
  ServiceFormQuestionDto,
  ServiceFormQuestionOptionDto,
  ServiceFormQuestionType,
} from '../../../../components/Global/Services';
import { CellError, RowData, ServiceMatchResult } from './types';

/**
 * Parses a raw checkbox cell value into individual tokens using a greedy algorithm.
 *
 * Some option values/labels contain commas (e.g. "Strengthening Families, Strengthening Communities",
 * "Healthy Start, Happy Start"). A naive split(',') would tear those apart incorrectly.
 *
 * The greedy approach splits on every comma first, then tries to re-combine adjacent parts —
 * longest combination first — until a known option is matched. This correctly handles:
 *   "Strengthening Families, Strengthening Communities"          → one token
 *   "Strengthening Families, Strengthening Communities,EPEC"    → two tokens
 *   "Healthy Start, Happy Start,Incredible Years Preschool"     → two tokens
 */
const parseCheckboxInput = (
  raw: string,
  options: ServiceFormQuestionOptionDto[]
): string[] => {
  if (!raw.trim()) return [];

  const parts = raw.split(',').map((v) => v.trim()).filter(Boolean);
  const result: string[] = [];
  let i = 0;

  while (i < parts.length) {
    let matched = false;
    // Try longest possible combination first (greedy)
    for (let j = parts.length; j > i; j--) {
      const candidate = parts.slice(i, j).join(', ');
      if (findOptionMatch(options, candidate)) {
        result.push(candidate);
        i = j;
        matched = true;
        break;
      }
    }
    // No option matched — preserve the raw part so validation can flag it
    if (!matched) {
      result.push(parts[i]);
      i++;
    }
  }

  return result;
};

/**
 * Finds an option by matching either value or label (case-insensitive)
 * Also supports partial label matching where the input starts with or is contained in the label
 */
export const findOptionMatch = (
  options: ServiceFormQuestionOptionDto[],
  inputValue: string
): ServiceFormQuestionOptionDto | undefined => {
  const normalizedInput = inputValue.trim().toLowerCase();

  // Trim option values/labels before comparing — some DB entries have trailing spaces
  // (e.g. "As needed ", "A telephone helpline ") that would otherwise prevent matching.
  const exactMatch = options.find(
    (o) =>
      o.value.trim().toLowerCase() === normalizedInput ||
      o.label.trim().toLowerCase() === normalizedInput
  );
  if (exactMatch) return exactMatch;

  const partialMatch = options.find(
    (o) =>
      o.label.trim().toLowerCase().startsWith(normalizedInput) ||
      normalizedInput.startsWith(o.label.trim().toLowerCase())
  );
  return partialMatch;
};

/**
 * Checks if a conditional question should be visible based on parent value
 * Supports multi-value conditionals (comma-separated) where any match triggers visibility
 */
const isConditionalQuestionVisible = (
  question: ServiceFormQuestionDto,
  allAnswers: Record<string, string>
): boolean => {
  if (!question.conditionalQuestionCode || !question.conditionalValue) {
    return true;
  }

  const parentValue = allAnswers[question.conditionalQuestionCode] || '';
  const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());

  // For checkbox parent questions, check if any of the selected values match any conditional value
  const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
  return conditionalValues.some((cv) => parentSelectedValues.includes(cv));
};

/**
 * Validates a single cell value against the question rules
 */
export const validateCell = (
  value: string | undefined,
  question: ServiceFormQuestionDto,
  allAnswers: Record<string, string>,
  allQuestions?: ServiceFormQuestionDto[]
): string | null => {
  // Check if this is a conditional question
  if (!isConditionalQuestionVisible(question, allAnswers)) {
    // Conditional not met — silently skip any provided value rather than blocking the row.
    // Users often leave conditional fields filled when they change a parent answer; we should
    // not penalise them for it.
    return null;
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
    case ServiceFormQuestionType.Radio:
    case ServiceFormQuestionType.Select: {
      const match = findOptionMatch(question.options, value);
      if (!match) {
        const validLabels = question.options.map((o) => o.label);
        return `Invalid option "${value}". Valid options: ${validLabels.join(', ')}`;
      }
      break;
    }

    case ServiceFormQuestionType.Checkbox: {
      const values = parseCheckboxInput(value, question.options);
      const invalidValues = values.filter((v) => !findOptionMatch(question.options, v));
      if (invalidValues.length > 0) {
        const validLabels = question.options.map((o) => o.label);
        return `Invalid options: ${invalidValues.join(', ')}. Valid options: ${validLabels.join(', ')}`;
      }
      break;
    }

    case ServiceFormQuestionType.Date: {
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

    case ServiceFormQuestionType.Text:
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
  questions: ServiceFormQuestionDto[],
  matchResult?: ServiceMatchResult
): CellError[] => {
  const errors: CellError[] = [];
  const rowIndex = -1; // Will be set by the caller

  // Normalize labels to underlying values for consistent conditional checks
  // (CSV data may contain labels like "Planned for implementation" but conditionalValue stores values like "1")
  const normalizedAnswers = normalizeRowData(rowData, questions);

  // Check if service name is provided
  const serviceName = rowData['SMD01'];
  if (!serviceName || serviceName.trim() === '') {
    errors.push({
      rowIndex,
      columnIndex: 0,
      questionCode: 'SMD01',
      message: 'Service name is required',
    });
  }

  // Validate each question
  for (const question of questions) {
    if (!question.isActive) continue;

    const value = rowData[question.code];
    const error = validateCell(value, question, normalizedAnswers, questions);

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
  questions: ServiceFormQuestionDto[],
  matchResults: ServiceMatchResult[]
): RowData[] => {
  return rows.map((row) => {
    const matchResult = matchResults.find(
      (m) => m.searchName.toLowerCase() === row.data['SMD01']?.toLowerCase()
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
  questions: ServiceFormQuestionDto[]
): Record<string, string> => {
  const normalized: Record<string, string> = { ...rowData };

  for (const question of questions) {
    if (!question.isActive) continue;
    const value = rowData[question.code];
    if (!value || question.options.length === 0) continue;

    if (
      question.questionType === ServiceFormQuestionType.Radio ||
      question.questionType === ServiceFormQuestionType.Select
    ) {
      const match = findOptionMatch(question.options, value);
      if (match) normalized[question.code] = match.value;
    } else if (question.questionType === ServiceFormQuestionType.Checkbox) {
      const values = parseCheckboxInput(value, question.options);
      const normalizedValues = values
        .map((v) => findOptionMatch(question.options, v)?.value)
        .filter((v): v is string => v !== undefined);
      normalized[question.code] = normalizedValues.join(',');
    }
  }

  return normalized;
};
