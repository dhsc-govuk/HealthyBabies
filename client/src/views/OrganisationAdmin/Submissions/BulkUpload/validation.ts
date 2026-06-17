import { Delimiter, ParsedData, CellError } from './types';
import { BulkUploadValidationResultDto, BulkUploadFieldMetadataDto } from '../queries';

interface FieldOption {
  value: string;
  label: string;
}

/**
 * Finds an option by matching either value or label (case-insensitive)
 */
export const findOptionMatch = (
  options: FieldOption[],
  inputValue: string
): FieldOption | undefined => {
  const normalizedInput = inputValue.trim().toLowerCase();
  return options.find(
    (o) =>
      o.value.toLowerCase() === normalizedInput ||
      o.label.toLowerCase() === normalizedInput
  );
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
 * Validates a cell value against field metadata
 */
export const validateCellValue = (
  value: string | undefined,
  fieldMeta: BulkUploadFieldMetadataDto,
  allRowValues: Record<string, string>
): string | null => {
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
        const values = value.split(',').map((v) => v.trim()).filter(v => v);
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
      if (isNaN(Number(value))) {
        return 'Invalid number value';
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
export const normalizeRowData = (
  rowData: string[],
  headers: string[],
  fieldMetadata: BulkUploadFieldMetadataDto[] | undefined
): string[] => {
  if (!fieldMetadata) return rowData;

  return rowData.map((value, index) => {
    if (!value || value.trim() === '') return value;

    const fieldCode = headers[index];
    const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
    if (!fieldMeta || fieldMeta.options.length === 0) return value;

    const fieldType = fieldMeta.fieldType.toLowerCase();

    if (fieldType === 'radio' || fieldType === 'select') {
      const match = findOptionMatch(fieldMeta.options, value);
      if (match) return match.value;
    } else if (fieldType === 'checkbox') {
      const values = value.split(',').map((v) => v.trim());
      const normalizedValues = values
        .map((v) => findOptionMatch(fieldMeta.options, v)?.value)
        .filter((v): v is string => v !== undefined);
      if (normalizedValues.length > 0) return normalizedValues.join(',');
    }

    return value;
  });
};

/**
 * Validates a row and returns cell errors for client-side validation
 */
export const validateRowClientSide = (
  rowData: string[],
  headers: string[],
  fieldMetadata: BulkUploadFieldMetadataDto[] | undefined
): CellError[] => {
  if (!fieldMetadata) return [];

  const errors: CellError[] = [];
  
  // Build a map of field code to value for conditional checks
  const allRowValues: Record<string, string> = {};
  headers.forEach((header, index) => {
    allRowValues[header] = rowData[index] || '';
  });

  headers.forEach((fieldCode, cellIndex) => {
    const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
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

export const parseCSV = (content: string, delim: Delimiter): ParsedData => {
  const lines = content.split(/\r?\n/).filter(line => line.trim());
  const rows = lines.map(line => {
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
  const labels = rows[1] || [];

  // Check if row 3 is required indicators (R/O pattern)
  let requiredFields: boolean[] = [];
  let dataRows: string[][];

  if (rows[2] && rows[2].every(cell => cell === 'R' || cell === 'O' || cell === '')) {
    // Row 3 is required indicators
    requiredFields = rows[2].map(cell => cell === 'R');
    dataRows = rows.slice(3);
  } else {
    // No required indicators row, assume all fields are optional
    requiredFields = headers.map(() => false);
    dataRows = rows.slice(2);
  }

  return { headers, labels, requiredFields, rows: dataRows };
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

export const convertValidationErrorsToCellErrors = (
  validationResult: BulkUploadValidationResultDto,
  headers: string[],
  parsedData: ParsedData
): CellError[] => {
  const errors: CellError[] = [];

  if (validationResult.rowValidations) {
    validationResult.rowValidations.forEach((rowValidation) => {
      if (!rowValidation.isValid && rowValidation.errors) {
        // Use the backend's rowNumber directly to calculate the correct row index
        // Backend rowNumber is 1-indexed and includes header rows
        // parsedData.rows is 0-indexed data rows only
        
        // Determine how many header rows were skipped
        const hasLabelsRow = parsedData.labels.length > 0 && parsedData.labels.some(l => l.trim() !== '');
        const hasRequiredRow = parsedData.requiredFields.some(r => r);
        let headerOffset = 1; // At minimum, header row
        if (hasLabelsRow) headerOffset++;
        if (hasRequiredRow) headerOffset++;
        
        // Calculate row index from backend's row number
        const rowIndex = rowValidation.rowNumber - headerOffset - 1;

        if (rowIndex >= 0 && rowIndex < parsedData.rows.length) {
          const rowData = parsedData.rows[rowIndex];
          rowValidation.errors.forEach((error) => {
            const cellIndex = headers.indexOf(error.fieldCode);
            if (cellIndex !== -1) {
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
            }
          });
        }
      }
    });
  }

  // Add sum group validation errors for each row
  if (validationResult.fieldMetadata) {
    parsedData.rows.forEach((row, rowIndex) => {
      const sumErrors = validateSumGroups(row, headers, rowIndex, validationResult.fieldMetadata);
      errors.push(...sumErrors);
    });
  }

  return errors;
};

export const updateRequiredFieldsFromMetadata = (
  headers: string[],
  validationResult: BulkUploadValidationResultDto
): boolean[] => {
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
  return cellErrors.some(error => error.rowIndex === rowIndex);
};

export const getErrorMessage = (rowIndex: number, cellIndex: number, cellErrors: CellError[]): string | null => {
  const error = cellErrors.find(e => e.rowIndex === rowIndex && e.cellIndex === cellIndex);
  return error?.message || null;
};

interface ConditionalRule {
  showWhen?: {
    fieldKey?: string;
    equals?: string;
    notEquals?: string;
    in?: string[];
    allOf?: Array<{
      fieldKey: string;
      equals?: string;
      notEquals?: string;
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
export const validateSumGroups = (
  rowData: string[],
  headers: string[],
  rowIndex: number,
  fieldMetadata: FieldMetadataWithRequired[] | undefined
): CellError[] => {
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
  fieldMetadata: FieldMetadataWithRequired[] | undefined
): boolean => {
  if (!fieldMetadata) return true;

  const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
  if (!fieldMeta?.conditionalRules) return true; // No conditions = always visible

  try {
    const rules: ConditionalRule = JSON.parse(fieldMeta.conditionalRules);
    if (!rules.showWhen) return true;

    // Helper to check if a value matches (handles both single values and comma-separated checkbox values)
    const valueMatches = (parentValue: string | undefined, targetValue: string, parentFieldCode: string): boolean => {
      if (!parentValue) return false;
      const normalizedTarget = targetValue.toLowerCase();
      
      // Check if parent field is a checkbox (multi-select) by looking at field metadata
      const parentFieldMeta = fieldMetadata.find(f => f.fieldCode === parentFieldCode);
      const isCheckbox = parentFieldMeta?.fieldType?.toLowerCase() === 'checkbox';
      
      if (isCheckbox) {
        // For checkbox fields, check if any selected value matches
        const selectedValues = parentValue.split(',').map(v => v.trim().toLowerCase());
        
        // Map labels to values for comparison
        const optionValueMap = new Map<string, string>();
        if (parentFieldMeta?.options) {
          parentFieldMeta.options.forEach(opt => {
            optionValueMap.set(opt.label.toLowerCase(), opt.value.toLowerCase());
            optionValueMap.set(opt.value.toLowerCase(), opt.value.toLowerCase());
          });
        }
        
        const normalizedSelectedValues = selectedValues.map(v => optionValueMap.get(v) || v);
        return normalizedSelectedValues.includes(normalizedTarget);
      }
      
      return parentValue.toLowerCase() === normalizedTarget;
    };

    const valueNotMatches = (parentValue: string | undefined, targetValue: string, parentFieldCode: string): boolean => {
      return !valueMatches(parentValue, targetValue, parentFieldCode);
    };

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
      return rules.showWhen.allOf.every(condition => {
        const parentFieldIndex = headers.indexOf(condition.fieldKey);
        if (parentFieldIndex === -1) return false;
        const parentValue = rowData[parentFieldIndex]?.trim();
        
        if (condition.equals) {
          return valueMatches(parentValue, condition.equals, condition.fieldKey);
        }
        if (condition.notEquals) {
          return valueNotMatches(parentValue, condition.notEquals, condition.fieldKey);
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
        const selectedValues = rawParentValue?.split(',').map(v => v.trim().toLowerCase()).filter(v => v !== '') || [];

        // Get the parent field's options to map labels to values
        const parentFieldMeta = fieldMetadata.find(f => f.fieldCode === rules.showWhen?.fieldKey);
        const optionValueMap = new Map<string, string>();
        if (parentFieldMeta?.options) {
          parentFieldMeta.options.forEach(opt => {
            optionValueMap.set(opt.label.toLowerCase(), opt.value.toLowerCase());
            optionValueMap.set(opt.value.toLowerCase(), opt.value.toLowerCase());
          });
        }

        // Normalize selected values to option values (handle both labels and values)
        const normalizedSelectedValues = selectedValues.map(v => optionValueMap.get(v) || v);

        return rules.showWhen.in.some(v => normalizedSelectedValues.includes(v.toLowerCase()));
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
export const shouldValidateField = (
  fieldCode: string,
  rowData: string[],
  headers: string[],
  fieldMetadata: FieldMetadataWithRequired[] | undefined
): boolean => {
  if (!fieldMetadata) return true;

  const fieldIndex = headers.indexOf(fieldCode);
  if (fieldIndex === -1) return true;

  const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
  
  // For fields with conditional rules, check if parent condition is met first
  if (fieldMeta?.conditionalRules) {
    const visible = isFieldVisible(fieldCode, rowData, headers, fieldMetadata);
    if (!visible) return false; // Don't validate hidden fields
  }

  // Check all preceding fields (left to right)
  for (let i = 0; i < fieldIndex; i++) {
    const precedingFieldCode = headers[i];
    const precedingMeta = fieldMetadata.find(f => f.fieldCode === precedingFieldCode);
    
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
export const isFieldConditionallyRequired = (
  fieldCode: string,
  rowData: string[],
  headers: string[],
  fieldMetadata: FieldMetadataWithRequired[] | undefined
): boolean => {
  if (!fieldMetadata) return false;

  const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
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

  const fieldMeta = fieldMetadata.find(f => f.fieldCode === fieldCode);
  if (!fieldMeta?.conditionalRules) return { isInvalid: false };

  // Check if field has a value
  const hasValue = cellValue && cellValue.trim() !== '';
  if (!hasValue) return { isInvalid: false };

  // Check if field is visible (parent condition met)
  const visible = isFieldVisible(fieldCode, rowData, headers, fieldMetadata);
  
  // If field has a value but should NOT be visible (parent condition NOT met),
  // it's an error - the value should be empty
  if (!visible && hasValue) {
    try {
      const rules: ConditionalRule = JSON.parse(fieldMeta.conditionalRules);
      if (rules.showWhen?.fieldKey) {
        const parentFieldIndex = headers.indexOf(rules.showWhen.fieldKey);
        const parentFieldMeta = fieldMetadata.find(f => f.fieldCode === rules.showWhen?.fieldKey);
        
        // Get parent question label (from labels array or fallback to code)
        const parentLabel = (labels && parentFieldIndex >= 0 ? labels[parentFieldIndex] : null) 
          || parentFieldMeta?.fieldCode 
          || rules.showWhen.fieldKey;
        
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
              const match = parentFieldMeta?.options?.find(
                (opt) => opt.value.toLowerCase() === v.toLowerCase()
              );
              return match?.label ?? v;
            })
            .join(', ');
          return {
            isInvalid: true,
            errorMessage: `"${currentFieldLabel}" should be empty because "${parentLabel}" does not include "${expectedValue}"`
          };
        } else {
          expectedValue = rules.showWhen.equals || 'Yes';
          return {
            isInvalid: true,
            errorMessage: `"${currentFieldLabel}" should be empty because "${parentLabel}" is "${parentValue || 'empty'}" (expected "${expectedValue}" to fill this field)`
          };
        }
      }
    } catch {
      // Invalid JSON
    }
    return {
      isInvalid: true,
      errorMessage: 'This field should be empty based on the parent field value'
    };
  }

  return { isInvalid: false };
};
