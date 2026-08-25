import { Delimiter, ParsedData, CellError } from './types';
import { BulkUploadValidationResultDto, BulkUploadFieldMetadataDto } from './dtos';
import { evaluateNumericOperators } from '../../../helpers/conditionalOperators';

interface FieldOption {
  value: string;
  label: string;
}

/**
 * Finds an option by matching either value or label (case-insensitive)
 */
export const findOptionMatch = (options: FieldOption[], inputValue: string): FieldOption | undefined => {
  const normalizedInput = inputValue.trim().toLowerCase();
  return options.find((o) => o.value.toLowerCase() === normalizedInput || o.label.toLowerCase() === normalizedInput);
};

/**
 * Validates a date value - accepts DD-MM-YYYY, DD/MM/YYYY, or YYYY-MM-DD
 */
export const validateDateValue = (value: string): string | null => {
  const ddmmyyyyRegex = /^(\d{2})[-/](\d{2})[-/](\d{4})$/;
  const yyyymmddRegex = /^(\d{4})[-/](\d{2})[-/](\d{2})$/;

  let day: number, month: number, year: number;

  const ddmmMatch = value.match(ddmmyyyyRegex);
  const yyyymmMatch = value.match(yyyymmddRegex);

  if (ddmmMatch) {
    day = parseInt(ddmmMatch[1], 10);
    month = parseInt(ddmmMatch[2], 10);
    year = parseInt(ddmmMatch[3], 10);
  } else if (yyyymmMatch) {
    year = parseInt(yyyymmMatch[1], 10);
    month = parseInt(yyyymmMatch[2], 10);
    day = parseInt(yyyymmMatch[3], 10);
  } else {
    return 'Invalid date format. Expected DD-MM-YYYY or YYYY-MM-DD';
  }

  // Check if it's a valid date
  const date = new Date(year, month - 1, day);
  if (isNaN(date.getTime()) || date.getDate() !== day || date.getMonth() !== month - 1 || date.getFullYear() !== year) {
    return 'Invalid date value';
  }

  return null;
};

/**
 * Reads configuration.min (e.g. "min": 0 on every demographic breakdown field) from a field's raw
 * configuration JSON. Returns null when absent/unparseable so callers can skip the check.
 */
const parseFieldMin = (configuration: string | null | undefined): number | null => {
  if (!configuration) return null;
  try {
    const parsed = JSON.parse(configuration) as { min?: number };
    return typeof parsed.min === 'number' ? parsed.min : null;
  } catch {
    return null;
  }
};

/**
 * Validates a cell value against field metadata
 */
export const validateCellValue = (value: string | undefined, fieldMeta: BulkUploadFieldMetadataDto, allRowValues: Record<string, string>): string | null => {
  // Check if this is a conditional field
  if (fieldMeta.conditionalRules) {
    try {
      const rules = JSON.parse(fieldMeta.conditionalRules);
      if (rules.showWhen?.fieldKey && rules.showWhen?.equals) {
        const parentValue = allRowValues[rules.showWhen.fieldKey];
        if (parentValue?.toLowerCase() !== rules.showWhen.equals.toLowerCase()) {
          // Field is not visible/applicable, skip validation
          return null;
        }
      }
    } catch {
      // Invalid JSON, continue with validation
    }
  }

  // Check required fields
  if (fieldMeta.isRequired && (!value || value.trim() === '')) {
    return 'This field is required';
  }

  // If no value and not required, skip further validation
  if (!value || value.trim() === '') {
    return null;
  }

  const fieldType = fieldMeta.fieldType.toLowerCase();

  // Validate based on field type
  switch (fieldType) {
    case 'radio':
    case 'select': {
      if (fieldMeta.options.length > 0) {
        const match = findOptionMatch(fieldMeta.options, value);
        if (!match) {
          const validLabels = fieldMeta.options.map((o) => o.label);
          return `Invalid option "${value}". Valid options: ${validLabels.join(', ')}`;
        }
      }
      break;
    }

    case 'checkbox': {
      if (fieldMeta.options.length > 0) {
        const values = value
          .split(',')
          .map((v) => v.trim())
          .filter((v) => v);
        const invalidValues = values.filter((v) => !findOptionMatch(fieldMeta.options, v));
        if (invalidValues.length > 0) {
          const validLabels = fieldMeta.options.map((o) => o.label);
          return `Invalid options: ${invalidValues.join(', ')}. Valid options: ${validLabels.join(', ')}`;
        }
      }
      break;
    }

    case 'date': {
      const dateError = validateDateValue(value);
      if (dateError) return dateError;
      break;
    }

    case 'number': {
      const numericValue = Number(value);
      if (isNaN(numericValue)) {
        return 'Invalid number value';
      }
      const min = parseFieldMin(fieldMeta.configuration);
      if (min !== null && numericValue < min) {
        return `Must be ${min} or more`;
      }
      break;
    }

    case 'email': {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(value)) {
        return 'Invalid email format';
      }
      break;
    }
  }

  return null;
};

/**
 * Normalizes row data by converting human-readable labels to their underlying values.
 * This ensures backwards compatibility where users can enter either labels or values.
 */
export const normalizeRowData = (rowData: string[], headers: string[], fieldMetadata: BulkUploadFieldMetadataDto[] | undefined): string[] => {
  if (!fieldMetadata) return rowData;

  return rowData.map((value, index) => {
    if (!value || value.trim() === '') return value;

    const fieldCode = headers[index];
    const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);
    if (!fieldMeta || fieldMeta.options.length === 0) return value;

    const fieldType = fieldMeta.fieldType.toLowerCase();

    if (fieldType === 'radio' || fieldType === 'select') {
      const match = findOptionMatch(fieldMeta.options, value);
      if (match) return match.value;
    } else if (fieldType === 'checkbox') {
      const values = value.split(',').map((v) => v.trim());
      const normalizedValues = values.map((v) => findOptionMatch(fieldMeta.options, v)?.value).filter((v): v is string => v !== undefined);
      if (normalizedValues.length > 0) return normalizedValues.join(',');
    }

    return value;
  });
};

/**
 * Validates a row and returns cell errors for client-side validation
 */
export const validateRowClientSide = (rowData: string[], headers: string[], fieldMetadata: BulkUploadFieldMetadataDto[] | undefined): CellError[] => {
  if (!fieldMetadata) return [];

  const errors: CellError[] = [];

  // Build a map of field code to value for conditional checks
  const allRowValues: Record<string, string> = {};
  headers.forEach((header, index) => {
    allRowValues[header] = rowData[index] || '';
  });

  headers.forEach((fieldCode, cellIndex) => {
    const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);
    if (!fieldMeta) return;

    const value = rowData[cellIndex];
    const error = validateCellValue(value, fieldMeta, allRowValues);

    if (error) {
      errors.push({
        rowIndex: -1, // Will be set by caller
        cellIndex,
        fieldCode,
        message: error,
      });
    }
  });

  return errors;
};

const isRequiredIndicatorsRow = (row: string[]): boolean => row.some((cell) => cell !== '') && row.every((cell) => cell === 'R' || cell === 'O' || cell === '');

// The template's "valid options" row, listing each question's option values so form builder changes
// are visible in a plain CSV. Documentation, never data — every populated cell carries the prefix,
// which no genuine answer does. Mirrors BulkUploadOptionsRow.CellPrefix on the server.
const OPTIONS_CELL_PREFIX = 'options:';

const isOptionsRow = (row: string[]): boolean => {
  const populated = row.filter((cell) => cell !== '');
  return populated.length > 0 && populated.every((cell) => cell.toLowerCase().startsWith(OPTIONS_CELL_PREFIX));
};

/**
 * Parses raw CSV text into headers/labels/options/required-indicators/data rows.
 *
 * Without `knownDataStartLine`, whether row 2 is a "field labels" row (present in every template
 * downloaded from this app) or the first real data row (e.g. a hand-built CSV with no labels row)
 * is ambiguous from the CSV content alone, so this falls back to assuming a labels row is present.
 * The valid-options and required-indicators rows below it are self-identifying, so those are
 * detected rather than guessed. Once the backend has validated the file, pass its authoritative
 * `rowValidations[0].rowNumber` (the true 1-based line number of the first data row) as
 * `knownDataStartLine` to re-derive the split exactly, instead of guessing — this is what keeps the
 * on-screen rows, cell errors, and the rows actually persisted in sync.
 */
export const parseCSV = (content: string, delim: Delimiter, knownDataStartLine?: number): ParsedData => {
  const lines = content.split(/\r?\n/).filter((line) => line.trim());
  const rows = lines.map((line) => {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      if (char === '"') {
        inQuotes = !inQuotes;
      } else if (char === delim && !inQuotes) {
        result.push(current.trim());
        current = '';
      } else {
        current += char;
      }
    }
    result.push(current.trim());
    return result;
  });

  const headers = rows[0] || [];

  if (knownDataStartLine && knownDataStartLine >= 2) {
    const dataStartIndex = knownDataStartLine - 1;
    const preambleRows = rows.slice(1, dataStartIndex);

    // Classify each preamble row rather than keying off how many there are: a template carries up to
    // three (labels, valid options, R/O indicators) and a hand-built CSV any subset of them.
    const indicatorsRow = preambleRows.find(isRequiredIndicatorsRow);
    const labels = preambleRows.find((row) => !isRequiredIndicatorsRow(row) && !isOptionsRow(row)) ?? [];
    const requiredFields = indicatorsRow ? indicatorsRow.map((cell) => cell === 'R') : headers.map(() => false);

    return { headers, labels, requiredFields, rows: rows.slice(dataStartIndex), dataStartLine: knownDataStartLine };
  }

  // Fallback heuristic used before the file has been validated (e.g. the 'preview' step) — see
  // doc comment above for why this can't reliably tell a labels row from a genuine data row.
  const labels = rows[1] || [];

  // Advance past whichever self-identifying rows follow the labels, so neither shows up as a phantom
  // first data row in the preview.
  let dataStartIndex = 2;
  while (rows[dataStartIndex] && (isOptionsRow(rows[dataStartIndex]) || isRequiredIndicatorsRow(rows[dataStartIndex]))) {
    dataStartIndex++;
  }

  const indicatorsRow = rows.slice(2, dataStartIndex).find(isRequiredIndicatorsRow);
  const requiredFields = indicatorsRow ? indicatorsRow.map((cell) => cell === 'R') : headers.map(() => false);

  return { headers, labels, requiredFields, rows: rows.slice(dataStartIndex), dataStartLine: dataStartIndex + 1 };
};

export const validateFile = (file: File): string | null => {
  const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

  if (!file.name.endsWith('.csv')) {
    return 'Please select a CSV file (.csv)';
  }

  if (file.size > MAX_FILE_SIZE) {
    return 'File size must be less than 5MB';
  }

  return null;
};

export interface ConvertedValidationErrors {
  cellErrors: CellError[];
  // Field codes the server rejected that have no column in the uploaded file. They cannot be shown
  // against a cell, and dropping them silently is how a file the server had already rejected could
  // reach the "choose services" step looking clean — the caller surfaces these as a file-level error.
  unmappedFieldCodes: string[];
}

export const convertValidationErrorsToCellErrors = (validationResult: BulkUploadValidationResultDto, headers: string[], parsedData: ParsedData): ConvertedValidationErrors => {
  const errors: CellError[] = [];

  if (validationResult.rowValidations) {
    validationResult.rowValidations.forEach((rowValidation) => {
      if (!rowValidation.isValid && rowValidation.errors) {
        // Use the backend's rowNumber directly to calculate the correct row index
        // Backend rowNumber is 1-indexed and includes header rows
        // parsedData.rows is 0-indexed data rows only

        // Calculate the local row index from the backend's 1-based CSV line number.
        const rowIndex = rowValidation.rowNumber - parsedData.dataStartLine;

        if (rowIndex >= 0 && rowIndex < parsedData.rows.length) {
          const rowData = parsedData.rows[rowIndex];
          rowValidation.errors.forEach((error) => {
            // Case-insensitively, because the server maps headers that way (MapRowToFields uses an
            // OrdinalIgnoreCase dictionary). Matching exactly would report a column the server had
            // read quite happily as missing from the file.
            const cellIndex = headers.findIndex((h) => h.toLowerCase() === error.fieldCode?.toLowerCase());
            if (cellIndex === -1) {
              return;
            }

            // Skip errors for fields that are not visible (parent conditions not met)
            // This handles cases where backend validation doesn't properly check conditional rules
            if (!isFieldVisible(error.fieldCode, rowData, headers, validationResult.fieldMetadata)) {
              return;
            }
            errors.push({
              rowIndex,
              cellIndex,
              fieldCode: error.fieldCode,
              message: error.errorMessage,
            });
          });
        }
      }
    });
  }

  // Add sum group validation errors for each row. The server reports these as well now
  // (ValidateBulkUploadCommand.ValidateRow), so this pass mostly re-derives errors already added
  // above - it is kept for a response that predates that, and because the inline editor re-runs the
  // same function on every cell edit. Hence the dedupe below.
  if (validationResult.fieldMetadata) {
    parsedData.rows.forEach((row, rowIndex) => {
      const sumErrors = validateSumGroups(row, headers, rowIndex, validationResult.fieldMetadata);
      errors.push(...sumErrors);
    });
  }

  return { cellErrors: dedupeCellErrors(errors), unmappedFieldCodes: getUnmappedFieldCodes(validationResult, headers) };
};

/**
 * Columns the server raised errors against that the uploaded file doesn't have. Nothing in the
 * table can carry such an error and no cell edit can satisfy it, so the only remedy is a different
 * file - which is why this both drives the file-level message and stops the check step continuing.
 */
export const getUnmappedFieldCodes = (validationResult: BulkUploadValidationResultDto, headers: string[]): string[] => {
  const unmapped = new Set<string>();

  validationResult.rowValidations?.forEach((rowValidation) => {
    if (rowValidation.isValid || !rowValidation.errors) return;

    rowValidation.errors.forEach((error) => {
      if (error.fieldCode && !headers.some((h) => h.toLowerCase() === error.fieldCode.toLowerCase())) {
        unmapped.add(error.fieldCode);
      }
    });
  });

  return [...unmapped];
};

/**
 * One error per cell, keeping the first. The table can only highlight a cell once and only ever
 * shows the first message for it, and the count above it reads "N cells ... contain data validation
 * errors" - which a cell listed twice overstates. Two sources can name the same cell: a per-cell
 * rule and a sum-group rule, and the server and the client both run the sum-group rule.
 */
export const dedupeCellErrors = (errors: CellError[]): CellError[] => {
  const seenCells = new Set<string>();

  return errors.filter((error) => {
    const key = `${error.rowIndex}:${error.cellIndex}`;
    if (seenCells.has(key)) {
      return false;
    }

    seenCells.add(key);
    return true;
  });
};

export const updateRequiredFieldsFromMetadata = (headers: string[], validationResult: BulkUploadValidationResultDto): boolean[] => {
  return headers.map((header) => {
    // ServiceName is always required
    if (header === 'ServiceName') return true;
    // AnonymisedId is always required for outcome scores
    if (header === 'AnonymisedId') return true;
    const fieldMeta = validationResult.fieldMetadata?.find((f) => f.fieldCode === header);
    return fieldMeta?.isRequired ?? false;
  });
};

export const hasErrorsInRow = (rowIndex: number, cellErrors: CellError[]): boolean => {
  return cellErrors.some((error) => error.rowIndex === rowIndex);
};

export const getErrorMessage = (rowIndex: number, cellIndex: number, cellErrors: CellError[]): string | null => {
  const error = cellErrors.find((e) => e.rowIndex === rowIndex && e.cellIndex === cellIndex);
  return error?.message || null;
};

interface ConditionalRule {
  showWhen?: {
    fieldKey?: string;
    serviceFieldKey?: string;
    equals?: string;
    notEquals?: string;
    gt?: string;
    gte?: string;
    lt?: string;
    lte?: string;
    eq?: string;
    neq?: string;
    in?: string[];
    allOf?: Array<{
      fieldKey?: string;
      serviceFieldKey?: string;
      equals?: string;
      notEquals?: string;
      in?: string[];
      gt?: string;
      gte?: string;
      lt?: string;
      lte?: string;
      eq?: string;
      neq?: string;
    }>;
  };
  displayInline?: boolean;
  parentOption?: string;
}

interface FieldMetadataWithRequired {
  fieldCode: string;
  fieldType?: string;
  conditionalRules: string | null;
  isRequired?: boolean;
  configuration?: string | null;
  options?: { value: string; label: string }[];
}

interface FieldConfiguration {
  group?: string;
  sumGroup?: string;
  maxSumField?: string;
  groupLabel?: string;
}

/**
 * Parse field configuration JSON string
 */
const parseConfiguration = (config: string | null | undefined): FieldConfiguration | null => {
  if (!config) return null;
  try {
    return JSON.parse(config) as FieldConfiguration;
  } catch {
    return null;
  }
};

/**
 * Validate sum groups for a row - ensures grouped fields don't exceed the max field value
 * Also validates single fields with maxSumField (like QSU14)
 * Returns an array of cell errors for fields that violate the sum constraint
 */
export const validateSumGroups = (rowData: string[], headers: string[], rowIndex: number, fieldMetadata: FieldMetadataWithRequired[] | undefined): CellError[] => {
  if (!fieldMetadata) return [];

  const errors: CellError[] = [];
  const processedGroups = new Set<string>();

  fieldMetadata.forEach((fieldMeta) => {
    const config = parseConfiguration(fieldMeta.configuration);

    // Skip if no maxSumField configured
    if (!config?.maxSumField) return;

    // Skip validation if the field is not visible (parent conditions not met)
    if (!isFieldVisible(fieldMeta.fieldCode, rowData, headers, fieldMetadata)) {
      return;
    }

    // Get the max value from the reference field
    const maxFieldIndex = headers.indexOf(config.maxSumField);
    if (maxFieldIndex === -1) return;

    const maxValueStr = rowData[maxFieldIndex]?.trim();
    const maxValue = parseFloat(maxValueStr || '0');
    if (isNaN(maxValue) || maxValue === 0) return;

    // Handle grouped fields (sumGroup validation)
    if (config.sumGroup && !processedGroups.has(config.sumGroup)) {
      processedGroups.add(config.sumGroup);

      // Find all fields in this sum group
      const groupFields = fieldMetadata.filter((f) => {
        const fConfig = parseConfiguration(f.configuration);
        return fConfig?.sumGroup === config.sumGroup;
      });

      // Calculate the sum of all group fields
      let sum = 0;
      groupFields.forEach((gf) => {
        const fieldIndex = headers.indexOf(gf.fieldCode);
        if (fieldIndex !== -1) {
          const value = parseFloat(rowData[fieldIndex]?.trim() || '0');
          if (!isNaN(value)) {
            sum += value;
          }
        }
      });

      // Check if sum exceeds max
      if (sum > maxValue) {
        const groupLabel = config.groupLabel || config.sumGroup;
        const errorMessage = `The total for '${groupLabel}' (${sum}) cannot exceed the total number of service users (${maxValue})`;

        // Add error to all fields that have a value > 0 (contributing to the sum)
        groupFields.forEach((gf) => {
          const fieldIndex = headers.indexOf(gf.fieldCode);
          if (fieldIndex !== -1) {
            const value = parseFloat(rowData[fieldIndex]?.trim() || '0');
            if (!isNaN(value) && value > 0) {
              errors.push({
                rowIndex,
                cellIndex: fieldIndex,
                fieldCode: gf.fieldCode,
                message: errorMessage,
              });
            }
          }
        });
      }
    }
    // Handle single fields with maxSumField but no sumGroup (like QSU14)
    else if (!config.sumGroup) {
      const fieldIndex = headers.indexOf(fieldMeta.fieldCode);
      if (fieldIndex === -1) return;

      const fieldValue = parseFloat(rowData[fieldIndex]?.trim() || '0');
      if (isNaN(fieldValue) || fieldValue === 0) return;

      if (fieldValue > maxValue) {
        errors.push({
          rowIndex,
          cellIndex: fieldIndex,
          fieldCode: fieldMeta.fieldCode,
          message: `This value (${fieldValue}) cannot exceed the total number of service users (${maxValue})`,
        });
      }
    }
  });

  return errors;
};

/**
 * Check if a field is visible based on conditional rules.
 * Returns true if the field should be shown (parent conditions are met).
 */
export const isFieldVisible = (
  fieldCode: string,
  rowData: string[],
  headers: string[],
  fieldMetadata: FieldMetadataWithRequired[] | undefined,
  // Populated per-row from the matched Service's own answers (e.g. SMD02/SMD09). Not yet threaded through by
  // any caller today — the bulk-upload screen only fetches service names, not answers. When it is undefined,
  // service-scoped conditions (serviceFieldKey, including inside allOf) default to VISIBLE, because we cannot
  // evaluate them client-side and the server (ValidateBulkUploadCommand) already validates them correctly.
  // Once real service answers are wired in (see BulkUploadValidationResultDto), the conditions evaluate normally.
  serviceAnswers?: Record<string, string>
): boolean => {
  if (!fieldMetadata) return true;

  const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);
  if (!fieldMeta?.conditionalRules) return true; // No conditions = always visible

  try {
    const rules: ConditionalRule = JSON.parse(fieldMeta.conditionalRules);
    if (!rules.showWhen) return true;

    // Helper to check if a value matches (handles both single values and comma-separated checkbox values)
    const valueMatches = (parentValue: string | undefined, targetValue: string, parentFieldCode: string): boolean => {
      if (!parentValue) return false;
      const normalizedTarget = targetValue.toLowerCase();

      // Check if parent field is a checkbox (multi-select) by looking at field metadata
      const parentFieldMeta = fieldMetadata.find((f) => f.fieldCode === parentFieldCode);
      const isCheckbox = parentFieldMeta?.fieldType?.toLowerCase() === 'checkbox';

      if (isCheckbox) {
        // For checkbox fields, check if any selected value matches
        const selectedValues = parentValue.split(',').map((v) => v.trim().toLowerCase());

        // Map labels to values for comparison
        const optionValueMap = new Map<string, string>();
        if (parentFieldMeta?.options) {
          parentFieldMeta.options.forEach((opt) => {
            optionValueMap.set(opt.label.toLowerCase(), opt.value.toLowerCase());
            optionValueMap.set(opt.value.toLowerCase(), opt.value.toLowerCase());
          });
        }

        const normalizedSelectedValues = selectedValues.map((v) => optionValueMap.get(v) || v);
        return normalizedSelectedValues.includes(normalizedTarget);
      }

      return parentValue.toLowerCase() === normalizedTarget;
    };

    const valueNotMatches = (parentValue: string | undefined, targetValue: string, parentFieldCode: string): boolean => {
      return !valueMatches(parentValue, targetValue, parentFieldCode);
    };

    // Check simple service-scoped condition (e.g. SMD02/SMD09 on the row's Service, not the row itself)
    if (rules.showWhen.serviceFieldKey) {
      // The bulk-upload screen doesn't fetch service answers. Without them we cannot evaluate a
      // service-scoped condition, so default to visible (safe: the server validates these correctly).
      // Treating an equals/in condition as "not visible" here would flag valid data as "should be empty".
      if (!serviceAnswers) return true;
      const serviceValue = serviceAnswers[rules.showWhen.serviceFieldKey];
      const numeric = evaluateNumericOperators(serviceValue, rules.showWhen);
      if (numeric !== null) return numeric;
      if (rules.showWhen.equals) return (serviceValue ?? '').toLowerCase() === rules.showWhen.equals.toLowerCase();
      if (rules.showWhen.in && Array.isArray(rules.showWhen.in)) {
        return rules.showWhen.in.map((v) => v.toLowerCase()).includes((serviceValue ?? '').toLowerCase());
      }
      return true;
    }

    // Check numeric comparison on a row field (gt/gte/lt/lte/eq/neq)
    if (rules.showWhen.fieldKey) {
      const parentFieldIndex = headers.indexOf(rules.showWhen.fieldKey);
      const numeric = parentFieldIndex !== -1 ? evaluateNumericOperators(rowData[parentFieldIndex]?.trim(), rules.showWhen) : null;
      if (numeric !== null) return numeric;
    }

    // Check simple equals condition
    if (rules.showWhen.fieldKey && rules.showWhen.equals) {
      const parentFieldIndex = headers.indexOf(rules.showWhen.fieldKey);
      if (parentFieldIndex !== -1) {
        const parentValue = rowData[parentFieldIndex]?.trim();
        return valueMatches(parentValue, rules.showWhen.equals, rules.showWhen.fieldKey);
      }
    }

    // Check allOf conditions (all must be true)
    if (rules.showWhen.allOf && Array.isArray(rules.showWhen.allOf)) {
      return rules.showWhen.allOf.every((condition) => {
        if (condition.serviceFieldKey) {
          // No service answers available — cannot evaluate this condition, so treat it as satisfied
          // (default visible). Server-side validation handles service-scoped rules correctly.
          if (!serviceAnswers) return true;
          const serviceValue = serviceAnswers[condition.serviceFieldKey];
          const numeric = evaluateNumericOperators(serviceValue, condition);
          if (numeric !== null) return numeric;
          if (condition.equals) return (serviceValue ?? '').toLowerCase() === condition.equals.toLowerCase();
          if (condition.in && Array.isArray(condition.in)) {
            return condition.in.map((v) => v.toLowerCase()).includes((serviceValue ?? '').toLowerCase());
          }
          return true;
        }

        const parentFieldIndex = headers.indexOf(condition.fieldKey!);
        if (parentFieldIndex === -1) return false;
        const parentValue = rowData[parentFieldIndex]?.trim();

        const numeric = evaluateNumericOperators(parentValue, condition);
        if (numeric !== null) return numeric;
        if (condition.equals) {
          return valueMatches(parentValue, condition.equals, condition.fieldKey!);
        }
        if (condition.notEquals) {
          return valueNotMatches(parentValue, condition.notEquals, condition.fieldKey!);
        }
        return false;
      });
    }

    // Check "in" condition (value is in array)
    if (rules.showWhen.fieldKey && rules.showWhen.in) {
      const parentFieldIndex = headers.indexOf(rules.showWhen.fieldKey);
      if (parentFieldIndex !== -1) {
        const rawParentValue = rowData[parentFieldIndex];
        // Split first, then trim and lowercase each token so entries like "GAD-7 , ASQ-3" normalize per-token
        const selectedValues =
          rawParentValue
            ?.split(',')
            .map((v) => v.trim().toLowerCase())
            .filter((v) => v !== '') || [];

        // Get the parent field's options to map labels to values
        const parentFieldMeta = fieldMetadata.find((f) => f.fieldCode === rules.showWhen?.fieldKey);
        const optionValueMap = new Map<string, string>();
        if (parentFieldMeta?.options) {
          parentFieldMeta.options.forEach((opt) => {
            optionValueMap.set(opt.label.toLowerCase(), opt.value.toLowerCase());
            optionValueMap.set(opt.value.toLowerCase(), opt.value.toLowerCase());
          });
        }

        // Normalize selected values to option values (handle both labels and values)
        const normalizedSelectedValues = selectedValues.map((v) => optionValueMap.get(v) || v);

        return rules.showWhen.in.some((v) => normalizedSelectedValues.includes(v.toLowerCase()));
      }
    }
  } catch {
    // Invalid JSON, assume visible
  }

  return true;
};

/**
 * Check if a field should be validated based on progressive validation rules.
 * A field should only show errors if all preceding required fields have been answered.
 * This prevents showing all errors at once - only the first unanswered required field shows an error.
 */
export const shouldValidateField = (fieldCode: string, rowData: string[], headers: string[], fieldMetadata: FieldMetadataWithRequired[] | undefined): boolean => {
  if (!fieldMetadata) return true;

  const fieldIndex = headers.indexOf(fieldCode);
  if (fieldIndex === -1) return true;

  const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);

  // For fields with conditional rules, check if parent condition is met first
  if (fieldMeta?.conditionalRules) {
    const visible = isFieldVisible(fieldCode, rowData, headers, fieldMetadata);
    if (!visible) return false; // Don't validate hidden fields
  }

  // Check all preceding fields (left to right)
  for (let i = 0; i < fieldIndex; i++) {
    const precedingFieldCode = headers[i];
    const precedingMeta = fieldMetadata.find((f) => f.fieldCode === precedingFieldCode);

    if (!precedingMeta) continue;

    // Skip ServiceName column - it's always prefilled
    if (precedingFieldCode === 'ServiceName' || precedingFieldCode === 'PPS01') continue;

    // Check if preceding field has conditional rules
    if (precedingMeta.conditionalRules) {
      const precedingVisible = isFieldVisible(precedingFieldCode, rowData, headers, fieldMetadata);
      if (!precedingVisible) continue; // Skip hidden fields
    }

    // If preceding field is required and empty, don't validate current field yet
    if (precedingMeta.isRequired) {
      const precedingValue = rowData[i]?.trim();
      if (!precedingValue) {
        return false; // Stop - preceding required field is empty
      }
    }
  }

  return true;
};

/**
 * Check if a field is conditionally required based on parent field values in the row.
 * A field is conditionally required if:
 * 1. It has conditional rules (showWhen)
 * 2. The parent condition is met (field is visible)
 * 3. The field itself is marked as required OR it's a sub-field that should have data
 *
 * This is used to highlight empty sub-fields when parent is "Yes"
 */
export const isFieldConditionallyRequired = (fieldCode: string, rowData: string[], headers: string[], fieldMetadata: FieldMetadataWithRequired[] | undefined): boolean => {
  if (!fieldMetadata) return false;

  const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);
  if (!fieldMeta?.conditionalRules) return false;

  // Only mark as conditionally required if the field is visible (parent condition met)
  // AND the field is actually required
  const visible = isFieldVisible(fieldCode, rowData, headers, fieldMetadata);

  // If not visible, not conditionally required
  if (!visible) return false;

  // If visible and has isRequired flag, it's conditionally required
  if (fieldMeta.isRequired) return true;

  // For grouped sub-fields (like QSU08_*, QSU10_*, QSU12_*), when parent is "Yes",
  // ALL sub-fields must be filled - highlight each individually until it has a value.
  // Only applies to fields with a "group" key in their configuration — plain optional
  // fields (e.g. QSU17) also carry parentOption but must not be treated as required.
  try {
    const rules: ConditionalRule = JSON.parse(fieldMeta.conditionalRules);
    if (rules.displayInline || rules.parentOption) {
      const config = parseConfiguration(fieldMeta.configuration);
      if (!config?.group) return false;

      const fieldIndex = headers.indexOf(fieldCode);
      if (fieldIndex !== -1) {
        const value = rowData[fieldIndex]?.trim();
        return !value || value === '';
      }
    }
  } catch {
    // Invalid JSON
  }

  return false;
};

/**
 * Check if a field has a value when it should be empty (parent condition NOT met).
 * This is "backward validation" - if parent is "No", child fields should be empty.
 * If a child field has a value when parent is "No", it's an error.
 */
export const isFieldValueInvalid = (
  fieldCode: string,
  cellValue: string,
  rowData: string[],
  headers: string[],
  fieldMetadata: FieldMetadataWithRequired[] | undefined,
  labels?: string[]
): { isInvalid: boolean; errorMessage?: string } => {
  if (!fieldMetadata) return { isInvalid: false };

  const fieldMeta = fieldMetadata.find((f) => f.fieldCode === fieldCode);
  if (!fieldMeta?.conditionalRules) return { isInvalid: false };

  // Check if field has a value
  const hasValue = cellValue && cellValue.trim() !== '';
  if (!hasValue) return { isInvalid: false };

  // Check if field is visible (parent condition met). Service-scoped conditions default to visible
  // when the screen has no service answers (see isFieldVisible), so they never produce a false
  // "should be empty" error here.
  const visible = isFieldVisible(fieldCode, rowData, headers, fieldMetadata);

  // If field has a value but should NOT be visible (parent condition NOT met),
  // it's an error - the value should be empty
  if (!visible && hasValue) {
    try {
      const rules: ConditionalRule = JSON.parse(fieldMeta.conditionalRules);
      if (rules.showWhen?.fieldKey) {
        const parentFieldIndex = headers.indexOf(rules.showWhen.fieldKey);
        const parentFieldMeta = fieldMetadata.find((f) => f.fieldCode === rules.showWhen?.fieldKey);

        // Get parent question label (from labels array or fallback to code)
        const parentLabel = (labels && parentFieldIndex >= 0 ? labels[parentFieldIndex] : null) || parentFieldMeta?.fieldCode || rules.showWhen.fieldKey;

        // Get current field label
        const fieldIndex = headers.indexOf(fieldCode);
        const currentFieldLabel = (labels && fieldIndex >= 0 ? labels[fieldIndex] : null) || fieldCode;

        // Get the parent's current value for context
        const parentValue = parentFieldIndex >= 0 ? rowData[parentFieldIndex] : '';

        // Determine expected value based on condition type
        let expectedValue: string;
        if (rules.showWhen.in && Array.isArray(rules.showWhen.in)) {
          // For checkbox "in" conditions, show human-readable labels (not internal value codes)
          expectedValue = rules.showWhen.in
            .map((v) => {
              const match = parentFieldMeta?.options?.find((opt) => opt.value.toLowerCase() === v.toLowerCase());
              return match?.label ?? v;
            })
            .join(', ');
          return {
            isInvalid: true,
            errorMessage: `"${currentFieldLabel}" should be empty because "${parentLabel}" does not include "${expectedValue}"`,
          };
        } else {
          expectedValue = rules.showWhen.equals || 'Yes';
          return {
            isInvalid: true,
            errorMessage: `"${currentFieldLabel}" should be empty because "${parentLabel}" is "${parentValue || 'empty'}" (expected "${expectedValue}" to fill this field)`,
          };
        }
      }
    } catch {
      // Invalid JSON
    }
    return {
      isInvalid: true,
      errorMessage: 'This field should be empty based on the parent field value',
    };
  }

  return { isInvalid: false };
};
